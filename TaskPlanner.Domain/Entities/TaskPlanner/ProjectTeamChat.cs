using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    public class ProjectChatGroup
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        [Required]
        [StringLength(150)]
        public string Name { get; set; } = null!;

        [Required]
        [StringLength(450)]
        public string CreatedByUserId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public bool IsArchived { get; set; } = false;

        public ICollection<ProjectChatGroupMember> Members { get; set; } = new List<ProjectChatGroupMember>();
        public ICollection<ProjectChatMessage> Messages { get; set; } = new List<ProjectChatMessage>();
    }

    public class ProjectChatGroupMember
    {
        public int Id { get; set; }

        [Required]
        public int ProjectChatGroupId { get; set; }
        public ProjectChatGroup ProjectChatGroup { get; set; } = null!;

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = null!;

        [StringLength(450)]
        public string? AddedByUserId { get; set; }

        public DateTime AddedAt { get; set; } = DateTime.UtcNow;
    }

    public class ProjectChatMessage
    {
        public int Id { get; set; }

        [Required]
        public int ProjectChatGroupId { get; set; }
        public ProjectChatGroup ProjectChatGroup { get; set; } = null!;

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = null!;

        [Required]
        [StringLength(200)]
        public string UserName { get; set; } = null!;

        [Required]
        [StringLength(4000)]
        public string Message { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public bool IsDeleted { get; set; } = false;
    }
}
