namespace TaskPlanner.Application.Services.DisplaySettingsService
{
    public interface IUserProjectDisplaySettingsService
    {
        Task<UserProjectDisplaySettingsDto> GetAsync(
            string userId,
            int projectId,
            CancellationToken cancellationToken = default);

        Task SaveAsync(
            string userId,
            int projectId,
            UserProjectDisplaySettingsDto settings,
            CancellationToken cancellationToken = default);
    }

    public class UserProjectDisplaySettingsDto
    {
        public bool ShowTasks { get; set; } = true;
        public bool ShowKanban { get; set; } = true;
        public bool ShowSprints { get; set; } = true;
        public bool ShowFeatures { get; set; } = true;
        public bool ShowTickets { get; set; } = true;
        public bool ShowGallery { get; set; } = true;
        public bool ShowCategories { get; set; } = true;
    }
}
