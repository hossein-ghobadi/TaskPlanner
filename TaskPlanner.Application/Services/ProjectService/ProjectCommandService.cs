using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using System.Text.Json;
using TaskPlanner.Application.Interfaces.Contexts;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Domain.Entities.Users;
using TaskPlanner.Application.Services.NotificationService;
using TaskPlanner.Application.Services.SMS;

namespace TaskPlanner.Application.Services.ProjectService
{
    /// <summary>
    /// سرویس Command برای پروژه‌ها - فقط برای تغییر و ایجاد اطلاعات
    /// </summary>
    public class ProjectCommandService : IProjectCommandService
    {
        private readonly IMVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;
        private readonly INotificationService _notificationService;
        private readonly IInvitationSmsService _invitationSmsService;

        public ProjectCommandService(
            IMVPTestDatabaseContext context,
            UserManager<User> userManager,
            INotificationService notificationService,
            IInvitationSmsService invitationSmsService)
        {
            _context = context;
            _userManager = userManager;
            _notificationService = notificationService;
            _invitationSmsService = invitationSmsService;
        }

        private async Task EnsureDefaultIssueTypesAsync(Project project, string creatorUserId)
        {
            if (project == null)
            {
                return;
            }

            var now = DateTime.UtcNow;
            var existingLevels = await _context.ProjectIssueTypes
                .Where(p => p.ProjectId == project.Id && !p.IsCustom)
                .Select(p => new { p.Level, p.BaseType })
                .ToListAsync();

            var defaults = new List<ProjectIssueType>();

            // Epic - همیشه باید وجود داشته باشد (Level = Epic)
            var hasEpic = existingLevels.Any(e => e.Level == IssueTypeLevel.Epic);
            if (!hasEpic)
            {
                defaults.Add(new ProjectIssueType
                {
                    ProjectId = project.Id,
                    Name = "اپیک",
                    Description = "نوع پیش‌فرض اپیک",
                    Icon = "📦",
                    Color = "#8B5CF6",
                    Order = 0,
                    BaseType = IssueType.Epic,
                    IsCustom = false,
                    CanAddToSprint = false,
                    CanHaveChildren = true,
                    IncludeInReports = true,
                    Level = IssueTypeLevel.Epic,
                    CreatedByUserId = creatorUserId,
                    CreatedAt = now
                });
            }

            // Task - همیشه باید وجود داشته باشد (Level = StoryLevel)
            var hasTask = existingLevels.Any(e => e.Level == IssueTypeLevel.StoryLevel && e.BaseType == IssueType.Task);
            if (!hasTask)
            {
                defaults.Add(new ProjectIssueType
                {
                    ProjectId = project.Id,
                    Name = "تسک",
                    Description = "نوع پیش‌فرض تسک",
                    Icon = "✅",
                    Color = "#3B82F6",
                    Order = 1,
                    BaseType = IssueType.Task,
                    IsCustom = false,
                    CanAddToSprint = true,
                    CanHaveChildren = true,
                    IncludeInReports = true,
                    Level = IssueTypeLevel.StoryLevel,
                    CreatedByUserId = creatorUserId,
                    CreatedAt = now
                });
            }

            // Subtask - همیشه باید وجود داشته باشد (Level = Subtask)
            var hasSubtask = existingLevels.Any(e => e.Level == IssueTypeLevel.Subtask);
            if (!hasSubtask)
            {
                defaults.Add(new ProjectIssueType
                {
                    ProjectId = project.Id,
                    Name = "زیرتسک",
                    Description = "نوع پیش‌فرض زیرتسک",
                    Icon = "🔹",
                    Color = "#06B6D4",
                    Order = 2,
                    BaseType = IssueType.Subtask,
                    IsCustom = false,
                    CanAddToSprint = false,
                    CanHaveChildren = false,
                    IncludeInReports = true,
                    Level = IssueTypeLevel.Subtask,
                    CreatedByUserId = creatorUserId,
                    CreatedAt = now
                });
            }

            if (defaults.Any())
            {
                _context.ProjectIssueTypes.AddRange(defaults);
                await _context.SaveChangesAsync();
            }
        }
        /// <summary>
        /// ایجاد پروژه جدید
        /// </summary>
        public async Task<int> CreateProjectAsync(CreateProjectDto dto, string creatorUserId)
        {
            // ایجاد پروژه جدید
            var project = new Project
            {
                Name = dto.Name,
                Description = dto.Description,
                CreatorUserId = creatorUserId
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();

            await EnsureDefaultIssueTypesAsync(project, creatorUserId);
            
            // Seed کردن Workflow پیش‌فرض برای پروژه‌های جدید
            var hasAnyStatuses = await _context.WorkflowStatuses.AnyAsync(ws => ws.ProjectId == project.Id);
            if (!hasAnyStatuses)
            {
                // 1) ایجاد وضعیت‌های پیش‌فرض
                var wf = WorkflowHelper.CreateDefaultWorkflow(project.Id);
                _context.WorkflowStatuses.AddRange(wf.statuses);
                await _context.SaveChangesAsync();

                // 2) پیدا کردن ID وضعیت‌ها برای ساخت Transition ها
                var statuses = await _context.WorkflowStatuses
                    .Where(ws => ws.ProjectId == project.Id)
                    .OrderBy(ws => ws.Order)
                    .ToListAsync();

                var todoId = statuses.FirstOrDefault(s => s.Type == WorkflowType.Todo)?.Id;
                var inProgressId = statuses.FirstOrDefault(s => s.Type == WorkflowType.InProgress)?.Id;
                var doneId = statuses.FirstOrDefault(s => s.Type == WorkflowType.Done)?.Id;
                var blockedId = statuses.FirstOrDefault(s => s.Type == WorkflowType.Blocked)?.Id;

                if (todoId.HasValue && inProgressId.HasValue && doneId.HasValue && blockedId.HasValue)
                {
                    var transitions = WorkflowHelper.CreateDefaultTransitions(
                        project.Id, todoId.Value, inProgressId.Value, doneId.Value, blockedId.Value);

                    // جلوگیری از تکرار
                    var existingKeys = await _context.WorkflowTransitions
                        .Where(t => t.ProjectId == project.Id)
                        .Select(t => new { t.FromStatusId, t.ToStatusId })
                        .ToListAsync();

                    var toAdd = transitions.Where(t => !existingKeys
                        .Any(k => k.FromStatusId == t.FromStatusId && k.ToStatusId == t.ToStatusId))
                        .ToList();

                    if (toAdd.Any())
                    {
                        _context.WorkflowTransitions.AddRange(toAdd);
                        await _context.SaveChangesAsync();
                    }
                }
            }

            // اضافه کردن اعضای انتخاب‌شده
            if (dto.SelectedUserIds != null && dto.SelectedUserIds.Any())
            {
                foreach (var memberId in dto.SelectedUserIds)
                {
                    _context.ProjectMembers.Add(new ProjectMember
                    {
                        ProjectId = project.Id,
                        UserId = memberId
                    });
                }
            }

            // اتصال دعوت‌های در انتظار به این پروژه
            var pendingInvitations = await _context.ProjectInvitations
                .Where(i => i.InviterId == creatorUserId && i.ProjectId == null && i.Status == InvitationStatus.Pending)
                .ToListAsync();

            if (pendingInvitations.Any())
            {
                foreach (var invite in pendingInvitations)
                {
                    invite.ProjectId = project.Id;
                }
            }

            await _context.SaveChangesAsync();
            return project.Id;
        }

        /// <summary>
        /// ویرایش پروژه
        /// </summary>
        public async Task UpdateProjectAsync(UpdateProjectDto dto)
        {
            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == dto.Id);

            if (project == null)
                throw new InvalidOperationException("پروژه یافت نشد.");

            // بروزرسانی اطلاعات پروژه
            project.Name = dto.Name;
            project.Description = dto.Description;
            project.IsClosed = dto.IsClosed;
            project.UpdatedAt = DateTime.UtcNow;

            var currentMembers = project.Members.Select(m => m.UserId).ToList();
            var newMembers = dto.SelectedUserIds ?? new List<string>();

            // افزودن اعضای جدید
            var toAdd = newMembers.Except(currentMembers).ToList();
            foreach (var userId in toAdd)
            {
                _context.ProjectMembers.Add(new ProjectMember
                {
                    ProjectId = project.Id,
                    UserId = userId
                });
            }

            // حذف اعضای حذف‌شده
            var toRemove = currentMembers.Except(newMembers).ToList();
            foreach (var userId in toRemove)
            {
                var member = project.Members.FirstOrDefault(m => m.UserId == userId);
                if (member != null)
                    _context.ProjectMembers.Remove(member);
            }

            if (toRemove.Count > 0)
                await RemoveProjectInvitationsForUsersAsync(project.Id, toRemove);

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// حذف پروژه و تمام وابستگی‌های آن - فقط سازنده می‌تواند حذف کند
        /// </summary>
        public async Task DeleteProjectAsync(int projectId, string userId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
                throw new InvalidOperationException("پروژه یافت نشد.");

            // بررسی اینکه فقط سازنده پروژه می‌تواند آن را حذف کند
            if (project.CreatorUserId != userId)
                throw new InvalidOperationException("فقط سازنده پروژه می‌تواند آن را حذف کند.");

            // حذف به ترتیب صحیح (از وابسته‌ترین به مستقل‌ترین)
            
            // 1) حذف SprintTasks (وابسته به TaskItems) - باید قبل از TaskItems حذف شوند
            var taskIds = await _context.TaskItems
                .Where(t => t.ProjectId == projectId)
                .Select(t => t.Id)
                .ToListAsync();
            
            if (taskIds.Any())
            {
                var sprintTasks = await _context.SprintTasks
                    .Where(st => taskIds.Contains(st.TaskId))
                    .ToListAsync();
                _context.SprintTasks.RemoveRange(sprintTasks);
            }

            // 2) حذف IssueStatusHistories (وابسته به TaskItems)
            var histories = await _context.IssueStatusHistories
                .Where(h => _context.TaskItems.Any(t => t.Id == h.TaskId && t.ProjectId == projectId))
                .ToListAsync();
            _context.IssueStatusHistories.RemoveRange(histories);

            // 3) حذف WorkflowTransitions (وابسته به WorkflowStatuses)
            var transitions = _context.WorkflowTransitions.Where(t => t.ProjectId == projectId);
            _context.WorkflowTransitions.RemoveRange(transitions);

            // 4) حذف WorkflowStatuses (وابسته به Project)
            var statuses = _context.WorkflowStatuses.Where(s => s.ProjectId == projectId);
            _context.WorkflowStatuses.RemoveRange(statuses);

            // 5) حذف TaskItems (وابسته به Project)
            var tasks = _context.TaskItems.Where(t => t.ProjectId == projectId);
            _context.TaskItems.RemoveRange(tasks);

            // 6) حذف ProjectMembers (وابسته به Project)
            var members = _context.ProjectMembers.Where(m => m.ProjectId == projectId);
            _context.ProjectMembers.RemoveRange(members);

            // 7) حذف ProjectInvitations (وابسته به Project)
            var invitations = _context.ProjectInvitations.Where(i => i.ProjectId == projectId);
            _context.ProjectInvitations.RemoveRange(invitations);

            // 8) حذف ProjectNotes (وابسته به Project)
            var notes = _context.ProjectNotes.Where(n => n.ProjectId == projectId);
            _context.ProjectNotes.RemoveRange(notes);

            // 9) حذف Sprints (وابسته به Project)
            var sprints = _context.Sprints.Where(s => s.ProjectId == projectId);
            _context.Sprints.RemoveRange(sprints);

            // 10) حذف Project (آخرین)
            _context.Projects.Remove(project);
            
            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// دعوت کاربر به پروژه
        /// </summary>
        public async Task InviteUserToProjectAsync(int projectId, string phone, string inviterId)
        {
            if (string.IsNullOrWhiteSpace(phone))
                throw new ArgumentException("شماره تلفن وارد نشده است.", nameof(phone));

            phone = phone.Trim();

            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null)
                throw new InvalidOperationException("پروژه یافت نشد.");

            var inviter = await _userManager.FindByIdAsync(inviterId);

            if (inviter != null && !string.IsNullOrWhiteSpace(inviter.Phone) &&
                string.Equals(inviter.Phone, phone, StringComparison.OrdinalIgnoreCase))
            {
                throw new InvalidOperationException("نمی‌توانید خودتان را به پروژه دعوت کنید.");
            }

            // بررسی وجود دعوت در انتظار
            var exists = await _context.ProjectInvitations
                .AnyAsync(i => i.InviteePhone == phone && i.ProjectId == projectId && i.Status == InvitationStatus.Pending);

            if (exists)
                throw new InvalidOperationException("برای این شماره قبلاً دعوت در انتظار ارسال شده است.");

            // پیدا کردن کاربر
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Phone == phone);
            if (user == null)
                throw new InvalidOperationException("کاربری با این شماره پیدا نشد.");

            if (string.Equals(user.Id, inviterId, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("نمی‌توانید خودتان را به پروژه دعوت کنید.");

            if (string.Equals(project.CreatorUserId, user.Id, StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("سازنده پروژه نیازی به دعوت ندارد.");

            var isMember = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == user.Id);

            if (isMember)
                throw new InvalidOperationException("این کاربر هم‌اکنون عضو پروژه است.");

            // دعوت‌های پذیرفته/ردشدهٔ قبلی مانع دعوت مجدد نمی‌شوند
            await RemoveStaleProjectInvitationsAsync(projectId, user.Id, phone);

            // ثبت دعوت
            var invite = new ProjectInvitation
            {
                InviterId = inviterId,
                InviteePhone = phone,
                InviteeId = user.Id,
                ProjectId = projectId,
                Status = InvitationStatus.Pending
            };

            _context.ProjectInvitations.Add(invite);
            await _context.SaveChangesAsync();

            await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
            {
                UserId = user.Id,
                Title = $"دعوت به پروژه جدید",
                Message = $"برای شما دعوتی به پروژه «{project.Name}» ارسال شد.",
                RelatedEntityId = invite.Id.ToString(),
                RelatedEntityType = nameof(ProjectInvitation),
                Type = NotificationCreateType.ProjectInvitation,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    invitationId = invite.Id,
                    projectId = projectId
                })
            });

            var inviterName = !string.IsNullOrWhiteSpace(inviter?.FullName)
                ? inviter.FullName
                : inviter?.UserName;

            await _invitationSmsService.TrySendProjectInvitationAsync(phone, project.Name, inviterName);
        }

        /// <summary>
        /// حذف عضو از پروژه و تبدیل تسک‌های مرتبط به بدون مسئول
        /// </summary>
        public async Task RemoveMemberFromProjectAsync(int projectId, string memberUserId, string requesterUserId)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
                throw new InvalidOperationException("پروژه یافت نشد.");

            // بررسی اینکه فقط سازنده پروژه می‌تواند عضو را حذف کند
            if (project.CreatorUserId != requesterUserId)
                throw new InvalidOperationException("فقط سازنده پروژه می‌تواند اعضا را حذف کند.");

            // بررسی اینکه کاربر نمی‌تواند خودش را حذف کند
            if (memberUserId == requesterUserId)
                throw new InvalidOperationException("نمی‌توانید خودتان را از پروژه حذف کنید.");

            // بررسی اینکه عضو واقعاً در پروژه است
            var member = await _context.ProjectMembers
                .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == memberUserId);

            if (member == null)
                throw new InvalidOperationException("این کاربر عضو این پروژه نیست.");

            // پیدا کردن تمام تسک‌های پروژه که به این کاربر اختصاص داده شده‌اند
            var assignedTasks = await _context.TaskItems
                .Where(t => t.ProjectId == projectId && t.AssignedUserId == memberUserId)
                .ToListAsync();

            // تبدیل تسک‌ها به بدون مسئول
            foreach (var task in assignedTasks)
            {
                task.AssignedUserId = null;
            }

            // حذف عضو از پروژه
            _context.ProjectMembers.Remove(member);

            var memberUser = await _userManager.FindByIdAsync(memberUserId);
            await RemoveProjectInvitationsForUserAsync(projectId, memberUserId, memberUser?.Phone);

            await _context.SaveChangesAsync();
        }

        private async Task RemoveProjectInvitationsForUserAsync(int projectId, string userId, string? phone)
        {
            await RemoveProjectInvitationsForUsersAsync(projectId, new[] { userId }, phone);
        }

        private async Task RemoveProjectInvitationsForUsersAsync(int projectId, IEnumerable<string> userIds, string? phone = null)
        {
            var ids = userIds.Where(id => !string.IsNullOrEmpty(id)).Distinct().ToList();
            if (ids.Count == 0 && string.IsNullOrWhiteSpace(phone))
                return;

            var invitations = await _context.ProjectInvitations
                .Where(i => i.ProjectId == projectId &&
                    (ids.Contains(i.InviteeId) || (!string.IsNullOrEmpty(phone) && i.InviteePhone == phone)))
                .ToListAsync();

            if (invitations.Count > 0)
                _context.ProjectInvitations.RemoveRange(invitations);
        }

        private async Task RemoveStaleProjectInvitationsAsync(int projectId, string userId, string phone)
        {
            var leftoverInvites = await _context.ProjectInvitations
                .Where(i => i.ProjectId == projectId
                    && i.Status != InvitationStatus.Pending
                    && (i.InviteeId == userId || i.InviteePhone == phone))
                .ToListAsync();

            if (leftoverInvites.Count > 0)
                _context.ProjectInvitations.RemoveRange(leftoverInvites);
        }

        /// <summary>
        /// بستن یا باز کردن پروژه — سازنده و اعضای پروژه می‌توانند این کار را انجام دهند.
        /// </summary>
        public async Task<bool> ToggleProjectClosedAsync(int projectId, string userId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
                throw new InvalidOperationException("پروژه یافت نشد.");

            var hasAccess = project.CreatorUserId == userId
                || await _context.ProjectMembers.AnyAsync(m => m.ProjectId == projectId && m.UserId == userId);

            if (!hasAccess)
                throw new InvalidOperationException("فقط اعضای پروژه می‌توانند آن را ببندند یا باز کنند.");

            project.IsClosed = !project.IsClosed;
            project.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return project.IsClosed;
        }
    }
}

