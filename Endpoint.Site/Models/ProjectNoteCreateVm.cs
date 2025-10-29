using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Areas.TaskPlanner.Models
{
    public class ProjectNoteCreateVm
    {
        [Required(ErrorMessage = "عنوان یادداشت الزامی است")]
        public string Title { get; set; } = null!;

        public string? Content { get; set; }

        [Required]
        public int ProjectId { get; set; }

        // فایل‌های پیوست
        public List<IFormFile>? Attachments { get; set; }
    }
}


