using System.ComponentModel.DataAnnotations;
using System.Linq;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    public class Sprint
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(1000)]
        public string? Description { get; set; }

        [StringLength(500)]
        public string? Goal { get; set; }

        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; } = true;
        public bool IsCompleted { get; set; } = false;
        public SprintStatus Status { get; set; } = SprintStatus.Planning;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation Properties
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public string CreatorUserId { get; set; } = null!;

        public ICollection<SprintTask> SprintTasks { get; set; } = new List<SprintTask>();

        // برای دسترسی آسان به تسک‌ها (Issues)
        public ICollection<TaskItem> Tasks => SprintTasks.Select(st => st.Task).ToList();

        /// <summary>
        /// آیا می‌شه Issue جدید به این Sprint اضافه کرد؟
        /// فقط Sprint فعال می‌تونه Issue بگیره
        /// </summary>
        public bool CanAddIssues => Status == SprintStatus.Active;
    }

    public enum SprintStatus
    {
        Planning = 0,    // در حال برنامه‌ریزی
        Active = 1,      // فعال
        Completed = 2    // خاتمه یافته
    }
}