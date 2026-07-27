namespace TaskPlanner.Application.Services.LeaveService
{
    public interface ILeaveService
    {
        Task<IReadOnlyList<LeaveTypeDto>> GetActiveLeaveTypesAsync(CancellationToken cancellationToken = default);
        Task<IReadOnlyList<ColleagueSelectDto>> GetColleaguesAsync(string adminUserId, CancellationToken cancellationToken = default);
        Task<LeaveRecordListResultDto> GetMyRecordsAsync(string adminUserId, LeaveRecordFilterDto filter, CancellationToken cancellationToken = default);
        Task<LeaveRecordDto?> GetByIdAsync(int id, string adminUserId, CancellationToken cancellationToken = default);
        Task<int> CreateAsync(string adminUserId, CreateLeaveRecordDto dto, CancellationToken cancellationToken = default);
        Task UpdateAsync(string adminUserId, UpdateLeaveRecordDto dto, CancellationToken cancellationToken = default);
        Task DeleteAsync(int id, string adminUserId, CancellationToken cancellationToken = default);
        Task<LeaveStatsDto> GetStatsAsync(string adminUserId, DateTime from, DateTime to, CancellationToken cancellationToken = default);
        Task<IReadOnlyList<LeaveCalendarDayDto>> GetCalendarAsync(string adminUserId, DateTime startOfWeek, DateTime endOfWeek, CancellationToken cancellationToken = default);
    }
}
