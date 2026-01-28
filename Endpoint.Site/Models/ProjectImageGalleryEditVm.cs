using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class ProjectImageGalleryEditVm
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "عنوان عکس الزامی است")]
        [MaxLength(200, ErrorMessage = "عنوان عکس نمی‌تواند بیشتر از 200 کاراکتر باشد")]
        public string Title { get; set; } = null!;

        public string? Description { get; set; }

        [Required]
        public int ProjectId { get; set; }

        // پوشه (اختیاری)
        public int? FolderId { get; set; }

        // مسیر فعلی فایل (برای نمایش)
        public string CurrentFilePath { get; set; } = null!;
    }
}

