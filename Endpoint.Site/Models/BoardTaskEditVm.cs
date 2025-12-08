using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class BoardTaskEditVm
    {
        public int Id { get; set; }

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
        /// ترتیب نمایش
        /// </summary>
        [Display(Name = "ترتیب")]
        public int Order { get; set; } = 0;
    }
}

