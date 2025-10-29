using System;
using System.Linq;
using System.Text.RegularExpressions;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// Generator برای ساخت IssueKey منحصر به فرد (مثل PROJ-123 در Jira)
    /// </summary>
    public static class IssueKeyGenerator
    {
        /// <summary>
        /// ساخت IssueKey از نام پروژه
        /// مثلاً: "پروژه تابلو" -> "PROJ-1"
        /// مثلاً: "Task Planner System" -> "TPS-1"
        /// </summary>
        public static string Generate(string projectName, int issueNumber)
        {
            var prefix = GeneratePrefix(projectName);
            return $"{prefix}-{issueNumber}";
        }

        /// <summary>
        /// تولید Prefix از نام پروژه
        /// </summary>
        public static string GeneratePrefix(string projectName)
        {
            if (string.IsNullOrWhiteSpace(projectName))
                return "PROJ";

            // حذف کاراکترهای غیرضروری
            var cleaned = Regex.Replace(projectName, @"[^\w\s]", "");

            // جدا کردن کلمات
            var words = cleaned.Split(new[] { ' ', '_', '-' }, StringSplitOptions.RemoveEmptyEntries);

            if (words.Length == 0)
                return "PROJ";

            // اگر یک کلمه است
            if (words.Length == 1)
            {
                var word = words[0];
                // اگر فارسی است، از حروف اول استفاده کن
                if (IsPersian(word))
                {
                    return "PROJ";
                }
                // اگر انگلیسی است، حداکثر 4 حرف اول
                return word.Length <= 4
                    ? word.ToUpper()
                    : word.Substring(0, 4).ToUpper();
            }

            // اگر چند کلمه است، حرف اول هر کلمه
            var initials = string.Join("",
                words.Take(4).Select(w => w.Length > 0 ? w[0].ToString() : ""));

            return string.IsNullOrEmpty(initials)
                ? "PROJ"
                : initials.ToUpper();
        }

        /// <summary>
        /// بررسی اینکه آیا متن فارسی است
        /// </summary>
        private static bool IsPersian(string text)
        {
            return text.Any(c => c >= 0x0600 && c <= 0x06FF);
        }

        /// <summary>
        /// Parse کردن IssueKey و استخراج شماره
        /// مثلاً: "PROJ-123" -> 123
        /// </summary>
        public static int? ParseNumber(string issueKey)
        {
            if (string.IsNullOrWhiteSpace(issueKey))
                return null;

            var match = Regex.Match(issueKey, @"-(\d+)$");
            if (match.Success && int.TryParse(match.Groups[1].Value, out int number))
            {
                return number;
            }

            return null;
        }

        /// <summary>
        /// استخراج Prefix از IssueKey
        /// مثلاً: "PROJ-123" -> "PROJ"
        /// </summary>
        public static string? ParsePrefix(string issueKey)
        {
            if (string.IsNullOrWhiteSpace(issueKey))
                return null;

            var match = Regex.Match(issueKey, @"^([A-Z]+)-\d+$");
            if (match.Success)
            {
                return match.Groups[1].Value;
            }

            return null;
        }

        /// <summary>
        /// Validation برای IssueKey
        /// </summary>
        public static bool IsValid(string issueKey)
        {
            if (string.IsNullOrWhiteSpace(issueKey))
                return false;

            // فرمت: PREFIX-NUMBER (مثل PROJ-123)
            return Regex.IsMatch(issueKey, @"^[A-Z]{2,10}-\d+$");
        }
    }
}

