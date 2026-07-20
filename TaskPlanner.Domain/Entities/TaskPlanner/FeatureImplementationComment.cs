using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Users;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// ثبت تغییرات و یادداشت‌های حین پیاده‌سازی فیچر
    /// </summary>
    public class FeatureImplementationComment
    {
        public int Id { get; set; }

        [Required]
        public int FeatureId { get; set; }
        public ProjectFeature Feature { get; set; } = null!;

        [Required]
        [StringLength(450)]
        public string AuthorUserId { get; set; } = null!;
        public User? AuthorUser { get; set; }

        [Required]
        [StringLength(4000)]
        public string Body { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<FeatureImplementationCommentAttachment> Attachments { get; set; } = new List<FeatureImplementationCommentAttachment>();
    }

    public class FeatureImplementationCommentAttachment
    {
        public int Id { get; set; }

        [Required]
        public int CommentId { get; set; }
        public FeatureImplementationComment Comment { get; set; } = null!;

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

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
    }
}
