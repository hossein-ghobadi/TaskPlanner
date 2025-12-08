using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Http;

namespace Endpoint.Site.Models
{
    public class BoardTaskCommentCreateVm
    {
        [Required]
        public int BoardTaskId { get; set; }

        [Display(Name = "یادداشت")]
        public string? Message { get; set; }

        /// <summary>
        /// فایل‌های پیوست
        /// </summary>
        public List<IFormFile>? Attachments { get; set; }
    }
}

