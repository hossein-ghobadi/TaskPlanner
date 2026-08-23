using TaskPlanner.Domain.Entities.TaskPlanner;

namespace TaskPlanner.Application.Services.ProjectService
{
    public enum ProjectListFilter
    {
        Open = 0,
        Closed = 1,
        All = 2
    }

    public class ProjectDashboardDto
    {
        public List<ProjectCardDto> Projects { get; set; } = new();
        public Dictionary<int, int> ActiveSprintByProject { get; set; } = new();
        public int TotalProjectsCount { get; set; }
        public int AllProjectsCount { get; set; }
        public ProjectListFilter Status { get; set; } = ProjectListFilter.Open;
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasMoreProjects => Page * PageSize < TotalProjectsCount;
        public int PendingInviteCount { get; set; }
        public int CollaboratorCount { get; set; }
        public int RecentNoteCount { get; set; }
    }

    public class ProjectCardDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string CreatorUserId { get; set; } = string.Empty;
        public int TaskCount { get; set; }
        public bool IsClosed { get; set; }
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
        public ProjectListFilter Status { get; set; } = ProjectListFilter.Open;
        public int Page { get; set; }
        public int PageSize { get; set; }
        public bool HasMoreProjects => Page * PageSize < TotalProjectsCount;
    }
}
