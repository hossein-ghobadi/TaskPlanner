using Microsoft.AspNetCore.Http;
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

        [StringLength(4000)]
        public string? Message { get; set; }

        public int? ReplyToMessageId { get; set; }

        public List<IFormFile>? Attachments { get; set; }
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
        public bool IsBaleSynced { get; set; }
        public bool IsReadOnly { get; set; }
        public long? BaleChatId { get; set; }
    }

    public class ProjectChatMessageVm
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Message { get; set; } = null!;
        public int? ReplyToMessageId { get; set; }
        public string? ReplyPreviewUserName { get; set; }
        public string? ReplyPreviewMessage { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsCurrentUser { get; set; }
        public bool IsExternal { get; set; }
        public string? ExternalProvider { get; set; }
        public long? ExternalSenderBaleId { get; set; }
        public string? ExternalSenderUsername { get; set; }
        public string? ExternalSenderFirstName { get; set; }
        public string? ExternalSenderLastName { get; set; }
        public string? ExternalSenderPhone { get; set; }
        public bool ExternalIsChannelSender { get; set; }
        public string? ExternalSenderPhotoPath { get; set; }
        public List<ProjectChatMessageAttachmentVm> Attachments { get; set; } = new();
    }

    public class ProjectChatMessageAttachmentVm
    {
        public int Id { get; set; }
        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public string FileType { get; set; } = null!;
        public long FileSize { get; set; }
        public string? MimeType { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    public class ProjectChatGroupMemberVm
    {
        public string UserId { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
    }
}
