using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class PersonalNoteCreateVm
    {
        [Required(ErrorMessage = "عنوان یادداشت الزامی است")]
        public string Title { get; set; } = null!;

        public string? Content { get; set; }

        public string? Color { get; set; }

        public bool IsPinned { get; set; } = false;

        // فایل‌های پیوست
        public List<IFormFile>? Attachments { get; set; }
    }
}


