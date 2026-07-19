using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Users;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// پیام در گفتگوی تیکت (سوال یا پاسخ)
    /// </summary>
    public class ProjectTicketMessage
    {
        public int Id { get; set; }

        [Required]
        public int TicketId { get; set; }
        public ProjectTicket Ticket { get; set; } = null!;

        [Required]
        [StringLength(450)]
        public string AuthorUserId { get; set; } = null!;
        public User? AuthorUser { get; set; }

        public TicketMessageKind Kind { get; set; } = TicketMessageKind.Question;

        [StringLength(4000)]
        public string Body { get; set; } = "";

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProjectTicketMessageAttachment> Attachments { get; set; } = new List<ProjectTicketMessageAttachment>();
    }

    public class ProjectTicketMessageAttachment
    {
        public int Id { get; set; }

        [Required]
        public int MessageId { get; set; }
        public ProjectTicketMessage Message { get; set; } = null!;

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

    public enum TicketMessageKind
    {
        Question = 0,
        Answer = 1
    }
}
