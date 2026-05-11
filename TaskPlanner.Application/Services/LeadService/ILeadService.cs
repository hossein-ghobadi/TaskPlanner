using TaskPlanner.Application.Services.ProjectService;

namespace TaskPlanner.Application.Services.LeadService
{
    public interface ILeadService
    {
        Task<IReadOnlyList<LeadListItemDto>> GetMyLeadsAsync(string userId, CancellationToken cancellationToken = default);
        Task<LeadDetailsDto?> GetDetailsAsync(int id, string userId, CancellationToken cancellationToken = default);
        Task<int> CreateAsync(CreateLeadDto dto, string ownerUserId, CancellationToken cancellationToken = default);
        Task UpdateAsync(UpdateLeadDto dto, string ownerUserId, CancellationToken cancellationToken = default);
        Task<int> ConvertToProjectAsync(int leadId, string ownerUserId, string? projectNameOverride, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, string ownerUserId, CancellationToken cancellationToken = default);

        Task<IReadOnlyList<UserSelectDto>> GetAvailableCollaboratorsForLeadAsync(int leadId, string requesterUserId, CancellationToken cancellationToken = default);
        Task AddLeadMemberAsync(int leadId, string memberUserId, string requesterUserId, CancellationToken cancellationToken = default);
        Task RemoveLeadMemberAsync(int leadId, string memberUserId, string requesterUserId, CancellationToken cancellationToken = default);
        Task InviteUserToLeadAsync(int leadId, string phone, string inviterId, CancellationToken cancellationToken = default);
        Task CancelLeadInvitationAsync(int invitationId, string inviterUserId, CancellationToken cancellationToken = default);
        Task RespondToLeadInvitationAsync(int invitationId, string currentUserId, string currentUserPhone, bool accept, CancellationToken cancellationToken = default);
        Task AddSessionAsync(CreateLeadSessionDto dto, string requesterUserId, CancellationToken cancellationToken = default);
        Task UpdateSessionAsync(UpdateLeadSessionDto dto, string requesterUserId, CancellationToken cancellationToken = default);
        Task RemoveSessionAsync(int sessionId, string requesterUserId, CancellationToken cancellationToken = default);
        Task<int> AddNoteAsync(CreateLeadNoteDto dto, string requesterUserId, CancellationToken cancellationToken = default);
        Task UpdateNoteAsync(UpdateLeadNoteDto dto, string requesterUserId, CancellationToken cancellationToken = default);
        Task RemoveNoteAsync(int noteId, string requesterUserId, CancellationToken cancellationToken = default);
    }
}
