using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// تاریخچه تغییرات Status برای هر Issue
    /// برای Audit و گزارش‌گیری
    /// </summary>
    public class IssueStatusHistory
    {
        public int Id { get; set; }

        /// <summary>
        /// Issue مربوطه
        /// </summary>
        [Required]
        public int TaskId { get; set; }
        public TaskItem Task { get; set; } = null!;

        /// <summary>
        /// Status قبلی (nullable برای اولین Status)
        /// </summary>
        public int? FromStatusId { get; set; }
        public WorkflowStatus? FromStatus { get; set; }

        /// <summary>
        /// Status جدید
        /// </summary>
        [Required]
        public int ToStatusId { get; set; }
        public WorkflowStatus ToStatus { get; set; } = null!;

        /// <summary>
        /// Transition استفاده شده (اختیاری)
        /// </summary>
        public int? TransitionId { get; set; }
        public WorkflowTransition? Transition { get; set; }

        /// <summary>
        /// کاربری که تغییر رو انجام داده
        /// </summary>
        [Required]
        [StringLength(450)]
        public string ChangedByUserId { get; set; } = null!;

        /// <summary>
        /// دلیل تغییر (اختیاری)
        /// </summary>
        [StringLength(1000)]
        public string? ChangeReason { get; set; }

        /// <summary>
        /// زمان تغییر
        /// </summary>
        public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
    }
}


