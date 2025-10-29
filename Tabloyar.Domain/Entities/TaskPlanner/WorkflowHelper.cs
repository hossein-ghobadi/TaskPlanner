using System.Collections.Generic;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// Helper برای ایجاد Workflow و Transitions پیش‌فرض
    /// </summary>
    public static class WorkflowHelper
    {
        /// <summary>
        /// ایجاد Workflow پیش‌فرض برای یک پروژه جدید
        /// شامل: To Do, In Progress, Done + Transitions بین آنها
        /// </summary>
        public static (List<WorkflowStatus> statuses, List<WorkflowTransition> transitions) CreateDefaultWorkflow(int projectId)
        {
            var statuses = new List<WorkflowStatus>();
            var transitions = new List<WorkflowTransition>();

            // ✅ 1. ایجاد Statuses پیش‌فرض (مثل Jira)
            var todoStatus = new WorkflowStatus
            {
                Name = "باید انجام شود",
                Description = "Issue ها که باید شروع بشن",
                Color = "#6c757d", // Gray
                Order = 1,
                ProjectId = projectId,
                Type = WorkflowType.Todo,
                IsDefault = true, // Status پیش‌فرض برای Issue های جدید
                IsFinal = false
            };

            var inProgressStatus = new WorkflowStatus
            {
                Name = "در حال انجام",
                Description = "Issue های در دست اقدام",
                Color = "#0d6efd", // Blue
                Order = 2,
                ProjectId = projectId,
                Type = WorkflowType.InProgress,
                IsDefault = false,
                IsFinal = false
            };

            var doneStatus = new WorkflowStatus
            {
                Name = "تکمیل شده",
                Description = "Issue های تکمیل شده",
                Color = "#198754", // Green
                Order = 3,
                ProjectId = projectId,
                Type = WorkflowType.Done,
                IsDefault = false,
                IsFinal = true // Issue ها به این Status که برسن، Done میشن
            };

            var blockedStatus = new WorkflowStatus
            {
                Name = "مسدود شده",
                Description = "Issue های مسدود (منتظر وابستگی)",
                Color = "#dc3545", // Red
                Order = 4,
                ProjectId = projectId,
                Type = WorkflowType.Blocked,
                IsDefault = false,
                IsFinal = false
            };

            statuses.Add(todoStatus);
            statuses.Add(inProgressStatus);
            statuses.Add(doneStatus);
            statuses.Add(blockedStatus);

            // Note: برای اینکه بتونیم ID ها رو برای Transition استفاده کنیم،
            // باید بعد از SaveChanges این متد رو دوباره صدا بزنیم
            // یا از placeholder استفاده کنیم که بعداً جایگزین میشه

            return (statuses, transitions);
        }

        /// <summary>
        /// ایجاد Transitions پیش‌فرض بعد از ایجاد Statuses
        /// این متد باید بعد از SaveChanges (وقتی Status ها ID گرفتن) صدا زده بشه
        /// </summary>
        public static List<WorkflowTransition> CreateDefaultTransitions(
            int projectId,
            int todoStatusId,
            int inProgressStatusId,
            int doneStatusId,
            int blockedStatusId)
        {
            var transitions = new List<WorkflowTransition>();

            // ✅ To Do → In Progress
            transitions.Add(new WorkflowTransition
            {
                Name = "شروع کار",
                Description = "شروع کار روی Issue",
                FromStatusId = todoStatusId,
                ToStatusId = inProgressStatusId,
                ProjectId = projectId,
                IsActive = true,
                Order = 1,
                OnlyAssigneeCanTransition = false
            });

            // ✅ To Do → Blocked
            transitions.Add(new WorkflowTransition
            {
                Name = "مسدود کردن",
                Description = "مسدود کردن Issue (قبل از شروع)",
                FromStatusId = todoStatusId,
                ToStatusId = blockedStatusId,
                ProjectId = projectId,
                IsActive = true,
                Order = 2
            });

            // ✅ In Progress → Done
            transitions.Add(new WorkflowTransition
            {
                Name = "تکمیل",
                Description = "تکمیل Issue",
                FromStatusId = inProgressStatusId,
                ToStatusId = doneStatusId,
                ProjectId = projectId,
                IsActive = true,
                Order = 1,
                OnlyAssigneeCanTransition = true // فقط Assignee می‌تونه Done کنه
            });

            // ✅ In Progress → Blocked
            transitions.Add(new WorkflowTransition
            {
                Name = "مسدود شد",
                Description = "مسدود شدن Issue (در حین کار)",
                FromStatusId = inProgressStatusId,
                ToStatusId = blockedStatusId,
                ProjectId = projectId,
                IsActive = true,
                Order = 2
            });

            // ✅ In Progress → To Do (Reopen)
            transitions.Add(new WorkflowTransition
            {
                Name = "بازگشت",
                Description = "بازگشت به To Do",
                FromStatusId = inProgressStatusId,
                ToStatusId = todoStatusId,
                ProjectId = projectId,
                IsActive = true,
                Order = 3
            });

            // ✅ Blocked → To Do (Unblock)
            transitions.Add(new WorkflowTransition
            {
                Name = "رفع انسداد",
                Description = "برطرف شدن انسداد",
                FromStatusId = blockedStatusId,
                ToStatusId = todoStatusId,
                ProjectId = projectId,
                IsActive = true,
                Order = 1
            });

            // ✅ Blocked → In Progress
            transitions.Add(new WorkflowTransition
            {
                Name = "ادامه کار",
                Description = "ادامه کار (بعد از رفع انسداد)",
                FromStatusId = blockedStatusId,
                ToStatusId = inProgressStatusId,
                ProjectId = projectId,
                IsActive = true,
                Order = 2
            });

            // ✅ Done → To Do (Reopen)
            transitions.Add(new WorkflowTransition
            {
                Name = "باز کردن مجدد",
                Description = "بازگشایی Issue تکمیل شده",
                FromStatusId = doneStatusId,
                ToStatusId = todoStatusId,
                ProjectId = projectId,
                IsActive = true,
                Order = 1,
                RequiresApproval = true // نیاز به تایید داره
            });

            return transitions;
        }
    }
}


