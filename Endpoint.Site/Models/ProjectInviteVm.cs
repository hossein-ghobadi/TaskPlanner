using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Areas.TaskPlanner.Models
{
    public class ProjectInviteVm
    {
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "شماره موبایل الزامی است")]
        public string Phone { get; set; } = null!;
    }
}
