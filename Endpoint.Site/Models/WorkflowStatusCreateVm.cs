using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Areas.TaskPlanner.Models
{
    public class WorkflowStatusCreateVm
    {
        [Required(ErrorMessage = "نام وضعیت الزامی است")]
        [Display(Name = "نام وضعیت")]
        [MaxLength(100)]
        public string Name { get; set; } = null!;

        [Display(Name = "توضیحات")]
        public string? Description { get; set; }

        [Display(Name = "رنگ")]
        [MaxLength(20)]
        public string? Color { get; set; }

        [Display(Name = "ترتیب نمایش")]
        public int Order { get; set; }

        [Required(ErrorMessage = "پروژه الزامی است")]
        [Display(Name = "پروژه")]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "نوع وضعیت الزامی است")]
        [Display(Name = "نوع وضعیت")]
        public string Type { get; set; } = "Todo";
    }
}

