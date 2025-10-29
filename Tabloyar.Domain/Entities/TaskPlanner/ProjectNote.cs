using System;
using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    public class ProjectNote
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "عنوان یادداشت الزامی است")]
        public string Title { get; set; } = null!;

        public string? Content { get; set; }

        // ارتباط با پروژه
        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        // کاربر ایجادکننده یادداشت
        public string CreatorUserId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // فایل‌های پیوست
        public ICollection<ProjectNoteAttachment> Attachments { get; set; } = new List<ProjectNoteAttachment>();
    }

    // مدل برای فایل‌های پیوست
    public class ProjectNoteAttachment
    {
        public int Id { get; set; }

        [Required]
        public int ProjectNoteId { get; set; }
        public ProjectNote ProjectNote { get; set; } = null!;

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


