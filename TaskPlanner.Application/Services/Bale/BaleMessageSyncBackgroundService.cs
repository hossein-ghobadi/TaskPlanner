using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using TaskPlanner.Application.Interfaces.Contexts;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace TaskPlanner.Application.Services.Bale
{
    public class BaleMessageSyncBackgroundService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IBaleBotClient _baleBotClient;
        private readonly ILogger<BaleMessageSyncBackgroundService> _logger;
        private readonly BaleBotOptions _options;

        public BaleMessageSyncBackgroundService(
            IServiceScopeFactory scopeFactory,
            IBaleBotClient baleBotClient,
            IOptions<BaleBotOptions> options,
            ILogger<BaleMessageSyncBackgroundService> logger)
        {
            _scopeFactory = scopeFactory;
            _baleBotClient = baleBotClient;
            _logger = logger;
            _options = options.Value;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Bale message sync service started.");

            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    await PollAllLinksAsync(stoppingToken);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    _logger.LogError(ex, "Error while polling Bale updates.");
                }

                await Task.Delay(TimeSpan.FromSeconds(Math.Max(1, _options.PollIntervalSeconds)), stoppingToken);
            }
        }

        private async Task PollAllLinksAsync(CancellationToken cancellationToken)
        {
            using var scope = _scopeFactory.CreateScope();
            var context = scope.ServiceProvider.GetRequiredService<IMVPTestDatabaseContext>();
            var chatNotifier = scope.ServiceProvider.GetRequiredService<IBaleChatNotifier>();
            var mediaIngest = scope.ServiceProvider.GetRequiredService<IBaleMediaIngestService>();
            var senderEnrichment = scope.ServiceProvider.GetRequiredService<IBaleSenderEnrichmentService>();

            var links = await context.ProjectBaleGroupLinks
                .Where(l => l.IsEnabled && l.BotToken != "")
                .Select(l => new BaleLinkPollRow
                {
                    LinkId = l.Id,
                    ProjectChatGroupId = l.ProjectChatGroupId,
                    BaleChatId = l.BaleChatId,
                    BaleChatUsername = l.BaleChatUsername,
                    BotToken = l.BotToken,
                    LastUpdateId = l.LastUpdateId
                })
                .ToListAsync(cancellationToken);

            if (links.Count == 0)
            {
                return;
            }

            foreach (var tokenGroup in links.GroupBy(l => l.BotToken))
            {
                var botToken = tokenGroup.Key;
                var offset = tokenGroup.Max(l => l.LastUpdateId) + 1;
                var updatesResult = await _baleBotClient.GetUpdatesAsync(botToken, offset, _options.LongPollTimeoutSeconds, cancellationToken);
                if (!updatesResult.Success)
                {
                    _logger.LogWarning("Bale getUpdates failed: {Error}", updatesResult.ErrorDescription);
                    continue;
                }

                var updates = updatesResult.Data ?? Array.Empty<BaleUpdate>();
                if (updates.Count == 0)
                {
                    continue;
                }

                var maxUpdateId = tokenGroup.Max(l => l.LastUpdateId);

                foreach (var update in updates.OrderBy(u => u.UpdateId))
                {
                    maxUpdateId = Math.Max(maxUpdateId, update.UpdateId);

                    var message = BaleMessageParser.TryGetUpdateMessage(update, out var isEdit);
                    if (message?.Chat == null)
                    {
                        continue;
                    }

                    if (!TryFindLink(tokenGroup, message.Chat, out var link))
                    {
                        _logger.LogDebug(
                            "Bale update {UpdateId} skipped: chat {ChatId} ({ChatType}) not linked.",
                            update.UpdateId,
                            message.Chat.Id,
                            message.Chat.Type);
                        continue;
                    }

                    var chatType = message.Chat.Type?.ToLowerInvariant();
                    if (chatType is not ("group" or "supergroup" or "channel"))
                    {
                        continue;
                    }

                    var saved = await SaveMessageAsync(
                        context,
                        chatNotifier,
                        mediaIngest,
                        senderEnrichment,
                        link.ProjectChatGroupId,
                        botToken,
                        message,
                        isEdit,
                        cancellationToken);
                    if (saved != null && !isEdit)
                    {
                        var group = await context.ProjectChatGroups.FirstOrDefaultAsync(g => g.Id == link.ProjectChatGroupId, cancellationToken);
                        if (group != null)
                        {
                            group.UpdatedAt = DateTime.UtcNow;
                        }
                    }
                }

                var linkIds = tokenGroup.Select(l => l.LinkId).ToList();
                await context.ProjectBaleGroupLinks
                    .Where(l => linkIds.Contains(l.Id))
                    .ExecuteUpdateAsync(s => s.SetProperty(l => l.LastUpdateId, maxUpdateId), cancellationToken);

                await context.SaveChangesAsync(cancellationToken);
            }
        }

        private async Task<BaleSyncedMessageDto?> SaveMessageAsync(
            IMVPTestDatabaseContext context,
            IBaleChatNotifier chatNotifier,
            IBaleMediaIngestService mediaIngest,
            IBaleSenderEnrichmentService senderEnrichment,
            int groupId,
            string botToken,
            BaleMessage message,
            bool isEdit,
            CancellationToken cancellationToken)
        {
            var existing = await context.ProjectChatMessages
                .Include(m => m.Attachments)
                .FirstOrDefaultAsync(m =>
                    m.ProjectChatGroupId == groupId &&
                    m.ExternalProvider == "Bale" &&
                    m.ExternalMessageId == message.MessageId,
                    cancellationToken);

            var sender = await senderEnrichment.ResolveAsync(botToken, message, cancellationToken);
            var text = BaleMessageParser.BuildMessageText(message, sender);
            var userName = sender.DisplayName;
            var userId = BaleMessageParser.ResolveUserId(message, sender);
            var createdAt = DateTimeOffset.FromUnixTimeSeconds(message.Date).UtcDateTime;

            if (existing != null)
            {
                if (isEdit)
                {
                    existing.Message = text;
                    existing.UserName = userName;
                    ApplySenderToEntity(existing, sender);

                    if (existing.Attachments.Count == 0 && HasDownloadableImage(message))
                    {
                        var editAttachments = await mediaIngest.IngestMessageMediaAsync(
                            botToken,
                            existing.Id,
                            message,
                            cancellationToken);
                        if (editAttachments.Count > 0)
                        {
                            context.ProjectChatMessageAttachments.AddRange(editAttachments);
                        }
                    }

                    await context.SaveChangesAsync(cancellationToken);

                    await chatNotifier.NotifyMessageUpdatedAsync(
                        groupId,
                        existing.Id,
                        existing.Message,
                        cancellationToken);
                }

                return null;
            }

            var photoPath = await mediaIngest.ResolveSenderPhotoPathAsync(
                botToken,
                groupId,
                sender.BaleUserId,
                context,
                cancellationToken);

            var entity = new ProjectChatMessage
            {
                ProjectChatGroupId = groupId,
                UserId = userId,
                UserName = userName,
                Message = text,
                CreatedAt = createdAt,
                ExternalProvider = "Bale",
                ExternalMessageId = message.MessageId,
                ExternalSenderPhotoPath = photoPath
            };
            ApplySenderToEntity(entity, sender);

            context.ProjectChatMessages.Add(entity);
            await context.SaveChangesAsync(cancellationToken);

            var attachments = await mediaIngest.IngestMessageMediaAsync(
                botToken,
                entity.Id,
                message,
                cancellationToken);

            if (attachments.Count > 0)
            {
                context.ProjectChatMessageAttachments.AddRange(attachments);
                await context.SaveChangesAsync(cancellationToken);
            }

            var dto = BaleMessageParser.ToSyncedDto(entity, attachments, groupId);

            await chatNotifier.NotifyMessageCreatedAsync(groupId, dto, cancellationToken);

            return dto;
        }

        private static void ApplySenderToEntity(ProjectChatMessage entity, BaleSenderInfo sender)
        {
            entity.ExternalSenderPhone = sender.Phone;
            entity.ExternalIsChannelSender = sender.IsChannelSender && !sender.HasKnownAuthor;

            if (sender.HasKnownAuthor)
            {
                entity.ExternalSenderBaleId = sender.BaleUserId;
                entity.ExternalSenderFirstName = sender.FirstName;
                entity.ExternalSenderLastName = sender.LastName;
                entity.ExternalSenderUsername = sender.Username;
                return;
            }

            entity.ExternalSenderBaleId = null;
            entity.ExternalSenderFirstName = null;
            entity.ExternalSenderLastName = null;
            entity.ExternalSenderUsername = null;
        }

        private static bool HasDownloadableImage(BaleMessage message)
        {
            return message.Photo is { Count: > 0 }
                || (message.Document != null && !string.IsNullOrEmpty(message.Document.FileId));
        }

        private static bool TryFindLink(IEnumerable<BaleLinkPollRow> links, BaleChat chat, out BaleLinkPollRow? link)
        {
            link = links.FirstOrDefault(l => l.BaleChatId == chat.Id);
            if (link != null)
            {
                return true;
            }

            var messageUsername = BaleChatResolver.NormalizeUsername(chat.Username);
            if (string.IsNullOrEmpty(messageUsername))
            {
                link = null;
                return false;
            }

            link = links.FirstOrDefault(l =>
                !string.IsNullOrEmpty(l.BaleChatUsername) &&
                string.Equals(l.BaleChatUsername, messageUsername, StringComparison.OrdinalIgnoreCase));

            return link != null;
        }

        private sealed class BaleLinkPollRow
        {
            public int LinkId { get; init; }
            public int ProjectChatGroupId { get; init; }
            public long BaleChatId { get; init; }
            public string? BaleChatUsername { get; init; }
            public string BotToken { get; init; } = null!;
            public int LastUpdateId { get; init; }
        }
    }
}
