namespace TaskPlanner.Application.Services.MyWorkService
{
    public interface IMyWorkService
    {
        Task<MyWorkPageDto> GetMyWorkAsync(string userId, MyWorkQuery query, CancellationToken cancellationToken = default);
    }
}
