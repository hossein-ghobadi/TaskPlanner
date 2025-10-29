using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Areas.TaskPlanner.Models
{
    public class CombinedInvitationsVm
    {
        public List<ProjectInvitation> ProjectInvitations { get; set; } = new();
        public List<ProjectInvitation> UserInvitations { get; set; } = new();
    }
}
