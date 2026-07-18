using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Application.Services.FileUpload;

namespace TaskPlanner.Application.Services.Bale
{
    public static class BaleMessageParser
    {
        public static BaleMessage? TryGetUpdateMessage(BaleUpdate update, out bool isEdit)
        {
            if (update.Message != null)
            {
                isEdit = false;
                return update.Message;
            }

            if (update.EditedMessage != null)
            {
                isEdit = true;
                return update.EditedMessage;
            }

            if (update.ChannelPost != null)
            {
                isEdit = false;
                return update.ChannelPost;
            }

            if (update.EditedChannelPost != null)
            {
                isEdit = true;
                return update.EditedChannelPost;
            }

            isEdit = false;
            return null;
        }

        public static string ResolveUserId(BaleMessage message, BaleSenderInfo sender)
        {
            if (sender.BaleUserId.HasValue)
            {
                return $"bale:{sender.BaleUserId.Value}";
            }

            if (message.From != null)
            {
                return $"bale:{message.From.Id}";
            }

            if (message.SenderChat != null)
            {
                return $"bale-chat:{message.SenderChat.Id}";
            }

            if (message.Chat != null)
            {
                return $"bale-chat:{message.Chat.Id}";
            }

            return "bale:unknown";
        }

        public static string BuildMessageText(BaleMessage message, BaleSenderInfo sender)
        {
            if (message.Contact != null)
            {
                var contactLines = new List<string> { "مخاطب به‌اشتراک‌گذاشته‌شده" };
                var name = BaleSenderEnrichmentService.BuildDisplayName(sender);
                if (!string.IsNullOrWhiteSpace(name) && name != "کاربر بله" && name != "کانال بله")
                {
                    contactLines.Add($"نام: {name}");
                }

                if (!string.IsNullOrWhiteSpace(sender.Phone))
                {
                    contactLines.Add($"تلفن: {sender.Phone}");
                }

                return string.Join("\n", contactLines);
            }

            if (!string.IsNullOrWhiteSpace(message.Text))
            {
                return message.Text.Trim();
            }

            if (!string.IsNullOrWhiteSpace(message.Caption))
            {
                return message.Caption.Trim();
            }

            if (message.Photo is { Count: > 0 })
            {
                return string.Empty;
            }

            if (message.Document != null && IsImageMime(message.Document.MimeType))
            {
                return string.Empty;
            }

            if (message.Document != null)
            {
                var fileName = message.Document.FileName;
                return string.IsNullOrWhiteSpace(fileName) ? "[فایل]" : $"[فایل: {fileName}]";
            }

            if (message.Voice != null)
            {
                return $"[پیام صوتی — {message.Voice.Duration} ثانیه]";
            }

            if (message.Audio != null)
            {
                var title = message.Audio.Title ?? message.Audio.FileName;
                return string.IsNullOrWhiteSpace(title) ? "[فایل صوتی]" : $"[فایل صوتی: {title}]";
            }

            if (message.Video != null)
            {
                return $"[ویدیو — {message.Video.Duration} ثانیه]";
            }

            if (message.Sticker != null)
            {
                return string.IsNullOrWhiteSpace(message.Sticker.Emoji) ? "[استیکر]" : message.Sticker.Emoji!;
            }

            return "[پیام]";
        }

        public static BaleSyncedMessageDto ToSyncedDto(
            ProjectChatMessage entity,
            IReadOnlyList<ProjectChatMessageAttachment> attachments,
            int groupId)
        {
            return new BaleSyncedMessageDto
            {
                Id = entity.Id,
                GroupId = groupId,
                UserId = entity.UserId,
                UserName = entity.UserName,
                Message = entity.Message,
                CreatedAt = entity.CreatedAt,
                IsExternal = true,
                ExternalProvider = "Bale",
                ExternalSenderBaleId = entity.ExternalSenderBaleId,
                ExternalSenderUsername = entity.ExternalSenderUsername,
                ExternalSenderFirstName = entity.ExternalSenderFirstName,
                ExternalSenderLastName = entity.ExternalSenderLastName,
                ExternalSenderPhone = entity.ExternalSenderPhone,
                ExternalIsChannelSender = entity.ExternalIsChannelSender,
                ExternalSenderPhotoPath = FileUrls.ToPublicUrl(entity.ExternalSenderPhotoPath),
                Attachments = attachments.Select(a => new BaleSyncedMessageAttachmentDto
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FilePath = FileUrls.ToPublicUrl(a.FilePath),
                    FileType = a.FileType,
                    FileSize = a.FileSize,
                    MimeType = a.MimeType,
                    UploadedAt = a.UploadedAt
                }).ToList()
            };
        }

        private static bool IsImageMime(string? mimeType)
        {
            return !string.IsNullOrWhiteSpace(mimeType)
                && mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }
    }
}
