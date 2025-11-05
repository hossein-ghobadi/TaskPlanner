using TaskPlanner.Domain.Entities.TaskPlanner;

namespace TaskPlanner.Application.Services.ProjectService
{
    /// <summary>
    /// DTO برای جزئیات پروژه - استفاده در لایه Application
    /// </summary>
    public class ProjectDetailsDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public string CreatorUserId { get; set; } = null!;
        public string CreatorUserName { get; set; } = null!;
        public List<string> MemberUserNames { get; set; } = new();
        public List<TaskItem> Tasks { get; set; } = new();
        public List<ProjectInvitationDto> Invitations { get; set; } = new();
        public List<TaskCategory> Categories { get; set; } = new();
        public int? ActiveSprintId { get; set; }
        public string? ActiveSprintName { get; set; }
    }

    /// <summary>
    /// DTO برای دعوت پروژه
    /// </summary>
    public class ProjectInvitationDto
    {
        public string InviteePhone { get; set; } = null!;
        public InvitationStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}


