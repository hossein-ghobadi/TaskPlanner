namespace Endpoint.Site.Models
{
    public class AppSidebarVm
    {
        public string? AntiforgeryToken { get; set; }
        public string? UserName { get; set; }
        public bool IsAdmin { get; set; }

        public bool ShowLeads { get; set; } = true;
        public bool ShowBoards { get; set; } = true;
        public bool ShowMindMaps { get; set; } = true;
        public bool ShowDesigns { get; set; } = true;
        public bool ShowAdminUsers { get; set; }
        public bool ShowAdminLeaves { get; set; }
    }
}
