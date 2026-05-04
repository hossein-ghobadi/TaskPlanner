namespace TaskPlanner.Application.Services.LeadService
{
    public interface ILeadService
    {
        Task<IReadOnlyList<LeadListItemDto>> GetMyLeadsAsync(string ownerUserId, CancellationToken cancellationToken = default);
        Task<LeadDetailsDto?> GetDetailsAsync(int id, string ownerUserId, CancellationToken cancellationToken = default);
        Task<int> CreateAsync(CreateLeadDto dto, string ownerUserId, CancellationToken cancellationToken = default);
        Task UpdateAsync(UpdateLeadDto dto, string ownerUserId, CancellationToken cancellationToken = default);
        /// <summary>فقط وقتی وضعیت Qualified است و هنوز تبدیل نشده.</summary>
        Task<int> ConvertToProjectAsync(int leadId, string ownerUserId, string? projectNameOverride, CancellationToken cancellationToken = default);

        Task DeleteAsync(int id, string ownerUserId, CancellationToken cancellationToken = default);
    }
}
