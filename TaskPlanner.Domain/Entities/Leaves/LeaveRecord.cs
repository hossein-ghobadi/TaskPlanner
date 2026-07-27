using System;
using TaskPlanner.Domain.Entities.Users;

namespace TaskPlanner.Domain.Entities.Leaves
{
    public class LeaveRecord
    {
        public int Id { get; set; }

        /// <summary>همکاری که مرخصی برای او ثبت شده</summary>
        public string UserId { get; set; } = null!;
        public User User { get; set; } = null!;

        public int LeaveTypeId { get; set; }
        public LeaveType LeaveType { get; set; } = null!;

        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        /// <summary>تعداد روز مرخصی مؤثر (بدون تعطیلات) — فقط مرخصی روزانه</summary>
        public int? DurationDays { get; set; }
        public double DurationHours { get; set; }
        public string? Note { get; set; }

        /// <summary>ادمین ثبت‌کننده — کلید ایزولاسیون دید</summary>
        public string CreatedByUserId { get; set; } = null!;
        public User CreatedByUser { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }
        public string? UpdatedByUserId { get; set; }

        public bool IsDeleted { get; set; } = false;
    }
}
