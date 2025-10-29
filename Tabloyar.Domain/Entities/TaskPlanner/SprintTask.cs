using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    public class SprintTask
    {
        [Key]
        public int Id { get; set; }

        public int SprintId { get; set; }
        public Sprint Sprint { get; set; } = null!;

        public int TaskId { get; set; }
        public TaskItem Task { get; set; } = null!;

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
        public string AddedByUserId { get; set; } = null!;

        // وضعیت تسک در اسپرینت
        public SprintTaskStatus Status { get; set; } = SprintTaskStatus.Pending;

        // اولویت در اسپرینت (ممکنه با اولویت اصلی تسک متفاوت باشه)
        public int SprintPriority { get; set; } = 1;

        // یادداشت‌های مخصوص اسپرینت
        [StringLength(1000)]
        public string? SprintNotes { get; set; }

        public DateTime? CompletedAt { get; set; }
    }

    public enum SprintTaskStatus
    {
        Pending = 0,        // در انتظار
        InProgress = 1,     // در حال انجام
        Completed = 2,      // تکمیل شده
        Blocked = 3,        // مسدود شده
        Cancelled = 4       // لغو شده
    }
}
