using System.ComponentModel.DataAnnotations;
using DNTPersianUtils.Core;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Models
{
    public class SprintEditVm
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام اسپرینت الزامی است")]
        [StringLength(200, ErrorMessage = "نام اسپرینت نمی‌تواند بیش از 200 کاراکتر باشد")]
        public string Name { get; set; } = null!;

        [StringLength(1000, ErrorMessage = "توضیحات نمی‌تواند بیش از 1000 کاراکتر باشد")]
        public string? Description { get; set; }

        [StringLength(500, ErrorMessage = "هدف اسپرینت نمی‌تواند بیش از 500 کاراکتر باشد")]
        public string? Goal { get; set; }

        [Required(ErrorMessage = "تاریخ شروع الزامی است")]
        [DataType(DataType.Date)]
        public DateTime StartDate { get; set; }

        [Required(ErrorMessage = "تاریخ پایان الزامی است")]
        [DataType(DataType.Date)]
        public DateTime EndDate { get; set; }

        public bool IsActive { get; set; }
        public bool IsCompleted { get; set; }
        public SprintStatus Status { get; set; }

        [Required]
        public int ProjectId { get; set; }
    }
}