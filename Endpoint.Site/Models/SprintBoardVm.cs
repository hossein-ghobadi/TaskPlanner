using System.Collections.Generic;
using TaskPlanner.Domain.Entities.TaskPlanner;
using DNTPersianUtils.Core;

namespace Endpoint.Site.Models
{
    public class SprintBoardVm
    {
        public int SprintId { get; set; }
        public string SprintName { get; set; }
        public string? Description { get; set; }
        public string? Goal { get; set; }
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public SprintStatus Status { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }

        public IList<WorkflowStatus> Statuses { get; set; } = new List<WorkflowStatus>();
        public IList<TaskItem> SprintIssues { get; set; } = new List<TaskItem>();

        // Persian date properties
        public string StartDatePersian => StartDate.ToShortPersianDateString();
        public string EndDatePersian => EndDate.ToShortPersianDateString();

        // آمار
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int InProgressTasks { get; set; }
        public int PendingTasks { get; set; }
        public int BlockedTasks { get; set; }
        
        public double CompletionPercentage => TotalTasks > 0 ? (double)CompletedTasks / TotalTasks * 100 : 0;
    }
}




