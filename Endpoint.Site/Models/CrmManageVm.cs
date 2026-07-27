using TaskPlanner.Application.Services.CrmService;
using TaskPlanner.Application.Services.ProjectService;

namespace Endpoint.Site.Models
{
    public class CrmManageVm
    {
        public int CrmId { get; set; }
        public string DisplayLabel { get; set; } = null!;
        public List<CrmSelectDto> OwnedCrms { get; set; } = new();
        public List<CrmManageMemberVm> Members { get; set; } = new();
        public List<CrmManageInviteVm> PendingInvitations { get; set; } = new();
        public List<UserSelectDto> CollaboratorChoices { get; set; } = new();
    }

    public class CrmManageMemberVm
    {
        public string UserId { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
    }

    public class CrmManageInviteVm
    {
        public int Id { get; set; }
        public string InviteePhone { get; set; } = null!;
        public string? InviteeDisplayName { get; set; }
    }
}
