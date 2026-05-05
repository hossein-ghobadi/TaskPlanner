using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    public class LeadMember
    {
        public int Id { get; set; }

        public int LeadId { get; set; }
        public Lead Lead { get; set; } = null!;

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = null!;

        [StringLength(450)]
        public string? AddedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
