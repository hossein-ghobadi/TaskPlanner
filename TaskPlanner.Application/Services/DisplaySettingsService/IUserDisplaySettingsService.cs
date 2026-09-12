namespace TaskPlanner.Application.Services.DisplaySettingsService
{
    public interface IUserDisplaySettingsService
    {
        Task<UserDisplaySettingsDto> GetAsync(string userId, CancellationToken cancellationToken = default);

        /// <param name="isAdmin">اگر false باشد فیلدهای ادمین ذخیره/بازنویسی نمی‌شوند</param>
        Task SaveAsync(
            string userId,
            UserDisplaySettingsDto settings,
            bool isAdmin,
            CancellationToken cancellationToken = default);
    }

    public class UserDisplaySettingsDto
    {
        public bool ShowLeads { get; set; } = true;
        public bool ShowBoards { get; set; } = true;
        public bool ShowMindMaps { get; set; } = true;
        public bool ShowDesigns { get; set; } = true;
        public bool ShowAdminUsers { get; set; } = true;
        public bool ShowAdminLeaves { get; set; } = true;
    }
}
