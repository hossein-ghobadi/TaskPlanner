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
            foreach (var member in project.Members)
            {
                var userInfo = await _userManager.FindByIdAsync(member.UserId);
                if (userInfo != null)
                    memberUsernames.Add($"{userInfo.FullName ?? "بدون نام"} ({userInfo.Phone})");
            }
            dto.MemberUserNames = memberUsernames;

            // 📋 تسک‌های نمایش‌پذیر در بخش پروژه (بدون Epic)
            dto.Tasks = await _context.TaskItems
                .Where(t => t.ProjectId == projectId
                    && t.ParentTaskId == null
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
                .Where(t => t.ProjectId == projectId && t.IssueType != IssueType.Epic)
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
                .Where(t => t.ProjectId == projectId && t.CategoryId != null && t.IssueType != IssueType.Epic)
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
    }
}
