using System;
using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    public class TaskComment
    {
        public int Id { get; set; }

        public string? Message { get; set; }

        // ارتباط با تسک
        [Required]
        public int TaskId { get; set; }
        public TaskItem Task { get; set; } = null!;

        // کاربر فرستنده پیام
        [Required]
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!; // برای نمایش سریع‌تر

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // برای ویرایش و حذف پیام
        public bool IsEdited { get; set; } = false;
        public bool IsDeleted { get; set; } = false;

        // فایل‌های پیوست
        public ICollection<TaskCommentAttachment> Attachments { get; set; } = new List<TaskCommentAttachment>();
    }

    // مدل برای فایل‌های پیوست کامنت تسک
    public class TaskCommentAttachment
    {
        public int Id { get; set; }

        [Required]
        public int TaskCommentId { get; set; }
        public TaskComment TaskComment { get; set; } = null!;

        [Required]
        public string FileName { get; set; } = null!; // نام اصلی فایل

        [Required]
        public string FilePath { get; set; } = null!; // مسیر ذخیره شده

        [Required]
        public string FileType { get; set; } = null!; // نوع فایل: Image, Audio, Document, Other

        public long FileSize { get; set; } // سایز به بایت

        public string? MimeType { get; set; } // مثلاً image/jpeg, audio/mpeg

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}

