using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// اتصال یک گروه بله به گروه چت تیمی داخلی پروژه.
    /// </summary>
    public class ProjectBaleGroupLink
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        [Required]
        public int ProjectChatGroupId { get; set; }
        public ProjectChatGroup ProjectChatGroup { get; set; } = null!;

        /// <summary>شناسه عددی چت در بله (معمولاً عدد منفی).</summary>
        [Required]
        public long BaleChatId { get; set; }

        /// <summary>نام کاربری کانال/گروه بدون @ (برای تطبیق پیام‌ها).</summary>
        [StringLength(100)]
        public string? BaleChatUsername { get; set; }

        [StringLength(200)]
        public string? BaleChatTitle { get; set; }

        /// <summary>توکن بازوی اختصاصی این پروژه.</summary>
        [Required]
        [StringLength(120)]
        public string BotToken { get; set; } = null!;

        /// <summary>آخرین update_id دریافت‌شده از API بله برای این اتصال.</summary>
        public int LastUpdateId { get; set; }

        public bool IsEnabled { get; set; } = true;

        [Required]
        [StringLength(450)]
        public string CreatedByUserId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    /// <summary>
    /// وضعیت polling سراسری بازو (یک ردیف).
    /// </summary>
    public class BaleBotSyncState
    {
        public int Id { get; set; } = 1;

        public int LastUpdateId { get; set; }

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
