using System.ComponentModel.DataAnnotations;
using TaskPlanner.Application.Services.CrmService;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Models
{
    public class LeadFormVm
    {
        public int? Id { get; set; }

        [Display(Name = "CRM")]
        public int? CrmId { get; set; }

        public List<CrmSelectDto> AvailableCrms { get; set; } = new();

        [Required(ErrorMessage = "عنوان لید الزامی است")]
        [StringLength(200)]
        [Display(Name = "عنوان لید")]
        public string Title { get; set; } = null!;

        [StringLength(200)]
        public string? CompanyName { get; set; }

        [StringLength(200)]
        public string? ContactName { get; set; }

        [StringLength(50)]
        public string? Phone { get; set; }

        [StringLength(256)]
        [EmailAddress(ErrorMessage = "ایمیل معتبر نیست")]
        public string? Email { get; set; }

        public string? Notes { get; set; }

        [StringLength(100)]
        public string? Source { get; set; }

        public LeadPipelineStatus Status { get; set; } = LeadPipelineStatus.New;

        [Display(Name = "مبلغ پروژه (تومان)")]
        [Range(0, double.MaxValue, ErrorMessage = "مبلغ نمی‌تواند منفی باشد")]
        public decimal? ProjectAmount { get; set; }
    }
}
