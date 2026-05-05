using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    public class LeadSession
    {
        public int Id { get; set; }

        [Required]
        public int LeadId { get; set; }
        public Lead Lead { get; set; } = null!;

        /// <summary>زمان جلسه به UTC.</summary>
        public DateTime ScheduledAt { get; set; }

        [StringLength(500)]
        public string? Notes { get; set; }

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
