using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    public class ProjectChatGroup
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = null!;

        [Required]
        [StringLength(450)]
        public string CreatedByUserId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsArchived { get; set; } = false;

        public ICollection<ProjectChatGroupMember> Members { get; set; } = new List<ProjectChatGroupMember>();
        public ICollection<ProjectChatMessage> Messages { get; set; } = new List<ProjectChatMessage>();
    }

    public class ProjectChatGroupMember
    {
        public int Id { get; set; }

        [Required]
        public int ProjectChatGroupId { get; set; }
        public ProjectChatGroup ProjectChatGroup { get; set; } = null!;

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = null!;

        [StringLength(450)]
        public string? AddedByUserId { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }

    public class ProjectChatMessage
    {
        public int Id { get; set; }

        [Required]
        public int ProjectChatGroupId { get; set; }
        public ProjectChatGroup ProjectChatGroup { get; set; } = null!;

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string UserName { get; set; } = null!;

        [Required]
        [StringLength(4000)]
        public string Message { get; set; } = null!;

        public int? ReplyToMessageId { get; set; }
        public ProjectChatMessage? ReplyToMessage { get; set; }
        public ICollection<ProjectChatMessage> Replies { get; set; } = new List<ProjectChatMessage>();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;

        /// <summary>منبع خارجی پیام، مثلاً Bale.</summary>
        [StringLength(20)]
        public string? ExternalProvider { get; set; }

        /// <summary>شناسه پیام در پلتفرم خارجی (برای جلوگیری از تکرار).</summary>
        public long? ExternalMessageId { get; set; }

        /// <summary>شناسه کاربر بله (در صورت ارسال توسط کاربر).</summary>
        public long? ExternalSenderBaleId { get; set; }

        [StringLength(100)]
        public string? ExternalSenderUsername { get; set; }

        [StringLength(100)]
        public string? ExternalSenderFirstName { get; set; }

        [StringLength(100)]
        public string? ExternalSenderLastName { get; set; }

        [StringLength(20)]
        public string? ExternalSenderPhone { get; set; }

        public bool ExternalIsChannelSender { get; set; }

        [StringLength(500)]
        public string? ExternalSenderPhotoPath { get; set; }

        public ICollection<ProjectChatMessageAttachment> Attachments { get; set; } = new List<ProjectChatMessageAttachment>();
    }

    public class ProjectChatMessageAttachment
    {
        public int Id { get; set; }

        [Required]
        public int ProjectChatMessageId { get; set; }
        public ProjectChatMessage ProjectChatMessage { get; set; } = null!;

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
