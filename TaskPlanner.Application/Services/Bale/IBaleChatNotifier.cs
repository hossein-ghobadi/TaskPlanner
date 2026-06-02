namespace TaskPlanner.Application.Services.Bale
{
    public interface IBaleChatNotifier
    {
        Task NotifyMessageCreatedAsync(int groupId, BaleSyncedMessageDto message, CancellationToken cancellationToken = default);

        Task NotifyMessageUpdatedAsync(int groupId, int messageId, string messageText, CancellationToken cancellationToken = default);
    }
}
