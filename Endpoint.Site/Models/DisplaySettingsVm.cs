using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class DisplaySettingsVm
    {
        [Display(Name = "لیدها (CRM)")]
        public bool ShowLeads { get; set; } = true;

        [Display(Name = "تخته‌ها")]
        public bool ShowBoards { get; set; } = true;

        [Display(Name = "مایندمپ‌ها")]
        public bool ShowMindMaps { get; set; } = true;

        [Display(Name = "صفحه طراحی")]
        public bool ShowDesigns { get; set; } = true;

        public bool CanManageAdminSections { get; set; }

        [Display(Name = "مدیریت کاربران")]
        public bool ShowAdminUsers { get; set; } = true;

        [Display(Name = "مرخصی همکاران")]
        public bool ShowAdminLeaves { get; set; } = true;

        [Display(Name = "لیست کارها")]
        public bool ShowProjectTasks { get; set; } = true;

        [Display(Name = "تخته کانبان")]
        public bool ShowProjectKanban { get; set; } = true;

        [Display(Name = "اسپرینت‌ها")]
        public bool ShowProjectSprints { get; set; } = true;

        [Display(Name = "فیچرها")]
        public bool ShowProjectFeatures { get; set; } = true;

        [Display(Name = "تیکت‌ها")]
        public bool ShowProjectTickets { get; set; } = true;

        [Display(Name = "گالری عکس")]
        public bool ShowProjectGallery { get; set; } = true;

        [Display(Name = "دسته‌بندی‌ها")]
        public bool ShowProjectCategories { get; set; } = true;
    }
}
