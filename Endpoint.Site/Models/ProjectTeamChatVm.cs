using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class ProjectChatGroupCreateVm
    {
        [Required]
        public int ProjectId { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string Name { get; set; } = null!;

        public List<string>? MemberUserIds { get; set; }
    }

    public class ProjectChatSendMessageVm
    {
        [Required]
        public int GroupId { get; set; }

        [Required]
        [StringLength(4000, MinimumLength = 1)]
        public string Message { get; set; } = null!;
    }

    public class ProjectChatAddMembersVm
    {
        [Required]
        public int GroupId { get; set; }

        public List<string> UserIds { get; set; } = new();
    }

    public class ProjectChatGroupItemVm
    {
        public int Id { get; set; }
        public int ProjectId { get; set; }
        public string Name { get; set; } = null!;
        public string CreatedByUserId { get; set; } = null!;
        public string CreatedByName { get; set; } = null!;
        public int MembersCount { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool CanManageMembers { get; set; }
    }

    public class ProjectChatMessageVm
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsCurrentUser { get; set; }
        public bool CanDelete { get; set; }
    }

    public class ProjectChatGroupMemberVm
    {
        public string UserId { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
    }
}
