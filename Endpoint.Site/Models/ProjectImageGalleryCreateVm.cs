using Microsoft.AspNetCore.Http;
using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class ProjectImageGalleryCreateVm
    {
        // عنوان پیش‌فرض (اختیاری - اگر خالی باشد از نام فایل استفاده می‌شود)
        [MaxLength(200, ErrorMessage = "عنوان عکس نمی‌تواند بیشتر از 200 کاراکتر باشد")]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [Required]
        public int ProjectId { get; set; }

        // پوشه (اختیاری)
        public int? FolderId { get; set; }

        // فایل‌های عکس (پشتیبانی از آپلود دسته‌ای)
        [Required(ErrorMessage = "لطفاً حداقل یک عکس انتخاب کنید")]
        public List<IFormFile> ImageFiles { get; set; } = new List<IFormFile>();
    }
}

