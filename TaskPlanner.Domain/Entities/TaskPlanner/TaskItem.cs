using Microsoft.AspNetCore.Identity;
using System;
using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Users;
using DNTPersianUtils.Core;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// TaskItem (Issue) - مدل اصلی تسک/ایشو مثل Jira
    /// می‌تونه Epic, Story, Task, Subtask یا Bug باشه
    /// </summary>
    public class TaskItem
    {
        public int Id { get; set; }

        /// <summary>
        /// کلید یونیک Issue (مثل PROJ-123 در Jira)
        /// این فیلد اتوماتیک generate میشه
        /// </summary>
        [StringLength(50)]
        public string? IssueKey { get; set; }

        [Required(ErrorMessage = "عنوان الزامی است")]
        [StringLength(500)]
        public string Title { get; set; }

        public string? Description { get; set; }

        /// <summary>
        /// نوع Issue (مثل Jira)
        /// Epic > Story > Task/Bug > Subtask
        /// </summary>
        [Required]
        public IssueType IssueType { get; set; } = IssueType.Task;

        public DateTime? StartDate { get; set; }

        public DateTime? DueDate { get; set; }

        /// <summary>
        /// مدت انجام کار به روز (از تاریخ شروع تا مهلت)
        /// </summary>
        public int? DurationDays { get; set; }

        public bool IsCompleted { get; set; }

        // اولویت
        public TaskPriority Priority { get; set; } = TaskPriority.Medium;

        /// <summary>
        /// Story Points - برای estimation (فقط برای Story/Task)
        /// </summary>
        public int? StoryPoints { get; set; }

        /// <summary>
        /// تخمین اولیه زمان (به ساعت)
        /// </summary>
        public decimal? OriginalEstimateHours { get; set; }

        /// <summary>
        /// زمان صرف شده (به ساعت)
        /// </summary>
        public decimal? TimeSpentHours { get; set; }

        /// <summary>
        /// زمان باقیمانده (به ساعت)
        /// </summary>
        public decimal? RemainingTimeHours { get; set; }

        // دسته‌بندی (مثل Component در Jira - اختیاری)
        // مثلاً: Frontend, Backend, Database, Testing
        public int? CategoryId { get; set; }
        public TaskCategory? Category { get; set; }

        /// <summary>
        /// وضعیت فعلی Issue در Workflow (مثل To Do, In Progress, Done)
        /// </summary>
        public int? StatusId { get; set; }
        public WorkflowStatus? Status { get; set; }

        /// <summary>
        /// تاریخچه تغییرات Status
        /// </summary>
        public ICollection<IssueStatusHistory> StatusHistory { get; set; } = new List<IssueStatusHistory>();

        // کاربر مسئول
        public string? AssignedUserId { get; set; }
        public User? AssignedUser { get; set; }

        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; }

        // اسپرینت (اختیاری - Epic نمی‌تونه مستقیماً در Sprint باشه)
        public int? SprintId { get; set; }
        public Sprint? Sprint { get; set; }

        // وضعیت چرخه کاری (Workflow Status)
        public int? WorkflowStatusId { get; set; }
        public WorkflowStatus? WorkflowStatus { get; set; }

        /// <summary>
        /// IssueType پروژه (برای Story-level سفارشی)
        /// </summary>
        public int? ProjectIssueTypeId { get; set; }
        public ProjectIssueType? ProjectIssueType { get; set; }

        /// <summary>
        /// Parent Issue (برای سلسله مراتب)
        /// - Story می‌تونه Parent Epic داشته باشه
        /// - Subtask باید Parent (Story/Task/Bug) داشته باشه
        /// </summary>
        public int? ParentTaskId { get; set; }
        public TaskItem? ParentTask { get; set; }

        /// <summary>
        /// Child Issues
        /// - Epic می‌تونه Story‌ها رو داشته باشه
        /// - Story/Task/Bug می‌تونن Subtask داشته باشن
        /// </summary>
        public ICollection<TaskItem> ChildIssues { get; set; } = new List<TaskItem>();

        // Relation با Sprint (Many-to-Many via SprintTask)
        public ICollection<SprintTask> SprintTasks { get; set; } = new List<SprintTask>();

        // Audit Fields
        public string? CreatedByUserId { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Properties for Views
        public string WorkflowStatusName => WorkflowStatus?.Name ?? "بدون وضعیت";
        public string WorkflowStatusColor => WorkflowStatus?.Color ?? "#6c757d";
        public string DueDatePersian => DueDate?.ToShortPersianDateString() ?? "تعیین نشده";
        public string AssignedUserName => AssignedUser?.UserName ?? "تخصیص نیافته";
        public string IssueTypeName => ProjectIssueType?.Name ?? IssueType.GetDisplayName();
        public string IssueTypeIcon => ProjectIssueType != null && !string.IsNullOrWhiteSpace(ProjectIssueType.Icon) ? ProjectIssueType.Icon : IssueType.GetIcon();
        public string CategoryName => Category?.Name ?? "بدون دسته";

        /// <summary>
        /// آیا این Issue می‌تونه child داشته باشه؟
        /// فقط Subtask نمی‌تونه child داشته باشه
        /// </summary>
        public bool CanHaveChildren => ProjectIssueType?.CanHaveChildren ?? (IssueType != IssueType.Subtask);

        /// <summary>
        /// آیا این Issue می‌تونه به Sprint اضافه بشه؟
        /// تنها Story / Task / Bug قابل افزودن به Sprint هستند.
        /// </summary>
        public bool CanAddToSprint => ProjectIssueType?.CanAddToSprint ?? (IssueType == IssueType.Story || IssueType == IssueType.Task || IssueType == IssueType.Bug);

        /// <summary>
        /// Progress بر اساس Child Issues (برای Epic/Story)
        /// </summary>
        public double ChildProgressPercentage
        {
            get
            {
                if (!ChildIssues.Any()) return 0;
                var completed = ChildIssues.Count(c => c.IsCompleted);
                return (double)completed / ChildIssues.Count * 100;
            }
        }
    }

    /// <summary>
    /// نوع Issue (مثل Jira)
    /// </summary>
    public enum IssueType
    {
        /// <summary>
        /// Epic - بزرگترین واحد کاری
        /// مثلاً: "پیاده‌سازی سیستم احراز هویت"
        /// </summary>
        Epic = 1,

        /// <summary>
        /// Story - داستان کاربری (User Story)
        /// زیرمجموعه Epic
        /// مثلاً: "به عنوان کاربر می‌خواهم بتوانم وارد سیستم شوم"
        /// </summary>
        Story = 2,

        /// <summary>
        /// Task - کار عادی/تکنیکال
        /// می‌تونه standalone باشه یا زیر Story
        /// مثلاً: "راه‌اندازی سرور CI/CD"
        /// </summary>
        Task = 3,

        /// <summary>
        /// Subtask - زیرکار
        /// باید Parent داشته باشه (Story/Task/Bug)
        /// نمی‌تونه خودش child داشته باشه
        /// مثلاً: "نوشتن unit test برای login"
        /// </summary>
        Subtask = 4,

        /// <summary>
        /// Bug - باگ یا مشکل
        /// می‌تونه standalone باشه
        /// مثلاً: "دکمه ورود در موبایل کار نمی‌کند"
        /// </summary>
        Bug = 5
    }

    public enum TaskPriority
    {
        Low = 1,        // کم
        Medium = 2,     // متوسط
        High = 3,       // بالا
        Critical = 4    // بحرانی
    }
}
