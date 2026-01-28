using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class ProjectNoteFolderCreateVm
    {
        [Required]
        public int ProjectId { get; set; }

        [Required(ErrorMessage = "نام پوشه الزامی است")]
        [MaxLength(200, ErrorMessage = "نام پوشه نمی‌تواند بیشتر از 200 کاراکتر باشد")]
        public string Name { get; set; } = null!;

        // پوشه والد (اختیاری)
        public int? ParentFolderId { get; set; }

        // رنگ پوشه (اختیاری)
        public string? Color { get; set; }
    }
}

