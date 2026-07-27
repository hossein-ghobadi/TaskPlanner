using TaskPlanner.Application.Services.ProjectService;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace TaskPlanner.Application.Services.CrmService
{
    public interface ICrmService
    {
        Task<Crm> GetOrCreatePersonalCrmAsync(string userId, CancellationToken cancellationToken = default);
        Task<CrmAccess?> GetCrmAccessAsync(int crmId, string userId, CancellationToken cancellationToken = default);
        Task<CrmAccess?> GetCrmAccessForLeadAsync(int leadId, string userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<int>> GetAccessibleCrmIdsAsync(string userId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CrmSelectDto>> GetAccessibleCrmsAsync(string userId, CancellationToken cancellationToken = default);
        /// <summary>لیدهای قدیمی بدون CrmId را به CRM شخصی مالک وصل می‌کند و LeadMemberها را ارتقا می‌دهد.</summary>
        Task EnsureLegacyLeadsAssignedToCrmAsync(CancellationToken cancellationToken = default);

        Task<IReadOnlyList<CrmMemberSummaryDto>> GetMembersAsync(int crmId, string requesterUserId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<CrmPendingInviteDto>> GetPendingInvitationsAsync(int crmId, string requesterUserId, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<UserSelectDto>> GetAvailableCollaboratorsAsync(int crmId, string requesterUserId, CancellationToken cancellationToken = default);

        Task AddMemberAsync(int crmId, string memberUserId, string requesterUserId, CancellationToken cancellationToken = default);
        Task RemoveMemberAsync(int crmId, string memberUserId, string requesterUserId, CancellationToken cancellationToken = default);
        Task InviteByPhoneAsync(int crmId, string phone, string inviterId, CancellationToken cancellationToken = default);
        Task CancelInvitationAsync(int invitationId, string inviterUserId, CancellationToken cancellationToken = default);
        Task RespondToInvitationAsync(int invitationId, string currentUserId, string currentUserPhone, bool accept, CancellationToken cancellationToken = default);
    }

    public class CrmAccess
    {
        public int CrmId { get; set; }
        public string OwnerUserId { get; set; } = null!;
        public bool IsOwner { get; set; }
        public bool CanEditLeads { get; set; }
        public bool CanManageMembers { get; set; }
    }

    public class CrmMemberSummaryDto
    {
        public string UserId { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
        public CrmMemberRole Role { get; set; }
    }

    public class CrmPendingInviteDto
    {
        public int Id { get; set; }
        public string InviteePhone { get; set; } = null!;
        public string? InviteeDisplayName { get; set; }
    }

    public class CrmSelectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string OwnerUserId { get; set; } = null!;
        public string? OwnerDisplayName { get; set; }
        public bool IsOwner { get; set; }

        public string DisplayLabel =>
            IsOwner
                ? "من"
                : (OwnerDisplayName ?? "همکار");
    }
}
