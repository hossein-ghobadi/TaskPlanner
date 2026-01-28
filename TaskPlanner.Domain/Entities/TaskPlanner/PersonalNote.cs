using System;
using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    public class PersonalNote
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "عنوان یادداشت الزامی است")]
        public string Title { get; set; } = null!;

        public string? Content { get; set; }

        // فقط به کاربر مربوط می‌شود
        public string UserId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // اولویت یا رنگ (اختیاری)
        public string? Color { get; set; }
        public bool IsPinned { get; set; } = false;

        // پوشه (اختیاری)
        public int? FolderId { get; set; }
        public PersonalNoteFolder? Folder { get; set; }

        // فایل‌های پیوست
        public ICollection<PersonalNoteAttachment> Attachments { get; set; } = new List<PersonalNoteAttachment>();
    }

    // مدل برای فایل‌های پیوست یادداشت شخصی
    public class PersonalNoteAttachment
    {
        public int Id { get; set; }

        [Required]
        public int PersonalNoteId { get; set; }
        public PersonalNote PersonalNote { get; set; } = null!;

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


