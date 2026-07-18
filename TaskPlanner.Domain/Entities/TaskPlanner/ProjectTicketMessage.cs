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

        [Required(ErrorMessage = "متن پیام الزامی است")]
        [StringLength(4000)]
        public string Body { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public enum TicketMessageKind
    {
        Question = 0,
        Answer = 1
    }
}
