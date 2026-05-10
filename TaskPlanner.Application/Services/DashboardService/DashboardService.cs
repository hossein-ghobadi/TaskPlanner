using Microsoft.EntityFrameworkCore;
using TaskPlanner.Application.Interfaces.Contexts;
using TaskPlanner.Application.Services.NotificationService;
using TaskPlanner.Application.Services.ProjectService;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace TaskPlanner.Application.Services.DashboardService
{
    public class DashboardService : IDashboardService
    {
        private readonly IMVPTestDatabaseContext _context;
        private readonly IProjectQueryService _projectQueryService;
        private readonly INotificationService _notificationService;

        public DashboardService(
            IMVPTestDatabaseContext context,
            IProjectQueryService projectQueryService,
            INotificationService notificationService)
        {
            _context = context;
            _projectQueryService = projectQueryService;
            _notificationService = notificationService;
        }

        public async Task<UserDashboardDto> GetUserDashboardAsync(string userId, CancellationToken cancellationToken = default)
        {
            // EF DbContext در این scope shared است؛ همه queryها باید ترتیبی باشند.
            var dash = await _projectQueryService.GetProjectsDashboardAsync(userId, 1, 1);

            var projectIds = await _context.Projects
                .AsNoTracking()
                .Where(p => p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId))
                .Select(p => p.Id)
                .ToListAsync(cancellationToken);

            var ownedLeadIds = await _context.Leads.AsNoTracking()
                .Where(l => l.OwnerUserId == userId)
                .Select(l => l.Id)
                .ToListAsync(cancellationToken);
            var memberLeadIds = await _context.LeadMembers.AsNoTracking()
                .Where(m => m.UserId == userId)
                .Select(m => m.LeadId)
                .ToListAsync(cancellationToken);
            var leadIds = ownedLeadIds.Union(memberLeadIds).Distinct().ToList();

            var boardIds = await _context.Boards.AsNoTracking()
                .Where(b => b.CreatorUserId == userId || b.Members.Any(m => m.UserId == userId))
                .Select(b => b.Id)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            var horizon = now.AddDays(42);

            var activeSprintCount = projectIds.Count == 0
                ? 0
                : await _context.Sprints.AsNoTracking()
                    .CountAsync(s => projectIds.Contains(s.ProjectId) && s.Status == SprintStatus.Active, cancellationToken);

            var myProjectTasks = projectIds.Count == 0
                ? new List<ProjectTaskSnapshot>()
                : (await _context.TaskItems.AsNoTracking()
                    .Where(t => t.AssignedUserId == userId && projectIds.Contains(t.ProjectId))
                    .Where(t =>
                        (t.ProjectIssueTypeId != null && t.ProjectIssueType != null && t.ProjectIssueType.CanAddToSprint)
                        || (t.ProjectIssueTypeId == null
                            && (t.IssueType == IssueType.Story || t.IssueType == IssueType.Task || t.IssueType == IssueType.Bug)))
                    .Select(t => new { t.IsCompleted, t.DueDate, t.Priority })
                    .ToListAsync(cancellationToken))
                .Select(x => new ProjectTaskSnapshot(x.IsCompleted, x.DueDate, x.Priority))
                .ToList();

            var dueTaskRows = projectIds.Count == 0
                ? new List<DueTaskSnapshot>()
                : (await _context.TaskItems.AsNoTracking()
                    .Where(t => projectIds.Contains(t.ProjectId))
                    .Where(t => !t.IsCompleted && t.DueDate.HasValue)
                    .Select(t => new
                    {
                        t.Id,
                        t.ProjectId,
                        t.Title,
                        ProjectName = t.Project.Name,
                        DueDate = t.DueDate!.Value
                    })
                    .ToListAsync(cancellationToken))
                .Select(x => new DueTaskSnapshot(x.Id, x.ProjectId, x.Title, x.ProjectName, x.DueDate))
                .ToList();

            var tasksByProject = projectIds.Count == 0
                ? new List<(string ProjectName, int Count)>()
                : (await _context.TaskItems.AsNoTracking()
                    .Where(t => t.AssignedUserId == userId && projectIds.Contains(t.ProjectId))
                    .Where(t =>
                        (t.ProjectIssueTypeId != null && t.ProjectIssueType != null && t.ProjectIssueType.CanAddToSprint)
                        || (t.ProjectIssueTypeId == null
                            && (t.IssueType == IssueType.Story || t.IssueType == IssueType.Task || t.IssueType == IssueType.Bug)))
                    .GroupBy(t => t.Project.Name)
                    .Select(g => new { ProjectName = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken))
                .Select(x => (x.ProjectName, x.Count))
                .ToList();

            var myBoardTasks = boardIds.Count == 0
                ? new List<bool>()
                : await _context.BoardTasks.AsNoTracking()
                    .Where(t => boardIds.Contains(t.BoardId) && t.AssignedUserId == userId)
                    .Select(t => t.IsCompleted)
                    .ToListAsync(cancellationToken);

            var leadPipeline = leadIds.Count == 0
                ? new List<(LeadPipelineStatus Status, int Count)>()
                : (await _context.Leads.AsNoTracking()
                    .Where(l => leadIds.Contains(l.Id))
                    .GroupBy(l => l.Status)
                    .Select(g => new { Status = g.Key, Count = g.Count() })
                    .ToListAsync(cancellationToken))
                .Select(x => (x.Status, x.Count))
                .ToList();

            List<DashboardMeetingDto> meetings = new();
            if (leadIds.Count > 0)
            {
                var sessionMeetings = await _context.LeadSessions.AsNoTracking()
                    .Where(s => leadIds.Contains(s.LeadId) && s.ScheduledAt >= now && s.ScheduledAt <= horizon)
                    .Join(
                        _context.Leads.AsNoTracking(),
                        s => s.LeadId,
                        l => l.Id,
                        (s, l) => new DashboardMeetingDto
                        {
                            AtUtc = s.ScheduledAt,
                            LeadId = l.Id,
                            LeadTitle = l.Title,
                            Kind = DashboardMeetingKind.ScheduledSession
                        })
                    .OrderBy(m => m.AtUtc)
                    .Take(30)
                    .ToListAsync(cancellationToken);

                var fieldMeetings = await _context.Leads.AsNoTracking()
                    .Where(l =>
                        leadIds.Contains(l.Id)
                        && l.MeetingAt.HasValue
                        && l.MeetingAt!.Value >= now
                        && l.MeetingAt.Value <= horizon)
                    .Select(l => new DashboardMeetingDto
                    {
                        AtUtc = l.MeetingAt!.Value,
                        LeadId = l.Id,
                        LeadTitle = l.Title,
                        Kind = DashboardMeetingKind.LeadScheduledField
                    })
                    .ToListAsync(cancellationToken);

                meetings = sessionMeetings
                    .Concat(fieldMeetings)
                    .OrderBy(m => m.AtUtc)
                    .Take(20)
                    .ToList();
            }

            var unread = await _notificationService.CountUnreadAsync(userId, cancellationToken);

            var openProject = myProjectTasks.Count(t => !t.IsCompleted);
            var doneProject = myProjectTasks.Count(t => t.IsCompleted);
            var overdueProject = myProjectTasks.Count(t => !t.IsCompleted && t.DueDate.HasValue && t.DueDate.Value < now);

            var prioritySlices = BuildPrioritySlices(myProjectTasks.Where(t => !t.IsCompleted));

            var startDay = now.Date;
            var dueWeek = new int[7];
            foreach (var t in myProjectTasks.Where(x => !x.IsCompleted && x.DueDate.HasValue))
            {
                var d = t.DueDate!.Value.Date;
                if (d < startDay || d >= startDay.AddDays(7))
                    continue;
                var idx = (int)(d - startDay).TotalDays;
                if (idx is >= 0 and < 7)
                    dueWeek[idx]++;
            }

            var dueDays = new List<DashboardDueDayDto>();
            for (var i = 0; i < 7; i++)
            {
                var day = startDay.AddDays(i);
                dueDays.Add(new DashboardDueDayDto
                {
                    Label = ToShortPersianDayLabel(day),
                    Count = dueWeek[i]
                });
            }

            var localNow = TimeZoneInfo.ConvertTimeFromUtc(now, TimeZoneInfo.Local);
            var localToday = localNow.Date;
            var localTomorrow = localToday.AddDays(1);

            var dueToday = dueTaskRows
                .Where(t => ToLocalDate(t.DueDateUtc) == localToday)
                .OrderBy(t => t.DueDateUtc)
                .Take(8)
                .Select(t => new DashboardDueTaskItemDto
                {
                    TaskId = t.TaskId,
                    ProjectId = t.ProjectId,
                    Title = t.Title,
                    ProjectName = t.ProjectName,
                    DueDateUtc = t.DueDateUtc
                })
                .ToList();

            var dueTomorrow = dueTaskRows
                .Where(t => ToLocalDate(t.DueDateUtc) == localTomorrow)
                .OrderBy(t => t.DueDateUtc)
                .Take(8)
                .Select(t => new DashboardDueTaskItemDto
                {
                    TaskId = t.TaskId,
                    ProjectId = t.ProjectId,
                    Title = t.Title,
                    ProjectName = t.ProjectName,
                    DueDateUtc = t.DueDateUtc
                })
                .ToList();

            return new UserDashboardDto
            {
                TotalProjectsCount = dash.TotalProjectsCount,
                PendingProjectInviteCount = dash.PendingInviteCount,
                CollaboratorCount = dash.CollaboratorCount,
                PersonalNotesCount = dash.RecentNoteCount,
                UnreadNotifications = unread,
                MyOpenProjectTasks = openProject,
                MyCompletedProjectTasks = doneProject,
                MyOverdueProjectTasks = overdueProject,
                MyOpenBoardTasks = myBoardTasks.Count(c => !c),
                MyCompletedBoardTasks = myBoardTasks.Count(c => c),
                ActiveSprintCount = activeSprintCount,
                AccessibleLeadsCount = leadIds.Count,
                UpcomingMeetings = meetings,
                TaskPrioritySlices = prioritySlices,
                ProjectTaskDistributionSlices = BuildProjectDistributionSlices(tasksByProject),
                LeadPipelineSlices = MapLeadPipelineRows(leadPipeline),
                DueTasksNext7Days = dueDays,
                DueTasksToday = dueToday,
                DueTasksTomorrow = dueTomorrow
            };
        }

        private static List<DashboardChartSliceDto> BuildPrioritySlices(IEnumerable<ProjectTaskSnapshot> openTasks)
        {
            var palette = new Dictionary<TaskPriority, string>
            {
                [TaskPriority.Low] = "#94a3b8",
                [TaskPriority.Medium] = "#3b82f6",
                [TaskPriority.High] = "#f59e0b",
                [TaskPriority.Critical] = "#dc2626"
            };

            var labels = new Dictionary<TaskPriority, string>
            {
                [TaskPriority.Low] = "کم",
                [TaskPriority.Medium] = "متوسط",
                [TaskPriority.High] = "بالا",
                [TaskPriority.Critical] = "بحرانی"
            };

            return openTasks
                .GroupBy(t => t.Priority)
                .OrderBy(g => g.Key)
                .Select(g => new DashboardChartSliceDto
                {
                    Label = labels[g.Key],
                    Count = g.Count(),
                    Color = palette[g.Key]
                })
                .Where(s => s.Count > 0)
                .ToList();
        }

        private static List<DashboardChartSliceDto> MapLeadPipelineRows(List<(LeadPipelineStatus Status, int Count)> rows)
        {
            var palette = new Dictionary<LeadPipelineStatus, string>
            {
                [LeadPipelineStatus.New] = "#6366f1",
                [LeadPipelineStatus.Contacted] = "#0ea5e9",
                [LeadPipelineStatus.QuoteAnnounced] = "#f59e0b",
                [LeadPipelineStatus.Qualified] = "#10b981",
                [LeadPipelineStatus.Converted] = "#22c55e",
                [LeadPipelineStatus.Lost] = "#94a3b8"
            };

            static int LeadStatusOrder(LeadPipelineStatus status) => status switch
            {
                LeadPipelineStatus.New => 0,
                LeadPipelineStatus.Contacted => 1,
                LeadPipelineStatus.QuoteAnnounced => 2,
                LeadPipelineStatus.Qualified => 3,
                LeadPipelineStatus.Converted => 4,
                LeadPipelineStatus.Lost => 5,
                _ => 99
            };

            return rows
                .OrderBy(r => LeadStatusOrder(r.Status))
                .Select(r => new DashboardChartSliceDto
                {
                    Label = LeadStatusFa(r.Status),
                    Count = r.Count,
                    Color = palette.GetValueOrDefault(r.Status, "#6366f1")
                })
                .Where(s => s.Count > 0)
                .ToList();
        }

        private static List<DashboardChartSliceDto> BuildProjectDistributionSlices(List<(string ProjectName, int Count)> rows)
        {
            var palette = new[]
            {
                "#4f46e5", "#0ea5e9", "#10b981", "#f59e0b",
                "#ef4444", "#8b5cf6", "#14b8a6", "#f97316"
            };

            return rows
                .Where(r => r.Count > 0)
                .OrderByDescending(r => r.Count)
                .Take(12)
                .Select((r, i) => new DashboardChartSliceDto
                {
                    Label = string.IsNullOrWhiteSpace(r.ProjectName) ? "بدون نام" : r.ProjectName,
                    Count = r.Count,
                    Color = palette[i % palette.Length]
                })
                .ToList();
        }

        private static string LeadStatusFa(LeadPipelineStatus s) => s switch
        {
            LeadPipelineStatus.New => "جدید",
            LeadPipelineStatus.Contacted => "تماس گرفته",
            LeadPipelineStatus.QuoteAnnounced => "اعلام قیمت",
            LeadPipelineStatus.Qualified => "واجد شرایط",
            LeadPipelineStatus.Converted => "تبدیل‌شده",
            LeadPipelineStatus.Lost => "از دست رفته",
            _ => s.ToString()
        };

        /// <summary>برچسب کوتاه روز شمسی برای محور نمودار.</summary>
        private static string ToShortPersianDayLabel(DateTime utcDay)
        {
            try
            {
                var local = utcDay.Kind == DateTimeKind.Utc
                    ? TimeZoneInfo.ConvertTimeFromUtc(utcDay, TimeZoneInfo.Local)
                    : utcDay;
                var pc = new System.Globalization.PersianCalendar();
                return $"{pc.GetMonth(local):00}/{pc.GetDayOfMonth(local):00}";
            }
            catch
            {
                return utcDay.ToString("MM/dd");
            }
        }

        private static DateTime ToLocalDate(DateTime dateTime)
        {
            var local = dateTime.Kind == DateTimeKind.Utc
                ? TimeZoneInfo.ConvertTimeFromUtc(dateTime, TimeZoneInfo.Local)
                : dateTime.ToLocalTime();
            return local.Date;
        }

        private sealed record ProjectTaskSnapshot(bool IsCompleted, DateTime? DueDate, TaskPriority Priority);
        private sealed record DueTaskSnapshot(int TaskId, int ProjectId, string Title, string ProjectName, DateTime DueDateUtc);
    }
}
