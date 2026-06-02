using Endpoint.Site.Hubs;
using Endpoint.Site.Models;
using Microsoft.AspNetCore.SignalR;
using TaskPlanner.Application.Services.Bale;

namespace Endpoint.Site.Services.Bale
{
    public sealed class BaleSignalRChatNotifier : IBaleChatNotifier
    {
        private readonly IHubContext<ProjectChatHub> _hubContext;

        public BaleSignalRChatNotifier(IHubContext<ProjectChatHub> hubContext)
        {
            _hubContext = hubContext;
        }

        public async Task NotifyMessageCreatedAsync(int groupId, BaleSyncedMessageDto message, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients
                .Group(ProjectChatHub.GetSignalRGroupName(groupId))
                .SendAsync("projectGroupMessageCreated", MapToVm(message), cancellationToken);
        }

        public async Task NotifyMessageUpdatedAsync(int groupId, int messageId, string messageText, CancellationToken cancellationToken = default)
        {
            await _hubContext.Clients
                .Group(ProjectChatHub.GetSignalRGroupName(groupId))
                .SendAsync("projectGroupMessageUpdated", new
                {
                    id = messageId,
                    groupId,
                    message = messageText
                }, cancellationToken);
        }

        private static ProjectChatMessageVm MapToVm(BaleSyncedMessageDto dto)
        {
            return new ProjectChatMessageVm
            {
                Id = dto.Id,
                GroupId = dto.GroupId,
                UserId = dto.UserId,
                UserName = dto.UserName,
                Message = dto.Message,
                CreatedAt = dto.CreatedAt,
                IsCurrentUser = false,
                IsExternal = dto.IsExternal,
                ExternalProvider = dto.ExternalProvider,
                ExternalSenderBaleId = dto.ExternalSenderBaleId,
                ExternalSenderUsername = dto.ExternalSenderUsername,
                ExternalSenderFirstName = dto.ExternalSenderFirstName,
                ExternalSenderLastName = dto.ExternalSenderLastName,
                ExternalSenderPhone = dto.ExternalSenderPhone,
                ExternalIsChannelSender = dto.ExternalIsChannelSender,
                ExternalSenderPhotoPath = dto.ExternalSenderPhotoPath,
                Attachments = dto.Attachments.Select(a => new ProjectChatMessageAttachmentVm
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileType = a.FileType,
                    FileSize = a.FileSize,
                    MimeType = a.MimeType,
                    UploadedAt = a.UploadedAt
                }).ToList()
            };
        }
    }
}
