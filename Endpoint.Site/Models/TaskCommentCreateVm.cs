using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class TaskCommentCreateVm
    {
        public string? Message { get; set; }

        [Required]
        public int TaskId { get; set; }

        // فایل‌های پیوست
        public List<IFormFile>? Attachments { get; set; }
    }
}

