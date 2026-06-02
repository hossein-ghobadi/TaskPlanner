using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class ProjectBaleGroupLinkVm
    {
        public int? Id { get; set; }

        [Required]
        public int ProjectId { get; set; }

        [Required]
        [StringLength(150, MinimumLength = 2)]
        public string DisplayName { get; set; } = null!;

        /// <summary>شناسه عددی پس از resolve (برای نمایش).</summary>
        public long BaleChatId { get; set; }

        /// <summary>ورودی کاربر: شناسه عددی یا @username کانال/گروه.</summary>
        [StringLength(120)]
        public string? BaleChatIdentifier { get; set; }

        [StringLength(200)]
        public string? BaleChatTitle { get; set; }

        [StringLength(120)]
        public string? BotToken { get; set; }

        public bool HasBotToken { get; set; }

        public string? BotTokenHint { get; set; }

        public bool IsEnabled { get; set; } = true;
    }

    public class ProjectBaleDiscoveredChatVm
    {
        public long ChatId { get; set; }
        public string? Username { get; set; }
        public string? Title { get; set; }
        public string? Type { get; set; }

        public string DisplayLabel =>
            !string.IsNullOrWhiteSpace(Title)
                ? Title!
                : (!string.IsNullOrWhiteSpace(Username) ? $"@{Username}" : ChatId.ToString());
    }
}
