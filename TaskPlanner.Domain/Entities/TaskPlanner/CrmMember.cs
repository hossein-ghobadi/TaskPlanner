using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    public class CrmMember
    {
        public int Id { get; set; }

        public int CrmId { get; set; }
        public Crm Crm { get; set; } = null!;

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = null!;

        public CrmMemberRole Role { get; set; } = CrmMemberRole.Member;

        [StringLength(450)]
        public string? AddedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
