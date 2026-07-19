using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Users;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// تیکت / سوال داخل پروژه — می‌تواند به یک فیچر و یک عضو پروژه لینک شود
    /// </summary>
    public class ProjectTicket
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        /// <summary>
        /// فیچر مرتبط با سوال (اختیاری)
        /// </summary>
        public int? FeatureId { get; set; }
        public ProjectFeature? Feature { get; set; }

        [Required(ErrorMessage = "عنوان سوال الزامی است")]
        [StringLength(300)]
        public string Title { get; set; } = null!;

        [StringLength(4000)]
        public string? Description { get; set; }

        public TicketType Type { get; set; } = TicketType.Other;

        /// <summary>
        /// فرد مورد سوال
        /// </summary>
        [Required(ErrorMessage = "انتخاب فرد مورد سوال الزامی است")]
        [StringLength(450)]
        public string AskedToUserId { get; set; } = null!;
        public User? AskedToUser { get; set; }

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }
        public User? CreatedByUser { get; set; }

        public TicketStatus Status { get; set; } = TicketStatus.Open;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<ProjectTicketMessage> Messages { get; set; } = new List<ProjectTicketMessage>();

        /// <summary>
        /// تسک‌های تعریف‌شده برای این تیکت
        /// </summary>
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }

    public enum TicketType
    {
        SpecificationMissing = 0,
        Bug = 1,
        CriticalBug = 2,
        Improvement = 3,
        Other = 4
    }

    public enum TicketStatus
    {
        Open = 0,
        Answered = 1,
        Closed = 2
    }
}
