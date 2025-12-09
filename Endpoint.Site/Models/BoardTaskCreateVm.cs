using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class BoardTaskCreateVm
    {
        [Required]
        public int BoardId { get; set; }

        [Required(ErrorMessage = "عنوان کار الزامی است")]
        [Display(Name = "عنوان کار")]
        [StringLength(500)]
        public string Title { get; set; } = null!;

        [Display(Name = "توضیحات")]
        public string? Description { get; set; }

        [Display(Name = "وضعیت")]
        [StringLength(50)]
        public string Status { get; set; } = "To Do";

        /// <summary>
        /// کاربر اختصاص‌یافته (اختیاری)
        /// </summary>
        [Display(Name = "اختصاص به")]
        public string? AssignedUserId { get; set; }

        /// <summary>
        /// تاریخ سررسید (اختیاری)
        /// </summary>
        [Display(Name = "تاریخ سررسید")]
        public DateTime? DueDate { get; set; }

        /// <summary>
        /// کار والد (برای ایجاد کارک)
        /// </summary>
        [Display(Name = "کار والد")]
        public int? ParentTaskId { get; set; }
    }
}

