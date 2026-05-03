using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Models
{
    public class CategoryIndexVm
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public IReadOnlyList<TaskCategory> Categories { get; set; } = Array.Empty<TaskCategory>();
    }
}
