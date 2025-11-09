using System.Collections.Generic;
using System.Threading.Tasks;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace TaskPlanner.Application.Services.NotificationService
{
    public interface INotificationService
    {
        Task<int> CreateNotificationAsync(NotificationCreateRequest request, CancellationToken cancellationToken = default);
        Task CreateNotificationsAsync(IEnumerable<NotificationCreateRequest> requests, CancellationToken cancellationToken = default);
        Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, int take = 20, CancellationToken cancellationToken = default);
        Task<int> CountUnreadAsync(string userId, CancellationToken cancellationToken = default);
        Task MarkAsReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default);
        Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default);
        Task TrySendDueSoonNotificationAsync(TaskItem task, string? projectName = null, CancellationToken cancellationToken = default);
    }
}

