using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Models
{
    public enum TaskEffortLevel
    {
        Simple = 1,
        Medium = 2,
        Hard = 3
    }

    public static class TaskEffortLevelMapper
    {
        public static int? ToStoryPoints(TaskEffortLevel? level)
        {
            return level switch
            {
                TaskEffortLevel.Simple => 1,
                TaskEffortLevel.Medium => 5,
                TaskEffortLevel.Hard => 13,
                _ => null
            };
        }

        public static TaskEffortLevel? FromStoryPoints(int? storyPoints)
        {
            if (!storyPoints.HasValue)
            {
                return null;
            }

            return storyPoints.Value switch
            {
                <= 3 => TaskEffortLevel.Simple,
                <= 8 => TaskEffortLevel.Medium,
                _ => TaskEffortLevel.Hard
            };
        }
    }

    public class TaskCreateVm
    {
        [Required(ErrorMessage = "عنوان الزامی است")]
        public string Title { get; set; }

        public string? Description { get; set; }

        /// <summary>
        /// نوع Issue (مثل Jira)
        /// </summary>
        [Required(ErrorMessage = "نوع Issue الزامی است")]
        public IssueType IssueType { get; set; } = IssueType.Task;

        [Required(ErrorMessage = "تاریخ شروع الزامی است")]
        public string StartDateSh { get; set; }   // تاریخ شمسی به صورت متن

        public string? DueDateSh { get; set; }    // تاریخ شمسی به صورت متن

        // دسته‌بندی (Component) - اختیاری مثل Jira
        public int? CategoryId { get; set; }

        /// <summary>
        /// Parent Issue (برای Story باید Epic باشه، برای Subtask باید Task/Story/Bug باشه)
        /// </summary>
        public int? ParentId { get; set; }

        [Required(ErrorMessage = "انتخاب پروژه الزامی است")]
        public int ProjectId { get; set; }

        // کاربر مسئول
        public string? AssignedUserId { get; set; }

        /// <summary>
        /// شناسه IssueType سفارشی پروژه (برای Story-level)
        /// </summary>
        public int? ProjectIssueTypeId { get; set; }

        /// <summary>
        /// Story Points (فقط برای Story و Task)
        /// </summary>
        public int? StoryPoints { get; set; }

        /// <summary>
        /// سطح سختی/زحمت تسک (ساده، متوسط، پرزحمت)
        /// </summary>
        public TaskEffortLevel? EffortLevel { get; set; }
    }
}
