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
    }
}
