using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// وضعیت‌های چرخه کاری (Workflow Status)
    /// هر پروژه می‌تواند وضعیت‌های سفارشی خودش را داشته باشد
    /// </summary>
    public class WorkflowStatus
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام وضعیت الزامی است")]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        /// <summary>
        /// رنگ نمایش وضعیت (برای UI)
        /// </summary>
        [MaxLength(20)]
        public string? Color { get; set; }

        /// <summary>
        /// ترتیب نمایش
        /// </summary>
        public int Order { get; set; }

        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        /// <summary>
        /// نوع وضعیت: Todo, InProgress, Done
        /// این برای دسته‌بندی کلی استفاده می‌شود
        /// </summary>
        [Required]
        [MaxLength(50)]
        public string Type { get; set; } = WorkflowType.Todo;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// آیا این Status پیش‌فرض پروژه است؟
        /// Issue های جدید به صورت خودکار این Status رو میگیرن
        /// </summary>
        public bool IsDefault { get; set; } = false;

        /// <summary>
        /// آیا این Status وضعیت نهایی است؟ (مثل Done, Closed)
        /// Issue هایی که به این Status میرسن، IsCompleted=true میشن
        /// </summary>
        public bool IsFinal { get; set; } = false;

        // تسک‌هایی که در این وضعیت هستند
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

        // Transitions که از این Status شروع میشن
        public ICollection<WorkflowTransition> OutgoingTransitions { get; set; } = new List<WorkflowTransition>();

        // Transitions که به این Status ختم میشن
        public ICollection<WorkflowTransition> IncomingTransitions { get; set; } = new List<WorkflowTransition>();
    }

    /// <summary>
    /// انواع کلی workflow
    /// </summary>
    public static class WorkflowType
    {
        public const string Todo = "Todo";              // باید انجام شود
        public const string InProgress = "InProgress";  // در حال انجام
        public const string Done = "Done";              // انجام شده
        public const string Blocked = "Blocked";        // مسدود شده
    }
}

