using System;
using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// عکس در گالری پروژه
    /// </summary>
    public class ProjectImageGallery
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام عکس الزامی است")]
        [MaxLength(200, ErrorMessage = "نام عکس نمی‌تواند بیشتر از 200 کاراکتر باشد")]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        // ارتباط با پروژه
        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        // کاربر ایجادکننده عکس
        [Required]
        public string CreatorUserId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // پوشه (اختیاری)
        public int? FolderId { get; set; }
        public ProjectImageGalleryFolder? Folder { get; set; }

        // اطلاعات فایل عکس
        [Required]
        public string FileName { get; set; } = null!; // نام اصلی فایل

        [Required]
        public string FilePath { get; set; } = null!; // مسیر ذخیره شده (URL)

        public long FileSize { get; set; } // سایز به بایت

        public string? MimeType { get; set; } // مثلاً image/jpeg, image/png

        // ابعاد عکس (اختیاری)
        public int? Width { get; set; }
        public int? Height { get; set; }

        // Thumbnail (اختیاری - برای نمایش کوچک)
        public string? ThumbnailPath { get; set; }

        /// <summary>
        /// تگ‌ها برای دسته‌بندی و فیلتر (با کاما جدا شده، مثلاً: صفحه اصلی,لاگین,موبایل)
        /// </summary>
        [MaxLength(500)]
        public string? Tags { get; set; }
    }
}

