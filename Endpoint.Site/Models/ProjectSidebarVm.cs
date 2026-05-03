namespace Endpoint.Site.Models
{
    public class ProjectSidebarVm
    {
        public int ProjectId { get; set; }
        public bool IsCreator { get; set; }
        public bool OnProjectDetailsPage { get; set; }
        public string ActiveNav { get; set; } = "";

        public int? TotalTasks { get; set; }
        public int? NotesCount { get; set; }
        public int? ImagesCount { get; set; }
        public int? PendingInvitesCount { get; set; }
        public int? MemberCount { get; set; }
    }
}
