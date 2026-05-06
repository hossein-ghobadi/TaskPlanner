namespace Endpoint.Site.Models
{
    public class ProjectsIndexVm
    {
        public string CurrentUserId { get; set; } = string.Empty;
        public List<ProjectCardVm> Projects { get; set; } = new();
        public Dictionary<int, int> ActiveSprintByProject { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalProjectsCount { get; set; }
        public bool HasMoreProjects { get; set; }
        public int PendingInviteCount { get; set; }
        public int CollaboratorCount { get; set; }
        public int RecentNoteCount { get; set; }
    }

    public class ProjectCardVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CreatorUserId { get; set; } = string.Empty;
        public int TaskCount { get; set; }
    }
}
