using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    public class LeadNote
    {
        public int Id { get; set; }

        [Required]
        public int LeadId { get; set; }
        public Lead Lead { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        public string? Content { get; set; }

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
    }
}
