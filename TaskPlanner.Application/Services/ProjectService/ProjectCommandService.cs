using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TaskPlanner.Application.Interfaces.Contexts;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Domain.Entities.Users;

namespace TaskPlanner.Application.Services.ProjectService
{
    /// <summary>
    /// سرویس Command برای پروژه‌ها - فقط برای تغییر و ایجاد اطلاعات
    /// </summary>
    public class ProjectCommandService : IProjectCommandService
    {
        private readonly IMVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;

        public ProjectCommandService(IMVPTestDatabaseContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
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

            await _context.SaveChangesAsync();
        }

        /// <summary>
        /// حذف پروژه و تمام وابستگی‌های آن
        /// </summary>
        public async Task DeleteProjectAsync(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
                throw new InvalidOperationException("پروژه یافت نشد.");

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

            // بررسی وجود دعوت در انتظار
            var exists = await _context.ProjectInvitations
                .AnyAsync(i => i.InviteePhone == phone && i.ProjectId == projectId && i.Status == InvitationStatus.Pending);

            if (exists)
                throw new InvalidOperationException("برای این شماره قبلاً دعوت در انتظار ارسال شده است.");

            // پیدا کردن کاربر
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Phone == phone);
            if (user == null)
                throw new InvalidOperationException("کاربری با این شماره پیدا نشد.");

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
        }
    }
}

