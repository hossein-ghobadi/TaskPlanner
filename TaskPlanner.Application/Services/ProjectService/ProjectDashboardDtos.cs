using TaskPlanner.Domain.Entities.TaskPlanner;

namespace TaskPlanner.Application.Services.ProjectService
{
    public class ProjectDashboardDto
    {
        public List<ProjectCardDto> Projects { get; set; } = new();
        public Dictionary<int, int> ActiveSprintByProject { get; set; } = new();
        public int TotalProjectsCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasMoreProjects => Page * PageSize < TotalProjectsCount;
        public List<ProjectInvitationDto> PendingProjectInvitations { get; set; } = new();
        public List<ProjectInvitationDto> PendingSystemInvitations { get; set; } = new();
        public Dictionary<string, string> InviterLookup { get; set; } = new();
        public List<CollaboratorDto> Collaborators { get; set; } = new();
        public List<RecentPersonalNoteDto> RecentNotes { get; set; } = new();
    }

    public class ProjectCardDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CreatorUserId { get; set; } = string.Empty;
        public int TaskCount { get; set; }
    }

    public class CollaboratorDto
    {
        public string Id { get; set; } = string.Empty;
        public string DisplayName { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
    }

    public class RecentPersonalNoteDto
    {
        public int Id { get; set; }
        public string UserId { get; set; } = string.Empty;
        public string? Title { get; set; }
        public string? Content { get; set; }
        public bool IsPinned { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    public class ProjectCardsPageDto
    {
        public List<ProjectCardDto> Projects { get; set; } = new();
        public Dictionary<int, int> ActiveSprintByProject { get; set; } = new();
        public int TotalProjectsCount { get; set; }
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasMoreProjects => Page * PageSize < TotalProjectsCount;
    }
}
