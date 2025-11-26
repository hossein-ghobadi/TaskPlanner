using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// IssueType سفارشی مخصوص هر پروژه (Story-level)
    /// </summary>
    public class ProjectIssueType
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        /// <summary>
        /// نام نمایشی
        /// </summary>
        [Required]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        /// <summary>
        /// توضیح اختیاری
        /// </summary>
        [StringLength(500)]
        public string? Description { get; set; }

        /// <summary>
        /// آیکون (Emoji یا کلاس)
        /// </summary>
        [StringLength(32)]
        public string? Icon { get; set; }

        /// <summary>
        /// رنگ هگز یا کلاس CSS
        /// </summary>
        [StringLength(32)]
        public string? Color { get; set; }

        /// <summary>
        /// ترتیب نمایش
        /// </summary>
        public int Order { get; set; }

        /// <summary>
        /// نوع پایه‌ای (برای منطق و تطابق با enum فعلی)
        /// </summary>
        public IssueType BaseType { get; set; } = IssueType.Task;

        /// <summary>
        /// آیا کاربر ایجاد کرده یا پیش‌فرض سیستم است؟
        /// </summary>
        public bool IsCustom { get; set; } = true;

        /// <summary>
        /// اجازه ورود به Sprint
        /// </summary>
        public bool CanAddToSprint { get; set; } = true;

        /// <summary>
        /// اجازه داشتن Subtask
        /// </summary>
        public bool CanHaveChildren { get; set; } = true;

        /// <summary>
        /// در گزارش‌ها/Velocity لحاظ شود
        /// </summary>
        public bool IncludeInReports { get; set; } = true;

        /// <summary>
        /// سطح Type (Epic, StoryLevel, Subtask)
        /// </summary>
        public IssueTypeLevel Level { get; set; } = IssueTypeLevel.StoryLevel;

        /// <summary>
        /// کاربری که این نوع را ساخته
        /// </summary>
        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }

    public enum IssueTypeLevel
    {
        Epic = 1,
        StoryLevel = 2,
        Subtask = 3
    }
}

