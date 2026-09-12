using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class ProjectSettingsVm
    {
        public int ProjectId { get; set; }
        public string ProjectName { get; set; } = "";
        public bool IsCreator { get; set; }

        [Display(Name = "لیست کارها")]
        public bool ShowTasks { get; set; } = true;

        [Display(Name = "تخته کانبان")]
        public bool ShowKanban { get; set; } = true;

        [Display(Name = "اسپرینت‌ها")]
        public bool ShowSprints { get; set; } = true;

        [Display(Name = "فیچرها")]
        public bool ShowFeatures { get; set; } = true;

        [Display(Name = "تیکت‌ها")]
        public bool ShowTickets { get; set; } = true;

        [Display(Name = "گالری عکس")]
        public bool ShowGallery { get; set; } = true;

        [Display(Name = "دسته‌بندی‌ها")]
        public bool ShowCategories { get; set; } = true;
    }
}
