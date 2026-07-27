using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>فضای مشترک CRM؛ لیدها داخل CRM زندگی می‌کنند و عضویت در سطح CRM است.</summary>
    public class Crm
    {
        public int Id { get; set; }

        [Required]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [Required]
        [StringLength(450)]
        public string OwnerUserId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CrmMember> Members { get; set; } = new List<CrmMember>();
        public ICollection<CrmInvitation> Invitations { get; set; } = new List<CrmInvitation>();
        public ICollection<Lead> Leads { get; set; } = new List<Lead>();
    }
}
