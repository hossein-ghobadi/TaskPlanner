using TaskPlanner.Domain.Entities.TaskPlanner;

namespace TaskPlanner.Application.Services.LeadService
{
    public class LeadListItemDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? CompanyName { get; set; }
        public LeadPipelineStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? MeetingAt { get; set; }
        public DateTime? NextSessionAt { get; set; }
        public decimal? ProjectAmount { get; set; }
        public int? ConvertedProjectId { get; set; }
        public int CrmId { get; set; }
        /// <summary>مالک فضای CRM.</summary>
        public bool IsCrmOwner { get; set; }
        /// <summary>عضو CRM می‌تواند لید را ویرایش کند.</summary>
        public bool CanEdit { get; set; }
        /// <summary>فقط اگر عضو پروژهٔ تبدیل‌شده باشد true است (عضویت CRM کافی نیست).</summary>
        public bool CanOpenConvertedProject { get; set; }
    }

    public class LeadMemberSummaryDto
    {
        public string UserId { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
    }

    public class LeadPendingInviteDto
    {
        public int Id { get; set; }
        public string InviteePhone { get; set; } = null!;
        public string? InviteeDisplayName { get; set; }
    }

    public class LeadSessionDto
    {
        public int Id { get; set; }
        public DateTime ScheduledAt { get; set; }
        public string? Notes { get; set; }
    }

    public class LeadNoteDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? Content { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<LeadNoteAttachmentDto> Attachments { get; set; } = new();
    }

    public class LeadNoteAttachmentDto
    {
        public int Id { get; set; }
        public string FileName { get; set; } = null!;
        public string FileType { get; set; } = null!;
        public string FilePath { get; set; } = null!;
    }

    public class LeadDetailsDto
    {
        public int Id { get; set; }
        public int CrmId { get; set; }
        public string Title { get; set; } = null!;
        public string? CompanyName { get; set; }
        public string? ContactName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public string? Source { get; set; }
        public LeadPipelineStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public DateTime? MeetingAt { get; set; }
        public decimal? ProjectAmount { get; set; }
        public int? ConvertedProjectId { get; set; }
        public DateTime? ConvertedAt { get; set; }
        public string OwnerUserId { get; set; } = null!;
        public bool IsCrmOwner { get; set; }
        public bool CanEdit { get; set; }
        /// <summary>فقط اگر عضو پروژهٔ تبدیل‌شده باشد؛ عضویت CRM به‌تنهایی کافی نیست.</summary>
        public bool CanOpenConvertedProject { get; set; }
        public List<LeadMemberSummaryDto> Members { get; set; } = new();
        public List<LeadPendingInviteDto> PendingInvitations { get; set; } = new();
        public List<LeadSessionDto> Sessions { get; set; } = new();
        public List<LeadNoteDto> NotesList { get; set; } = new();
    }

    public class CreateLeadDto
    {
        public int? CrmId { get; set; }
        public string Title { get; set; } = null!;
        public string? CompanyName { get; set; }
        public string? ContactName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public string? Source { get; set; }
        public LeadPipelineStatus Status { get; set; } = LeadPipelineStatus.New;
        public DateTime? MeetingAt { get; set; }
        public decimal? ProjectAmount { get; set; }
    }

    public class UpdateLeadDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string? CompanyName { get; set; }
        public string? ContactName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public string? Source { get; set; }
        public LeadPipelineStatus Status { get; set; }
        public DateTime? MeetingAt { get; set; }
        public decimal? ProjectAmount { get; set; }
    }

    public class CreateLeadSessionDto
    {
        public int LeadId { get; set; }
        public DateTime ScheduledAt { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateLeadSessionDto
    {
        public int SessionId { get; set; }
        public DateTime ScheduledAt { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateLeadNoteDto
    {
        public int LeadId { get; set; }
        public string Title { get; set; } = null!;
        public string? Content { get; set; }
    }

    public class UpdateLeadNoteDto
    {
        public int NoteId { get; set; }
        public string Title { get; set; } = null!;
        public string? Content { get; set; }
    }
}
