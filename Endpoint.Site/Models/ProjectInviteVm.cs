using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class ProjectInviteVm
    {
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "شماره موبایل الزامی است")]
        public string Phone { get; set; } = null!;
    }
}
