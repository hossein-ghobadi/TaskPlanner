namespace Endpoint.Site.Models
{
    public class ProjectStatisticsVm
    {
        public List<ProjectStatisticItem> Projects { get; set; } = new();
        public List<ProjectOption> ProjectOptions { get; set; } = new();
    }

    public class ProjectStatisticItem
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = null!;
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }
        public int OverdueTasks { get; set; }
        public int RemainingTasks { get; set; }
    }

    public class ProjectOption
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
    }

    public class TaskTimeSeriesData
    {
        public string Date { get; set; } = null!;
        public Dictionary<int, int> ProjectTaskCounts { get; set; } = new();
    }
}

