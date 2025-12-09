using System;
using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Users;

namespace TaskPlanner.Domain.Entities.Boards
{
    /// <summary>
    /// کار تخته - کارهای داخل تخته (مشابه Trello)
    /// </summary>
    public class BoardTask
    {
        public int Id { get; set; }

        [Required]
        public int BoardId { get; set; }
        public Board Board { get; set; } = null!;

        [Required(ErrorMessage = "عنوان کار الزامی است")]
        [StringLength(500)]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        /// <summary>
        /// وضعیت کار (مثل: To Do, In Progress, Done)
        /// </summary>
        [StringLength(50)]
        public string Status { get; set; } = "To Do";

        /// <summary>
        /// ترتیب نمایش کار در تخته
        /// </summary>
        public int Order { get; set; } = 0;

        /// <summary>
        /// کاربر ایجادکننده کار
        /// </summary>
        [Required]
        [StringLength(450)]
        public string CreatorUserId { get; set; } = null!;

        /// <summary>
        /// کاربر اختصاص‌یافته به کار (اختیاری)
        /// </summary>
        [StringLength(450)]
        public string? AssignedUserId { get; set; }

        /// <summary>
        /// تاریخ سررسید/خاتمه کار (اختیاری)
        /// </summary>
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// آیا کار تکمیل شده است؟ (مستقل از وضعیت)
        /// </summary>
        public bool IsCompleted { get; set; } = false;

        /// <summary>
        /// تاریخ تکمیل کار
        /// </summary>
        public DateTime? CompletedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// کار والد (برای کارک‌ها)
        /// </summary>
        public int? ParentTaskId { get; set; }
        public BoardTask? ParentTask { get; set; }

        /// <summary>
        /// کارک‌های این کار (کارهای خرد شده)
        /// </summary>
        public ICollection<BoardTask> ChildTasks { get; set; } = new List<BoardTask>();

        /// <summary>
        /// یادداشت‌های این کار
        /// </summary>
        public ICollection<BoardTaskComment> Comments { get; set; } = new List<BoardTaskComment>();
    }
}

