using System;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace TaskPlanner.Domain.Entities.Users
{
    /// <summary>
    /// تنظیمات نمایش بخش‌های سایدبار پروژه برای هر کاربر در هر پروژه.
    /// </summary>
    public class UserProjectDisplayPreference
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;
        public User User { get; set; } = null!;

        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        public bool ShowTasks { get; set; } = true;
        public bool ShowKanban { get; set; } = true;
        public bool ShowSprints { get; set; } = true;
        public bool ShowFeatures { get; set; } = true;
        public bool ShowTickets { get; set; } = true;
        public bool ShowGallery { get; set; } = true;
        public bool ShowCategories { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
