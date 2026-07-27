using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    public class CrmInvitation
    {
        public int Id { get; set; }

        public int CrmId { get; set; }
        public Crm Crm { get; set; } = null!;

        [Required]
        [StringLength(450)]
        public string InviterId { get; set; } = null!;

        [StringLength(450)]
        public string InviteeId { get; set; } = string.Empty;

        [Required]
        [StringLength(50)]
        public string InviteePhone { get; set; } = null!;

        public string? ResponseMessage { get; set; }

        public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
    }
}
