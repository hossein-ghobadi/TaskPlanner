using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskPlanner.Domain.Entities.Users;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    public class Project
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام پروژه الزامی است")]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        /// <summary>
        /// Prefix برای IssueKey (مثل PROJ در PROJ-123)
        /// اتوماتیک از نام پروژه generate میشه
        /// </summary>
        [Required]
        [StringLength(10)]
        public string IssueKeyPrefix { get; set; } = "PROJ";

        /// <summary>
        /// آخرین شماره Issue که استفاده شده
        /// برای generate کردن IssueKey بعدی
        /// </summary>
        public int LastIssueNumber { get; set; } = 0;

        // کاربر ایجادکننده
        [Required]
        [StringLength(450)]
        public string CreatorUserId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // اعضای پروژه
        public ICollection<ProjectMember> Members { get; set; } = new List<ProjectMember>();
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();

        // اسپرینت‌های پروژه
        public ICollection<Sprint> Sprints { get; set; } = new List<Sprint>();

        // وضعیت‌های چرخه کاری پروژه
        public ICollection<WorkflowStatus> WorkflowStatuses { get; set; } = new List<WorkflowStatus>();

        // انتقالات (Transitions) بین وضعیت‌ها
        public ICollection<WorkflowTransition> WorkflowTransitions { get; set; } = new List<WorkflowTransition>();

        // دسته‌بندی‌های پروژه
        public ICollection<TaskCategory> Categories { get; set; } = new List<TaskCategory>();

        /// <summary>
        /// IssueType های سفارشی پروژه
        /// </summary>
        public ICollection<ProjectIssueType> IssueTypes { get; set; } = new List<ProjectIssueType>();

        /// <summary>
        /// Generate کردن IssueKey بعدی برای این پروژه
        /// مثلاً: PROJ-1, PROJ-2, ...
        /// </summary>
        public string GenerateNextIssueKey()
        {
            LastIssueNumber++;
            return $"{IssueKeyPrefix}-{LastIssueNumber}";
        }
    }
}
