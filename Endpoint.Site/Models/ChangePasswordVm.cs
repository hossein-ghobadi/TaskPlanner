using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class ChangePasswordVm
    {
        [Required(ErrorMessage = "شناسه کاربر الزامی است")]
        public string UserId { get; set; } = "";

        [Display(Name = "نام کاربری")]
        public string UserName { get; set; } = "";

        [Display(Name = "نام کامل")]
        public string FullName { get; set; } = "";

        [Required(ErrorMessage = "رمز عبور جدید الزامی است")]
        [StringLength(100, ErrorMessage = "رمز عبور باید حداقل {2} کاراکتر باشد", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور جدید")]
        public string NewPassword { get; set; } = "";

        [Required(ErrorMessage = "تکرار رمز عبور الزامی است")]
        [DataType(DataType.Password)]
        [Display(Name = "تکرار رمز عبور")]
        [Compare("NewPassword", ErrorMessage = "رمز عبور و تکرار آن مطابقت ندارند")]
        public string ConfirmPassword { get; set; } = "";
    }
}

