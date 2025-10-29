using System.Collections.Generic;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Areas.TaskPlanner.Models
{
    public class SprintBoardVm
    {
        public int SprintId { get; set; }
        public string SprintName { get; set; }
        public int ProjectId { get; set; }
        public string ProjectName { get; set; }

        public IList<WorkflowStatus> Statuses { get; set; } = new List<WorkflowStatus>();
        public IList<TaskItem> SprintIssues { get; set; } = new List<TaskItem>();
    }
}




