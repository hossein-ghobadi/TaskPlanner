using System.Globalization;

namespace TaskPlanner.Application.Helpers
{
    public static class PersianDateTimeHelper
    {
        public static string ToLatinDigits(string value)
        {
            return value
                .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
                .Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9')
                .Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
                .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9');
        }

        public static DateTime ParsePersianDate(string persianDate)
        {
            var normalized = ToLatinDigits(persianDate.Trim())
                .Replace('-', '/')
                .Replace('\\', '/');

            var parts = normalized.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 3)
                throw new InvalidOperationException("فرمت تاریخ معتبر نیست. نمونه صحیح: 1404/05/01");

            var year = int.Parse(parts[0], CultureInfo.InvariantCulture);
            var month = int.Parse(parts[1], CultureInfo.InvariantCulture);
            var day = int.Parse(parts[2], CultureInfo.InvariantCulture);

            var persianCalendar = new PersianCalendar();
            var localDate = persianCalendar.ToDateTime(year, month, day, 0, 0, 0, 0);
            return DateTime.SpecifyKind(localDate, DateTimeKind.Unspecified);
        }

        public static DateTime ParsePersianDateTime(string persianDateTime)
        {
            var normalized = ToLatinDigits(persianDateTime.Trim())
                .Replace('-', '/')
                .Replace('٫', ':')
                .Replace('،', ':');

            var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                throw new InvalidOperationException("فرمت زمان معتبر نیست. نمونه صحیح: 1404/05/01 10:30");

            var dateParts = parts[0].Split('/');
            var timeParts = parts[1].Split(':');
            if (dateParts.Length != 3 || timeParts.Length < 2)
                throw new InvalidOperationException("فرمت زمان معتبر نیست. نمونه صحیح: 1404/05/01 10:30");

            var year = int.Parse(dateParts[0], CultureInfo.InvariantCulture);
            var month = int.Parse(dateParts[1], CultureInfo.InvariantCulture);
            var day = int.Parse(dateParts[2], CultureInfo.InvariantCulture);
            var hour = int.Parse(timeParts[0], CultureInfo.InvariantCulture);
            var minute = int.Parse(timeParts[1], CultureInfo.InvariantCulture);

            var persianCalendar = new PersianCalendar();
            var localTime = persianCalendar.ToDateTime(year, month, day, hour, minute, 0, 0);
            return DateTime.SpecifyKind(localTime, DateTimeKind.Unspecified);
        }

        public static string FormatPersianDate(DateTime dateTime)
        {
            var persianCalendar = new PersianCalendar();
            var year = persianCalendar.GetYear(dateTime);
            var month = persianCalendar.GetMonth(dateTime);
            var day = persianCalendar.GetDayOfMonth(dateTime);
            return $"{year:0000}/{month:00}/{day:00}";
        }

        public static string FormatPersianDateTime(DateTime dateTime)
        {
            var persianCalendar = new PersianCalendar();
            var year = persianCalendar.GetYear(dateTime);
            var month = persianCalendar.GetMonth(dateTime);
            var day = persianCalendar.GetDayOfMonth(dateTime);
            var hour = persianCalendar.GetHour(dateTime);
            var minute = persianCalendar.GetMinute(dateTime);
            return $"{year:0000}/{month:00}/{day:00} {hour:00}:{minute:00}";
        }
    }
}
