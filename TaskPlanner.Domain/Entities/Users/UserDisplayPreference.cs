using System;

namespace TaskPlanner.Domain.Entities.Users
{
    /// <summary>
    /// تنظیمات نمایش منوی اصلی برای هر کاربر (یک ردیف به‌ازای هر کاربر).
    /// پیش‌فرض همه بخش‌ها نمایش داده می‌شوند.
    /// </summary>
    public class UserDisplayPreference
    {
        public int Id { get; set; }

        public string UserId { get; set; } = null!;
        public User User { get; set; } = null!;

        public bool ShowLeads { get; set; } = true;
        public bool ShowBoards { get; set; } = true;
        public bool ShowMindMaps { get; set; } = true;
        public bool ShowDesigns { get; set; } = true;

        /// <summary>فقط برای کاربران ADMIN معنا دارد</summary>
        public bool ShowAdminUsers { get; set; } = true;

        /// <summary>فقط برای کاربران ADMIN معنا دارد</summary>
        public bool ShowAdminLeaves { get; set; } = true;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
    }
}
