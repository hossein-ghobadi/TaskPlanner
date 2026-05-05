using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Domain.Entities.Users;
using TaskPlanner.Application.Interfaces.Contexts;
using System.Linq;

namespace TaskPlanner.Application.Services.ProjectService
{
    /// <summary>
    /// سرویس Query برای پروژه‌ها - فقط برای خواندن اطلاعات
    /// </summary>
    public class ProjectQueryService : IProjectQueryService
    {
        private readonly IMVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;

        public ProjectQueryService(IMVPTestDatabaseContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        /// <summary>
        /// دریافت لیست پروژه‌های کاربر (که سازنده یا عضو آن است)
        /// </summary>
        public async Task<List<Project>> GetUserProjectsAsync(string userId)
        {
            return await _context.Projects
                .Include(p => p.Tasks)
                    .ThenInclude(t => t.ProjectIssueType)
                .Include(p => p.Members)
                .Where(p =>
                    p.CreatorUserId == userId ||            // خودش سازنده پروژه باشد
                    p.Members.Any(m => m.UserId == userId)  // یا در لیست اعضا باشد
                )
                .ToListAsync();
        }

        /// <summary>
        /// دریافت جزئیات یک پروژه با تمام وابستگی‌ها
        /// </summary>
        public async Task<Project?> GetProjectWithDetailsAsync(int projectId)
        {
            return await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == projectId);
        }

        /// <summary>
        /// بررسی دسترسی کاربر به پروژه
        /// </summary>
        public async Task<bool> HasUserAccessToProjectAsync(string userId, int projectId)
        {
            return await _context.Projects
                .AnyAsync(p => p.Id == projectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));
        }

        /// <summary>
        /// دریافت تمام اطلاعات لازم برای نمایش Details پروژه
        /// </summary>
        public async Task<ProjectDetailsDto> GetProjectDetailsDtoAsync(int projectId)
        {
            // دریافت پروژه
            var project = await GetProjectWithDetailsAsync(projectId);
            if (project == null)
                throw new InvalidOperationException("پروژه یافت نشد.");

            // دریافت سازنده پروژه
            var creator = await _userManager.FindByIdAsync(project.CreatorUserId);

            // ساخت DTO
            var dto = new ProjectDetailsDto
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                CreatorUserId = project.CreatorUserId,
                CreatorUserName = creator != null ? creator.FullName : "ناشناس",
                Tasks = new List<TaskItem>(),
                MemberUserNames = new List<string>(),
                Invitations = new List<ProjectInvitationDto>()
            };

            // 👥 اعضای پروژه
            var memberUsernames = new List<string>();
            var members = new List<ProjectMemberDto>();
            foreach (var member in project.Members)
            {
                var userInfo = await _userManager.FindByIdAsync(member.UserId);
                if (userInfo != null)
                {
                    var displayName = $"{userInfo.FullName ?? "بدون نام"} ({userInfo.Phone})";
                    memberUsernames.Add(displayName);
                    members.Add(new ProjectMemberDto
                    {
                        UserId = member.UserId,
                        DisplayName = displayName
                    });
                }
            }
            dto.MemberUserNames = memberUsernames;
            dto.Members = members;

            // 📋 تسک‌های نمایش‌پذیر در بخش پروژه (بدون Epic)
            dto.Tasks = await _context.TaskItems
                .Where(t => t.ProjectId == projectId
                    && (t.IssueType == IssueType.Story || t.IssueType == IssueType.Task || t.IssueType == IssueType.Bug))
                .Include(t => t.Category)
                .Include(t => t.AssignedUser)
                .Include(t => t.ChildIssues.Where(c => c.IssueType == IssueType.Subtask))
                    .ThenInclude(c => c.AssignedUser)
                .ToListAsync();

            // 🕓 دعوت‌های در انتظار (Pending)
            dto.Invitations = await _context.ProjectInvitations
                .Where(i => i.ProjectId == projectId && i.Status == InvitationStatus.Pending)
                .Select(i => new ProjectInvitationDto
                {
                    Id = i.Id,
                    InviteePhone = i.InviteePhone,
                    Status = i.Status,
                    CreatedAt = i.CreatedAt
                })
                .ToListAsync();

            // 🏷️ دسته‌بندی‌های پروژه
            dto.Categories = await _context.TaskCategories
                .Where(c => c.ProjectId == projectId)
                .OrderBy(c => c.Name)
                .ToListAsync();

            // 🎯 اسپرینت فعال
            var activeSprint = await _context.Sprints
                .Where(s => s.ProjectId == projectId && s.IsActive)
                .Select(s => new { s.Id, s.Name })
                .FirstOrDefaultAsync();

            if (activeSprint != null)
            {
                dto.ActiveSprintId = activeSprint.Id;
                dto.ActiveSprintName = activeSprint.Name;
            }

            // 📊 آمار تسک‌ها برای نمایش در View
            var taskStats = await _context.TaskItems
                .Where(t => t.ProjectId == projectId
                    && (t.IssueType == IssueType.Story || t.IssueType == IssueType.Task || t.IssueType == IssueType.Bug))
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Total = g.Count(),
                    Completed = g.Count(t => t.IsCompleted)
                })
                .FirstOrDefaultAsync();

            dto.TotalTasks = taskStats?.Total ?? 0;
            dto.CompletedTasks = taskStats?.Completed ?? 0;
            dto.ProgressPercentage = dto.TotalTasks > 0
                ? (dto.CompletedTasks * 100 / dto.TotalTasks)
                : 0;

            var categoryTaskCounts = await _context.TaskItems
                .Where(t => t.ProjectId == projectId
                    && t.CategoryId != null
                    && (t.IssueType == IssueType.Story || t.IssueType == IssueType.Task || t.IssueType == IssueType.Bug))
                .GroupBy(t => t.CategoryId!.Value)
                .Select(g => new
                {
                    CategoryId = g.Key,
                    Count = g.Count()
                })
                .ToListAsync();

            dto.CategoryTaskCounts = categoryTaskCounts.ToDictionary(x => x.CategoryId, x => x.Count);

            return dto;
        }

        /// <summary>
        /// دریافت لیست کاربران قابل انتخاب برای افزودن به پروژه (برای Create)
        /// </summary>
        public async Task<List<UserSelectDto>> GetAvailableUsersForCreateAsync(string inviterId)
        {
            // فقط کاربران تاییدشده (Accepted) از جدول دعوت‌ها
            var acceptedInviteIds = await _context.ProjectInvitations
                .Where(i => i.InviterId == inviterId && i.Status == InvitationStatus.Accepted)
                .Select(i => i.InviteeId)
                .ToListAsync();

            // تطبیق با کاربران سیستم
            var acceptedUsers = _userManager.Users
                .Where(u => acceptedInviteIds.Contains(u.Id))
                .Select(u => new UserSelectDto
                {
                    Id = u.Id,
                    DisplayName = !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.UserName
                })
                .ToList();

            return acceptedUsers;
        }

        /// <summary>
        /// دریافت لیست دعوت‌های در انتظار کاربر
        /// </summary>
        public async Task<List<string>> GetPendingInvitationsAsync(string inviterId)
        {
            return await _context.ProjectInvitations
                .Where(i => i.InviterId == inviterId && i.Status == InvitationStatus.Pending)
                .Select(i => i.InviteePhone)
                .ToListAsync();
        }

        /// <summary>
        /// دریافت لیست کاربران قابل انتخاب برای ویرایش پروژه
        /// </summary>
        public async Task<List<UserSelectDto>> GetAvailableUsersForEditAsync(string inviterId, int projectId)
        {
            var project = await GetProjectWithDetailsAsync(projectId);
            if (project == null)
                throw new InvalidOperationException("پروژه یافت نشد.");

            // اعضای پروژه فعلی
            var projectMembers = project.Members.Select(m => m.UserId).ToList();

            // تمام دعوت‌هایی که من فرستاده‌ام
            var allMyInvites = await _context.ProjectInvitations
                .Where(i => i.InviterId == inviterId)
                .ToListAsync();

            // دعوت‌های همین پروژه
            var projectInvites = allMyInvites
                .Where(i => i.ProjectId == projectId)
                .ToList();

            // دعوت‌های عمومی (قبلاً دعوت‌شده ولی هنوز در پروژه خاصی نیستند)
            var globalInvites = allMyInvites
                .Where(i => i.ProjectId == null)
                .ToList();

            // شماره موبایل تمام افرادی که تاکنون دعوت‌شده‌اند
            var invitedPhones = allMyInvites.Select(i => i.InviteePhone).Distinct().ToList();

            // کاربران سیستم
            var allUsers = await _userManager.Users
                .Where(u => invitedPhones.Contains(u.Phone) || projectMembers.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.Phone,
                    DisplayName = !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.UserName
                })
                .ToListAsync();

            var selectableUsers = new List<UserSelectDto>();
            var addedKeys = new HashSet<string>();

            // اعضای فعلی پروژه
            foreach (var member in allUsers.Where(u => projectMembers.Contains(u.Id)))
            {
                selectableUsers.Add(new UserSelectDto
                {
                    Id = member.Id,
                    DisplayName = $"👤 {member.DisplayName} (عضو پروژه)"
                });
                addedKeys.Add(member.Id);
            }

            // دعوت‌شده‌های همین پروژه
            foreach (var invite in projectInvites)
            {
                var user = allUsers.FirstOrDefault(u => u.Phone == invite.InviteePhone);
                string statusText = invite.Status switch
                {
                    InvitationStatus.Pending => "🕓 در انتظار پذیرش",
                    InvitationStatus.Accepted => "✅ پذیرفته‌شده",
                    InvitationStatus.Rejected => "❌ رد شده",
                    _ => "نامشخص"
                };

                string uniqueKey = user?.Id ?? $"ghost-{invite.InviteePhone}";
                if (addedKeys.Contains(uniqueKey)) continue;

                selectableUsers.Add(new UserSelectDto
                {
                    Id = uniqueKey,
                    DisplayName = user != null
                        ? $"📱 {user.DisplayName} ({statusText})"
                        : $"📞 {invite.InviteePhone} ({statusText})"
                });
                addedKeys.Add(uniqueKey);
            }

            // کاربرانی که قبلاً توسط من دعوت‌شده‌اند ولی هنوز در این پروژه دعوت نشده‌اند
            foreach (var invite in globalInvites)
            {
                var user = allUsers.FirstOrDefault(u => u.Phone == invite.InviteePhone);
                if (user == null || addedKeys.Contains(user.Id)) continue;

                selectableUsers.Add(new UserSelectDto
                {
                    Id = user.Id,
                    DisplayName = $"➕ {user.DisplayName} (قابل دعوت به این پروژه)"
                });
                addedKeys.Add(user.Id);
            }

            return selectableUsers;
        }

        public async Task<ProjectDashboardDto> GetProjectsDashboardAsync(string userId)
        {
            var userPhone = await _userManager.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.Phone)
                .FirstOrDefaultAsync();

            var projects = await _context.Projects
                .AsNoTracking()
                .Where(p => p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId))
                .Select(p => new ProjectCardDto
                {
                    Id = p.Id,
                    Name = p.Name,
                    CreatorUserId = p.CreatorUserId,
                    TaskCount = p.Tasks.Count(t =>
                        t.ProjectIssueTypeId != null
                            ? t.ProjectIssueType != null && t.ProjectIssueType.CanAddToSprint
                            : (t.IssueType == IssueType.Story || t.IssueType == IssueType.Task || t.IssueType == IssueType.Bug))
                })
                .ToListAsync();

            var projectIds = projects.Select(p => p.Id).ToList();
            var activeSprintByProject = await _context.Sprints
                .AsNoTracking()
                .Where(s => projectIds.Contains(s.ProjectId) && s.Status == SprintStatus.Active)
                .Select(s => new { s.ProjectId, s.Id })
                .ToListAsync();

            var pendingRawInvitations = string.IsNullOrWhiteSpace(userPhone)
                ? new List<ProjectInvitation>()
                : await _context.ProjectInvitations
                    .AsNoTracking()
                    .Where(i => i.InviteePhone == userPhone && i.Status == InvitationStatus.Pending)
                    .OrderByDescending(i => i.CreatedAt)
                    .ToListAsync();

            var pendingProjectInvitations = pendingRawInvitations
                .Where(i => i.ProjectId != null)
                .Take(5)
                .Select(i => new ProjectInvitationDto
                    {
                        Id = i.Id,
                        InviteePhone = i.InviteePhone,
                        Status = i.Status,
                        CreatedAt = i.CreatedAt
                    })
                    .ToList();

            var pendingSystemInvitations = pendingRawInvitations
                .Where(i => i.ProjectId == null)
                .Take(5)
                .Select(i => new ProjectInvitationDto
                {
                    Id = i.Id,
                    InviteePhone = i.InviteePhone,
                    Status = i.Status,
                    CreatedAt = i.CreatedAt
                })
                .ToList();

            var inviterIds = pendingRawInvitations
                .Select(i => i.InviterId)
                .Distinct()
                .ToList();

            var inviterLookup = await _userManager.Users
                .AsNoTracking()
                .Where(u => inviterIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : (u.UserName ?? "کاربر ناشناس"));

            var collaborators = await GetCollaboratorsAsync(userId, userPhone);

            var recentNotes = await _context.PersonalNotes
                .AsNoTracking()
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.CreatedAt)
                .Take(5)
                .Select(n => new RecentPersonalNoteDto
                {
                    Id = n.Id,
                    UserId = n.UserId,
                    Title = n.Title,
                    Content = n.Content != null && n.Content.Length > 100
                        ? n.Content.Substring(0, 100)
                        : n.Content,
                    IsPinned = n.IsPinned,
                    CreatedAt = n.CreatedAt,
                    UpdatedAt = n.UpdatedAt
                })
                .ToListAsync();

            return new ProjectDashboardDto
            {
                Projects = projects,
                ActiveSprintByProject = activeSprintByProject
                    .GroupBy(x => x.ProjectId)
                    .ToDictionary(g => g.Key, g => g.First().Id),
                PendingProjectInvitations = pendingProjectInvitations,
                PendingSystemInvitations = pendingSystemInvitations,
                InviterLookup = inviterLookup,
                Collaborators = collaborators,
                RecentNotes = recentNotes
            };
        }

        private async Task<List<CollaboratorDto>> GetCollaboratorsAsync(string currentUserId, string? phone)
        {
            var acceptedInvitations = await _context.ProjectInvitations
                .AsNoTracking()
                .Where(i =>
                    i.Status == InvitationStatus.Accepted &&
                    (
                        (i.InviterId == currentUserId && (!string.IsNullOrEmpty(i.InviteeId) || !string.IsNullOrEmpty(i.InviteePhone))) ||
                        (!string.IsNullOrWhiteSpace(phone) && i.InviteePhone == phone && !string.IsNullOrEmpty(i.InviteeId))
                    ))
                .Select(i => new
                {
                    i.InviterId,
                    i.InviteeId,
                    i.InviteePhone
                })
                .ToListAsync();

            var inviteeIds = acceptedInvitations
                .Where(i => i.InviterId == currentUserId && !string.IsNullOrEmpty(i.InviteeId))
                .Select(i => i.InviteeId!);

            var inviterIds = acceptedInvitations
                .Where(i => !string.IsNullOrWhiteSpace(phone) && i.InviteePhone == phone && !string.IsNullOrEmpty(i.InviteeId))
                .Select(i => i.InviterId);

            var phoneNumbers = acceptedInvitations
                .Where(i => i.InviterId == currentUserId && string.IsNullOrEmpty(i.InviteeId) && !string.IsNullOrEmpty(i.InviteePhone))
                .Select(i => i.InviteePhone!)
                .Distinct()
                .ToList();

            var usersByPhoneIds = phoneNumbers.Count == 0
                ? new List<string>()
                : await _userManager.Users
                    .AsNoTracking()
                    .Where(u => phoneNumbers.Contains(u.Phone))
                    .Select(u => u.Id)
                    .ToListAsync();

            var membersOfMyProjects = await (
                from pm in _context.ProjectMembers.AsNoTracking()
                join p in _context.Projects.AsNoTracking() on pm.ProjectId.Value equals p.Id
                where p.CreatorUserId == currentUserId && pm.UserId != currentUserId
                select pm.UserId)
                .Distinct()
                .ToListAsync();

            var projectsIMemberOf = await _context.ProjectMembers
                .AsNoTracking()
                .Where(pm => pm.UserId == currentUserId)
                .Select(pm => pm.ProjectId.Value)
                .ToListAsync();

            var creatorsOfMyProjects = projectsIMemberOf.Count == 0
                ? new List<string>()
                : await _context.Projects
                    .AsNoTracking()
                    .Where(p => projectsIMemberOf.Contains(p.Id) && p.CreatorUserId != currentUserId)
                    .Select(p => p.CreatorUserId)
                    .Distinct()
                    .ToListAsync();

            var otherMembersOfMyProjects = projectsIMemberOf.Count == 0
                ? new List<string>()
                : await _context.ProjectMembers
                    .AsNoTracking()
                    .Where(pm => projectsIMemberOf.Contains(pm.ProjectId.Value) && pm.UserId != currentUserId)
                    .Select(pm => pm.UserId)
                    .Distinct()
                    .ToListAsync();

            var collaboratorIds = inviteeIds
                .Concat(inviterIds)
                .Concat(usersByPhoneIds)
                .Concat(membersOfMyProjects)
                .Concat(creatorsOfMyProjects)
                .Concat(otherMembersOfMyProjects)
                .Where(id => !string.IsNullOrEmpty(id) && id != currentUserId)
                .Distinct()
                .ToList();

            if (collaboratorIds.Count == 0)
            {
                return new List<CollaboratorDto>();
            }

            return await _userManager.Users
                .AsNoTracking()
                .Where(u => collaboratorIds.Contains(u.Id))
                .Select(u => new CollaboratorDto
                {
                    Id = u.Id,
                    DisplayName = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : (u.UserName ?? "کاربر ناشناس"),
                    Phone = u.Phone ?? string.Empty,
                    Email = u.Email ?? string.Empty
                })
                .ToListAsync();
        }
    }
}
