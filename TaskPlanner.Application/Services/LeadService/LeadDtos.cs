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
        public int? ConvertedProjectId { get; set; }
    }

    public class LeadDetailsDto
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
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
        public int? ConvertedProjectId { get; set; }
        public DateTime? ConvertedAt { get; set; }
    }

    public class CreateLeadDto
    {
        public string Title { get; set; } = null!;
        public string? CompanyName { get; set; }
        public string? ContactName { get; set; }
        public string? Phone { get; set; }
        public string? Email { get; set; }
        public string? Notes { get; set; }
        public string? Source { get; set; }
        public LeadPipelineStatus Status { get; set; } = LeadPipelineStatus.New;
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
    }
}
