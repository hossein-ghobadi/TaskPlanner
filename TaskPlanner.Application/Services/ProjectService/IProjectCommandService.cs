namespace TaskPlanner.Application.Services.ProjectService
{
    /// <summary>
    /// سرویس Command برای پروژه‌ها - فقط برای تغییر و ایجاد اطلاعات
    /// </summary>
    public interface IProjectCommandService
    {
        /// <summary>
        /// ایجاد پروژه جدید
        /// </summary>
        Task<int> CreateProjectAsync(CreateProjectDto dto, string creatorUserId);

        /// <summary>
        /// ویرایش پروژه
        /// </summary>
        Task UpdateProjectAsync(UpdateProjectDto dto);

        /// <summary>
        /// حذف پروژه و تمام وابستگی‌های آن
        /// </summary>
        Task DeleteProjectAsync(int projectId);

        /// <summary>
        /// دعوت کاربر به پروژه
        /// </summary>
        Task InviteUserToProjectAsync(int projectId, string phone, string inviterId);
    }
}

