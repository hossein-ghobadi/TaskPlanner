namespace Endpoint.Site.Models
{
    public class ProjectSettingsVm
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public bool IsCreator { get; set; }
    }
}
