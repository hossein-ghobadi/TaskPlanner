using System;
using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.Boards
{
    /// <summary>
    /// یادداشت کار تخته
    /// </summary>
    public class BoardTaskComment
    {
        public int Id { get; set; }

        public string? Message { get; set; }

        // ارتباط با کار تخته
        [Required]
        public int BoardTaskId { get; set; }
        public BoardTask BoardTask { get; set; } = null!;

        // کاربر فرستنده پیام
        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = null!;
        
        [StringLength(200)]
        public string UserName { get; set; } = null!; // برای نمایش سریع‌تر

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // برای ویرایش و حذف پیام
        public bool IsEdited { get; set; } = false;
        public bool IsDeleted { get; set; } = false;

        // فایل‌های پیوست
        public ICollection<BoardTaskCommentAttachment> Attachments { get; set; } = new List<BoardTaskCommentAttachment>();
    }

    /// <summary>
    /// فایل پیوست یادداشت کار تخته
    /// </summary>
    public class BoardTaskCommentAttachment
    {
        public int Id { get; set; }

        [Required]
        public int BoardTaskCommentId { get; set; }
        public BoardTaskComment BoardTaskComment { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string FileName { get; set; } = null!; // نام اصلی فایل

        [Required]
        [StringLength(1000)]
        public string FilePath { get; set; } = null!; // مسیر ذخیره شده

        [Required]
        [StringLength(50)]
        public string FileType { get; set; } = null!; // نوع فایل: Image, Audio, Document, Other

        public long FileSize { get; set; } // سایز به بایت

        [StringLength(100)]
        public string? MimeType { get; set; } // مثلاً image/jpeg, audio/mpeg

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}

