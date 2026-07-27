namespace TaskPlanner.Application.Services.LeaveService
{
    public class LeaveTypeDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Color { get; set; } = "#6b7280";
        public bool IsHourlyAllowed { get; set; }
    }

    public class ColleagueSelectDto
    {
        public string Id { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
    }

    public class LeaveRecordDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public string ColleagueName { get; set; } = null!;
        public int LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = null!;
        public string LeaveTypeColor { get; set; } = "#6b7280";
        public bool LeaveTypeIsHourlyAllowed { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public int? DurationDays { get; set; }
        public double DurationHours { get; set; }
        public string? Note { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class LeaveRecordFilterDto
    {
        public string? UserId { get; set; }
        public int? LeaveTypeId { get; set; }
        public DateTime? FromDate { get; set; }
        public DateTime? ToDate { get; set; }
        public string? SearchNote { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
    }

    public class LeaveRecordListResultDto
    {
        public IReadOnlyList<LeaveRecordDto> Items { get; set; } = Array.Empty<LeaveRecordDto>();
        public int TotalCount { get; set; }
    }

    public class CreateLeaveRecordDto
    {
        public string UserId { get; set; } = null!;
        public int LeaveTypeId { get; set; }
        public string StartPersian { get; set; } = null!;
        public string EndPersian { get; set; } = null!;
        public bool IsHourly { get; set; }
        /// <summary>تعداد روز مرخصی مؤثر (مرخصی روزانه)</summary>
        public int? DurationDays { get; set; }
        public string? Note { get; set; }
    }

    public class UpdateLeaveRecordDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public int LeaveTypeId { get; set; }
        public string StartPersian { get; set; } = null!;
        public string EndPersian { get; set; } = null!;
        public bool IsHourly { get; set; }
        public int? DurationDays { get; set; }
        public string? Note { get; set; }
    }

    public class LeaveStatsDto
    {
        public double TotalHours { get; set; }
        public double TotalDays { get; set; }
        public int TotalRecords { get; set; }
        public IReadOnlyList<LeaveTypeStatDto> ByType { get; set; } = Array.Empty<LeaveTypeStatDto>();
        public IReadOnlyList<ColleagueLeaveStatDto> ByColleague { get; set; } = Array.Empty<ColleagueLeaveStatDto>();
    }

    public class LeaveTypeStatDto
    {
        public int LeaveTypeId { get; set; }
        public string LeaveTypeName { get; set; } = null!;
        public string Color { get; set; } = "#6b7280";
        public double TotalHours { get; set; }
        public double TotalDays { get; set; }
        public int Count { get; set; }
    }

    public class ColleagueLeaveStatDto
    {
        public string UserId { get; set; } = null!;
        public string ColleagueName { get; set; } = null!;
        public double TotalHours { get; set; }
        public double TotalDays { get; set; }
        public int Count { get; set; }
    }

    public class LeaveCalendarDayDto
    {
        public DateTime Date { get; set; }
        public IReadOnlyList<LeaveCalendarItemDto> Items { get; set; } = Array.Empty<LeaveCalendarItemDto>();
    }

    public class LeaveCalendarItemDto
    {
        public int Id { get; set; }
        public string ColleagueName { get; set; } = null!;
        public string LeaveTypeName { get; set; } = null!;
        public string LeaveTypeColor { get; set; } = "#6b7280";
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
    }
}
