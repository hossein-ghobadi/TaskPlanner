namespace TaskPlanner.Application.Services.DashboardService
{
    public class UserDashboardDto
    {
        public int TotalProjectsCount { get; set; }
        public int PendingProjectInviteCount { get; set; }
        public int CollaboratorCount { get; set; }
        public int PersonalNotesCount { get; set; }
        public int UnreadNotifications { get; set; }

        public int MyOpenProjectTasks { get; set; }
        public int MyCompletedProjectTasks { get; set; }
        public int MyOverdueProjectTasks { get; set; }

        public int MyOpenBoardTasks { get; set; }
        public int MyCompletedBoardTasks { get; set; }

        public int ActiveSprintCount { get; set; }
        public int AccessibleLeadsCount { get; set; }

        public List<DashboardMeetingDto> UpcomingMeetings { get; set; } = new();
        public List<DashboardChartSliceDto> TaskPrioritySlices { get; set; } = new();
        public List<DashboardChartSliceDto> ProjectTaskDistributionSlices { get; set; } = new();
        public List<DashboardChartSliceDto> ProjectTaskDistributionAllSlices { get; set; } = new();
        public List<DashboardChartSliceDto> LeadPipelineSlices { get; set; } = new();
        public List<DashboardDueDayDto> DueTasksNext7Days { get; set; } = new();
        public List<DashboardDueTaskItemDto> DueTasksToday { get; set; } = new();
        public List<DashboardDueTaskItemDto> DueTasksTomorrow { get; set; } = new();
    }

    public class DashboardMeetingDto
    {
        public DateTime AtUtc { get; set; }
        public int LeadId { get; set; }
        public string LeadTitle { get; set; } = string.Empty;
        public DashboardMeetingKind Kind { get; set; }
    }

    public enum DashboardMeetingKind
    {
        ScheduledSession = 0,
        LeadScheduledField = 1
    }

    public class DashboardChartSliceDto
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
        public string Color { get; set; } = "#6366f1";
    }

    public class DashboardDueDayDto
    {
        public string Label { get; set; } = string.Empty;
        public int Count { get; set; }
    }

    public class DashboardDueTaskItemDto
    {
        public int TaskId { get; set; }
        public int ProjectId { get; set; }
        public string Title { get; set; } = string.Empty;
        public string ProjectName { get; set; } = string.Empty;
        public DateTime DueDateUtc { get; set; }
    }
}
