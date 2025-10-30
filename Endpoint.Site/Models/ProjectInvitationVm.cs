using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Models
{
    public class ProjectInvitationVm
    {
        public string InviteePhone { get; set; } = null!;
        public InvitationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
