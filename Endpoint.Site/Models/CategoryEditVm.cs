using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class CategoryEditVm
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام دسته‌بندی الزامی است")]
        [StringLength(200, ErrorMessage = "نام دسته‌بندی نباید بیشتر از 200 کاراکتر باشد")]
        public string Name { get; set; } = string.Empty;
    }
}

