using TaskPlanner.Domain.Entities.TaskPlanner;

namespace TaskPlanner.Application.Services.ProjectService
{
    /// <summary>
    /// سرویس Query برای پروژه‌ها - فقط برای خواندن اطلاعات
    /// </summary>
    public interface IProjectQueryService
    {
        /// <summary>
        /// دریافت لیست پروژه‌های کاربر (که سازنده یا عضو آن است)
        /// </summary>
        Task<List<Project>> GetUserProjectsAsync(string userId);

        /// <summary>
        /// دریافت جزئیات یک پروژه با تمام وابستگی‌ها
        /// </summary>
        Task<Project?> GetProjectWithDetailsAsync(int projectId);

        /// <summary>
        /// بررسی دسترسی کاربر به پروژه
        /// </summary>
        Task<bool> HasUserAccessToProjectAsync(string userId, int projectId);

        /// <summary>
        /// دریافت تمام اطلاعات لازم برای نمایش Details پروژه
        /// شامل: تسک‌ها، دعوت‌ها، اسپرینت فعال
        /// </summary>
        Task<ProjectDetailsDto> GetProjectDetailsDtoAsync(int projectId);

        /// <summary>
        /// دریافت لیست کاربران قابل انتخاب برای افزودن به پروژه (برای Create)
        /// </summary>
        Task<List<UserSelectDto>> GetAvailableUsersForCreateAsync(string inviterId);

        /// <summary>
        /// دریافت لیست دعوت‌های در انتظار کاربر
        /// </summary>
        Task<List<string>> GetPendingInvitationsAsync(string inviterId);

        /// <summary>
        /// دریافت لیست کاربران قابل انتخاب برای ویرایش پروژه
        /// </summary>
        Task<List<UserSelectDto>> GetAvailableUsersForEditAsync(string inviterId, int projectId);
    }

    /// <summary>
    /// DTO برای انتخاب کاربر
    /// </summary>
    public class UserSelectDto
    {
        public string Id { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
    }
}
