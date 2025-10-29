using System;
using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// Extension Methods برای IssueType
    /// </summary>
    public static class IssueTypeExtensions
    {
        /// <summary>
        /// دریافت نام فارسی نوع Issue
        /// </summary>
        public static string GetDisplayName(this IssueType issueType)
        {
            return issueType switch
            {
                IssueType.Epic => "اپیک",
                IssueType.Story => "استوری",
                IssueType.Task => "تسک",
                IssueType.Subtask => "زیرتسک",
                IssueType.Bug => "باگ",
                _ => "نامشخص"
            };
        }

        /// <summary>
        /// دریافت آیکون نوع Issue (برای UI)
        /// </summary>
        public static string GetIcon(this IssueType issueType)
        {
            return issueType switch
            {
                IssueType.Epic => "📦",      // Epic
                IssueType.Story => "📝",     // Story
                IssueType.Task => "✅",      // Task
                IssueType.Subtask => "🔹",  // Subtask
                IssueType.Bug => "🐛",       // Bug
                _ => "❓"
            };
        }

        /// <summary>
        /// دریافت رنگ Badge برای UI (Bootstrap classes)
        /// </summary>
        public static string GetBadgeColor(this IssueType issueType)
        {
            return issueType switch
            {
                IssueType.Epic => "bg-dark",      // Epic: تیره (مثل purple در Bootstrap)
                IssueType.Story => "bg-success",   // Story: سبز
                IssueType.Task => "bg-primary",    // Task: آبی
                IssueType.Subtask => "bg-info",    // Subtask: آبی روشن
                IssueType.Bug => "bg-danger",      // Bug: قرمز
                _ => "bg-secondary"                // پیش‌فرض: خاکستری
            };
        }

        /// <summary>
        /// آیا این نوع می‌تونه Parent داشته باشه؟
        /// </summary>
        public static bool CanHaveParent(this IssueType issueType)
        {
            return issueType switch
            {
                IssueType.Epic => false,       // Epic نمی‌تونه parent داشته باشه
                IssueType.Story => true,       // Story می‌تونه Epic parent داشته باشه
                IssueType.Task => true,        // Task می‌تونه Epic parent داشته باشه ✅
                IssueType.Subtask => true,     // Subtask باید parent داشته باشه
                IssueType.Bug => true,         // Bug می‌تونه Epic parent داشته باشه ✅
                _ => false
            };
        }

        /// <summary>
        /// آیا این نوع باید حتماً Parent داشته باشه؟
        /// </summary>
        public static bool MustHaveParent(this IssueType issueType)
        {
            return issueType == IssueType.Subtask;
        }

        /// <summary>
        /// آیا این نوع می‌تونه Child داشته باشه؟
        /// </summary>
        public static bool CanHaveChildren(this IssueType issueType)
        {
            return issueType != IssueType.Subtask;
        }

        /// <summary>
        /// چه نوع Child هایی می‌تونه داشته باشه؟
        /// </summary>
        public static IssueType[] GetAllowedChildTypes(this IssueType issueType)
        {
            return issueType switch
            {
                IssueType.Epic => new[] { IssueType.Story, IssueType.Task, IssueType.Bug }, // Epic می‌تونه Story, Task, Bug داشته باشه ✅
                IssueType.Story => new[] { IssueType.Subtask },
                IssueType.Task => new[] { IssueType.Subtask },
                IssueType.Bug => new[] { IssueType.Subtask },
                IssueType.Subtask => Array.Empty<IssueType>(),
                _ => Array.Empty<IssueType>()
            };
        }

        /// <summary>
        /// چه نوع Parent هایی می‌تونه داشته باشه؟
        /// </summary>
        public static IssueType[] GetAllowedParentTypes(this IssueType issueType)
        {
            return issueType switch
            {
                IssueType.Epic => Array.Empty<IssueType>(),
                IssueType.Story => new[] { IssueType.Epic },
                IssueType.Task => new[] { IssueType.Epic },      // Task می‌تونه Epic parent داشته باشه ✅
                IssueType.Subtask => new[] { IssueType.Story, IssueType.Task, IssueType.Bug },
                IssueType.Bug => new[] { IssueType.Epic },       // Bug می‌تونه Epic parent داشته باشه ✅
                _ => Array.Empty<IssueType>()
            };
        }

        /// <summary>
        /// آیا می‌تونه به Sprint اضافه بشه؟
        /// </summary>
        public static bool CanAddToSprint(this IssueType issueType)
        {
            // Epic نمی‌تونه مستقیماً به Sprint اضافه بشه
            return issueType != IssueType.Epic;
        }

        /// <summary>
        /// آیا Story Points داره؟
        /// </summary>
        public static bool HasStoryPoints(this IssueType issueType)
        {
            return issueType == IssueType.Story || issueType == IssueType.Task;
        }
    }
}

