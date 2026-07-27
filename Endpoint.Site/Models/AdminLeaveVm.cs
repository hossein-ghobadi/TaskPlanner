using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class LeaveRecordListVm
    {
        public int Id { get; set; }
        public string ColleagueName { get; set; } = "";
        public string LeaveTypeName { get; set; } = "";
        public string LeaveTypeColor { get; set; } = "#6b7280";
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public int? DurationDays { get; set; }
        public double DurationHours { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LeaveFormVm
    {
        public int? Id { get; set; }

        [Required(ErrorMessage = "انتخاب همکار الزامی است.")]
        [Display(Name = "همکار")]
        public string UserId { get; set; } = "";

        [Required(ErrorMessage = "انتخاب نوع مرخصی الزامی است.")]
        [Display(Name = "نوع مرخصی")]
        public int LeaveTypeId { get; set; }

        [Required(ErrorMessage = "تاریخ شروع الزامی است.")]
        [Display(Name = "شروع")]
        public string StartPersian { get; set; } = "";

        [Required(ErrorMessage = "تاریخ پایان الزامی است.")]
        [Display(Name = "پایان")]
        public string EndPersian { get; set; } = "";

        [Display(Name = "ساعت شروع")]
        public string? StartTimePersian { get; set; }

        [Display(Name = "ساعت پایان")]
        public string? EndTimePersian { get; set; }

        [Display(Name = "مرخصی ساعتی")]
        public bool IsHourly { get; set; }

        [Display(Name = "تعداد روز مرخصی")]
        [Range(1, 365, ErrorMessage = "تعداد روز باید بین ۱ تا ۳۶۵ باشد.")]
        public int? DurationDays { get; set; }

        [Display(Name = "یادداشت")]
        [MaxLength(1000)]
        public string? Note { get; set; }
    }

    public class LeaveIndexFilterVm
    {
        public string? UserId { get; set; }
        public int? LeaveTypeId { get; set; }
        public string? FromPersian { get; set; }
        public string? ToPersian { get; set; }
        public string? SearchNote { get; set; }
        public int Page { get; set; } = 1;
    }

    public class LeaveStatsFilterVm
    {
        public string? FromPersian { get; set; }
        public string? ToPersian { get; set; }
    }
}
