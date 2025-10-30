using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Models
{
    public class CombinedInvitationsVm
    {
        public List<ProjectInvitation> ProjectInvitations { get; set; } = new();
        public List<ProjectInvitation> UserInvitations { get; set; } = new();
    }
}
