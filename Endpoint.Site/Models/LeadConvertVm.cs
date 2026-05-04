using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class LeadConvertVm
    {
        [Required]
        public int LeadId { get; set; }

        /// <summary>در صورت خالی بودن از عنوان لید استفاده می‌شود.</summary>
        [StringLength(200)]
        public string? ProjectName { get; set; }
    }
}
