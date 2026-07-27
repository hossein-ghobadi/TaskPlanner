namespace TaskPlanner.Application.Helpers
{
    public static class IranDateTimeHelper
    {
        public static TimeZoneInfo IranTimeZone =>
            TimeZoneInfo.FindSystemTimeZoneById(
                OperatingSystem.IsWindows() ? "Iran Standard Time" : "Asia/Tehran");

        /// <summary>
        /// زمان‌های مرخصی (شروع/پایان) به‌صورت ساعت تقویم ایران در DB ذخیره می‌شوند.
        /// </summary>
        public static DateTime AsIranWallClock(DateTime value) =>
            DateTime.SpecifyKind(value, DateTimeKind.Unspecified);

        /// <summary>
        /// تبدیل زمان UTC (مثل CreatedAt) به وقت ایران.
        /// </summary>
        public static DateTime FromUtcToIran(DateTime value)
        {
            var utc = value.Kind switch
            {
                DateTimeKind.Utc => value,
                DateTimeKind.Local => value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(value, DateTimeKind.Utc)
            };

            return TimeZoneInfo.ConvertTimeFromUtc(utc, IranTimeZone);
        }

        public static string FormatLeaveDateTime(DateTime value) =>
            PersianDateTimeHelper.FormatPersianDateTime(AsIranWallClock(value));

        public static string FormatLeaveDate(DateTime value) =>
            PersianDateTimeHelper.FormatPersianDate(AsIranWallClock(value));
    }
}
