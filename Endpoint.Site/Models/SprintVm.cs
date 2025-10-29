using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Areas.TaskPlanner.Models
{
    public class SprintVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string? Goal { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }
        public SprintStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public Project Project { get; set; } = null!;
        
        // Properties for Views
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
        public ICollection<TaskItem> TodoTasks { get; set; } = new List<TaskItem>();
        public DateTime? CompletedAt { get; set; }
        
        // Persian date properties
        public string StartDatePersian => StartDate.ToString("yyyy/MM/dd");
        public string EndDatePersian => EndDate.ToString("yyyy/MM/dd");
        
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int PendingTasks { get; set; }
        public int BlockedTasks { get; set; }
        
        public double ProgressPercentage => TotalTasks > 0 ? (double)CompletedTasks / TotalTasks * 100 : 0;
        
        public List<SprintTaskVm> SprintTasks { get; set; } = new List<SprintTaskVm>();
    }

    public class SprintTaskVm
    {
        public int Id { get; set; }
        public int SprintId { get; set; }
        public int TaskId { get; set; }
        public string TaskTitle { get; set; } = null!;
        public string? TaskDescription { get; set; }
        public DateTime? TaskDueDate { get; set; }
        public bool TaskIsCompleted { get; set; }
        public string? TaskCategoryName { get; set; }
        public string? TaskPriority { get; set; }
        
        public SprintTaskStatus Status { get; set; }
        public int SprintPriority { get; set; }
        public string? SprintNotes { get; set; }
        public DateTime AddedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public string AddedByUserName { get; set; } = null!;
    }
}
