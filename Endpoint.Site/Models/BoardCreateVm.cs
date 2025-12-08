using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class BoardCreateVm
    {
        [Required(ErrorMessage = "نام تخته الزامی است")]
        [Display(Name = "نام تخته")]
        public string Name { get; set; } = null!;

        [Display(Name = "توضیحات")]
        public string? Description { get; set; }

        /// <summary>
        /// اعضای انتخاب‌شده برای دعوت
        /// </summary>
        public List<string> SelectedUserIds { get; set; } = new List<string>();
    }
}

