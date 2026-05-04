using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    public class Lead
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Title { get; set; } = null!;

        [StringLength(200)]
        public string? CompanyName { get; set; }

        [StringLength(200)]
        public string? ContactName { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(256)]
        public string? Email { get; set; }

        public string? Notes { get; set; }

        [StringLength(100)]
        public string? Source { get; set; }

        public LeadPipelineStatus Status { get; set; } = LeadPipelineStatus.New;

        [Required]
        [StringLength(450)]
        public string OwnerUserId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public int? ConvertedProjectId { get; set; }
        public Project? ConvertedProject { get; set; }
        public DateTime? ConvertedAt { get; set; }
    }
}
