using TaskPlanner.Domain.Entities.TaskPlanner;

namespace TaskPlanner.Application.Services.MyWorkService
{
    public enum MyWorkScope
    {
        Assigned = 0,
        Created = 1,
        All = 2
    }

    public enum MyWorkDueFilter
    {
        All = 0,
        Overdue = 1,
        Today = 2,
        ThisWeek = 3
    }

    public class MyWorkQuery
    {
        public MyWorkScope Scope { get; set; } = MyWorkScope.Assigned;
        public int? ProjectId { get; set; }
        public bool IncludeCompleted { get; set; }
        public MyWorkDueFilter DueFilter { get; set; } = MyWorkDueFilter.All;
    }

    public class MyWorkPageDto
    {
        public MyWorkScope Scope { get; set; }
        public int? ProjectId { get; set; }
        public bool IncludeCompleted { get; set; }
        public MyWorkDueFilter DueFilter { get; set; }

        public int AssignedOpenCount { get; set; }
        public int CreatedOpenCount { get; set; }
        public int OpenCount { get; set; }
        public int OverdueCount { get; set; }
        public int CompletedCount { get; set; }

        public List<MyWorkProjectOptionDto> Projects { get; set; } = new();
        public List<MyWorkItemDto> Items { get; set; } = new();
    }

    public class MyWorkProjectOptionDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }

    public class MyWorkItemDto
    {
        public int Id { get; set; }
        public string? IssueKey { get; set; }
        public string Title { get; set; } = string.Empty;
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = string.Empty;
        public IssueType IssueType { get; set; }
        public string IssueTypeName { get; set; } = string.Empty;
        public string IssueTypeIcon { get; set; } = string.Empty;
        public TaskPriority Priority { get; set; }
        public bool IsCompleted { get; set; }
        public DateTime? DueDate { get; set; }
        public string? SprintName { get; set; }
        public string? WorkflowStatusName { get; set; }
        public string? WorkflowStatusColor { get; set; }
        public string? AssignedUserName { get; set; }
        public bool IsAssignedToMe { get; set; }
        public bool IsCreatedByMe { get; set; }
    }
}
