using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class SendSmsCodeRequest
    {
        [Required(ErrorMessage = "شماره تلفن الزامی است")]
        public string Phone { get; set; } = string.Empty;
    }
}
