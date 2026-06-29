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
        [StringLength(100, ErrorMessage = "رمز عبور باید حداقل {2} و حداکثر {1} کاراکتر باشد", MinimumLength = 8)]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور")]
        public string Password { get; set; } = string.Empty;

        [Required(ErrorMessage = "تکرار رمز عبور الزامی است")]
        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "رمز عبور و تکرار آن مطابقت ندارند")]
        [Display(Name = "تکرار رمز عبور")]
        public string ConfirmPassword { get; set; } = string.Empty;

        [Required(ErrorMessage = "شماره تلفن الزامی است")]
        [Display(Name = "شماره تلفن")]
        [RegularExpression(@"^09\d{9}$", ErrorMessage = "شماره موبایل باید 11 رقم و با 09 شروع شود")]
        public string Phone { get; set; } = string.Empty;

        [Required(ErrorMessage = "کد تأیید پیامک الزامی است")]
        [Display(Name = "کد تأیید پیامک")]
        [StringLength(6, MinimumLength = 4, ErrorMessage = "کد تأیید باید بین 4 تا 6 رقم باشد")]
        public string SmsCode { get; set; } = string.Empty;
    }
}
