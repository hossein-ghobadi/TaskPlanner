using DNTPersianUtils.Core;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Areas.TaskPlanner.Models
{
    public class SprintDetailsVm
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
        public ICollection<SprintTask> SprintTasks { get; set; } = new List<SprintTask>();
        public ICollection<TaskItem> TodoTasks { get; set; } = new List<TaskItem>();
        public DateTime? CompletedAt { get; set; }
        
        // Persian date properties
        public string StartDatePersian => StartDate.ToShortPersianDateString();
        public string EndDatePersian => EndDate.ToShortPersianDateString();
        
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int PendingTasks { get; set; }
        public int BlockedTasks { get; set; }
        
        public double ProgressPercentage => TotalTasks > 0 ? (double)CompletedTasks / TotalTasks * 100 : 0;
    }
}