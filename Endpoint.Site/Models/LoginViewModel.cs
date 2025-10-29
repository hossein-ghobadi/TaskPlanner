using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "ایمیل یا نام کاربری الزامی است")]
        [Display(Name = "ایمیل یا نام کاربری")]
        public string UserName { get; set; } = string.Empty;

        [Required(ErrorMessage = "رمز عبور الزامی است")]
        [DataType(DataType.Password)]
        [Display(Name = "رمز عبور")]
        public string Password { get; set; } = string.Empty;

        [Display(Name = "مرا به خاطر بسپار")]
        public bool RememberMe { get; set; }

        public string? ReturnUrl { get; set; }
    }
}
