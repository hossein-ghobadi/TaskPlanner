namespace Endpoint.Site.Models
{
    public class AppSidebarVm
    {
        public string? AntiforgeryToken { get; set; }
        public string? UserName { get; set; }
        public bool IsAdmin { get; set; }
    }
}
