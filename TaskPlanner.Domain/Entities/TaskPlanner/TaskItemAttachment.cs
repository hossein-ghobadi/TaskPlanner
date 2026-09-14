using System;
using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// فایل/تصویر پیوست مستقیم روی TaskItem (مثلاً عکس کارک)
    /// </summary>
    public class TaskItemAttachment
    {
        public int Id { get; set; }

        [Required]
        public int TaskId { get; set; }
        public TaskItem Task { get; set; } = null!;

        [Required]
        [StringLength(500)]
        public string FileName { get; set; } = null!;

        [Required]
        [StringLength(1000)]
        public string FilePath { get; set; } = null!;

        [Required]
        [StringLength(50)]
        public string FileType { get; set; } = null!;

        public long FileSize { get; set; }

        [StringLength(100)]
        public string? MimeType { get; set; }

        public string? UploadedByUserId { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
