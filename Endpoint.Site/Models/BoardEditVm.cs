using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class BoardEditVm
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام تخته الزامی است")]
        [Display(Name = "نام تخته")]
        public string Name { get; set; } = null!;

        [Display(Name = "توضیحات")]
        public string? Description { get; set; }
    }
}

