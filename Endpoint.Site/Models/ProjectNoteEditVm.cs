using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class ProjectNoteEditVm
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "عنوان یادداشت الزامی است")]
        public string Title { get; set; } = null!;

        public string? Content { get; set; }

        public int ProjectId { get; set; }

        // فایل‌های پیوست جدید
        public List<IFormFile>? NewAttachments { get; set; }
        
        // آیدی فایل‌هایی که باید حذف شوند
        public List<int>? DeletedAttachmentIds { get; set; }
    }
}


