using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// انتقالات (Transitions) بین وضعیت‌های چرخه کاری
    /// تعیین می‌کند کدام Status می‌تواند به کدام Status دیگر منتقل شود
    /// </summary>
    public class WorkflowTransition
    {
        public int Id { get; set; }

        /// <summary>
        /// نام Transition (مثلاً "شروع کار"، "تکمیل"، "بازگشت")
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// توضیحات Transition
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// وضعیت مبدا (From Status)
        /// </summary>
        [Required]
        public int FromStatusId { get; set; }
        public WorkflowStatus FromStatus { get; set; } = null!;

        /// <summary>
        /// وضعیت مقصد (To Status)
        /// </summary>
        [Required]
        public int ToStatusId { get; set; }
        public WorkflowStatus ToStatus { get; set; } = null!;

        /// <summary>
        /// پروژه مربوطه
        /// </summary>
        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        /// <summary>
        /// آیا این Transition فعال است؟
        /// </summary>
        public bool IsActive { get; set; } = true;

        /// <summary>
        /// ترتیب نمایش (برای UI)
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// آیا نیاز به تایید دارد؟ (برای مراحل بحرانی)
        /// </summary>
        public bool RequiresApproval { get; set; } = false;

        /// <summary>
        /// آیا فقط Assignee می‌تونه این Transition رو انجام بده؟
        /// </summary>
        public bool OnlyAssigneeCanTransition { get; set; } = false;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}


