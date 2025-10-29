using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Areas.TaskPlanner.Models
{
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
        /// Story Points (فقط برای Story و Task)
        /// </summary>
        public int? StoryPoints { get; set; }
    }
}
