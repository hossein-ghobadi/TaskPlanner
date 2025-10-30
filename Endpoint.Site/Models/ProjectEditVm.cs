using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class ProjectEditVm
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام پروژه الزامی است")]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        // اعضای انتخاب‌شده
        public List<string> SelectedUserIds { get; set; } = new List<string>();
    }
}
