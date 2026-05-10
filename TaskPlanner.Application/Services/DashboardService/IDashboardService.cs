namespace TaskPlanner.Application.Services.DashboardService
{
    public interface IDashboardService
    {
        Task<UserDashboardDto> GetUserDashboardAsync(string userId, CancellationToken cancellationToken = default);
    }
}
