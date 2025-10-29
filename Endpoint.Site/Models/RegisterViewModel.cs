using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "نام کامل الزامی است")]
        [Display(Name = "نام کامل")]
        [StringLength(100, ErrorMessage = "نام کامل نباید بیشتر از 100 کاراکتر باشد")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "ایمیل الزامی است")]
        [EmailAddress(ErrorMessage = "ایمیل معتبر نیست")]
        [Display(Name = "ایمیل")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز عبور الزامی است")]
        [StringLength(100, ErrorMessage = "رمز عبور باید حداقل {2} و حداکثر {1} کاراکتر باشد", MinimumLength = 6)]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تکرار رمز عبور الزامی است")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "رمز عبور و تکرار آن مطابقت ندارند")]
        [Display(Name = "تکرار رمز عبور")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Display(Name = "شماره تلفن")]
        [Phone(ErrorMessage = "شماره تلفن معتبر نیست")]
        public string? Phone { get; set; }
    }
}
