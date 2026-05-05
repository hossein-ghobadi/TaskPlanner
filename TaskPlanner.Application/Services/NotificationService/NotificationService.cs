using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TaskPlanner.Application.Interfaces.Contexts;
using TaskPlanner.Domain.Entities.Notifications;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace TaskPlanner.Application.Services.NotificationService
{
    public class NotificationService : INotificationService
    {
        private readonly IMVPTestDatabaseContext _context;

        public NotificationService(IMVPTestDatabaseContext context)
        {
            _context = context;
        }

        public async Task<int> CreateNotificationAsync(NotificationCreateRequest request, CancellationToken cancellationToken = default)
        {
            var notification = BuildNotification(request);
            await _context.Notifications.AddAsync(notification, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            return notification.Id;
        }

        public async Task CreateNotificationsAsync(IEnumerable<NotificationCreateRequest> requests, CancellationToken cancellationToken = default)
        {
            var notifications = requests.Select(BuildNotification).ToList();
            if (!notifications.Any())
            {
                return;
            }

            await _context.Notifications.AddRangeAsync(notifications, cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task<List<NotificationDto>> GetUserNotificationsAsync(string userId, bool unreadOnly = false, int take = 20, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .AsNoTracking()
                .Where(n => n.UserId == userId && (!unreadOnly || !n.IsRead))
                .OrderByDescending(n => n.CreatedAt)
                .Take(take)
                .Select(n => new NotificationDto
                {
                    Id = n.Id,
                    Title = n.Title,
                    Message = n.Message,
                    Type = n.Type.ToString(),
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt,
                    ReadAt = n.ReadAt,
                    PayloadJson = n.PayloadJson,
                    RelatedEntityId = n.RelatedEntityId,
                    RelatedEntityType = n.RelatedEntityType
                })
                .ToListAsync(cancellationToken);
        }

        public async Task<int> CountUnreadAsync(string userId, CancellationToken cancellationToken = default)
        {
            return await _context.Notifications
                .AsNoTracking()
                .CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);
        }

        public async Task MarkAsReadAsync(int notificationId, string userId, CancellationToken cancellationToken = default)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == notificationId && n.UserId == userId, cancellationToken);

            if (notification == null || notification.IsRead)
            {
                return;
            }

            notification.IsRead = true;
            notification.ReadAt = DateTime.UtcNow;
            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task MarkAllAsReadAsync(string userId, CancellationToken cancellationToken = default)
        {
            var notifications = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync(cancellationToken);

            if (!notifications.Any())
            {
                return;
            }

            var now = DateTime.UtcNow;
            foreach (var notification in notifications)
            {
                notification.IsRead = true;
                notification.ReadAt = now;
            }

            await _context.SaveChangesAsync(cancellationToken);
        }

        public async Task TrySendDueSoonNotificationAsync(TaskItem task, string? projectName = null, CancellationToken cancellationToken = default)
        {
            if (task.DueDate == null || string.IsNullOrWhiteSpace(task.AssignedUserId))
            {
                return;
            }

            var dueDateUtc = task.DueDate.Value.Kind switch
            {
                DateTimeKind.Utc => task.DueDate.Value,
                DateTimeKind.Local => task.DueDate.Value.ToUniversalTime(),
                _ => DateTime.SpecifyKind(task.DueDate.Value, DateTimeKind.Utc)
            };

            var hoursRemaining = (dueDateUtc - DateTime.UtcNow).TotalHours;
            if (hoursRemaining < 0 || hoursRemaining > 24)
            {
                return;
            }

            var relatedId = task.Id.ToString();
            var alreadyExists = await _context.Notifications
                .AsNoTracking()
                .AnyAsync(n => n.UserId == task.AssignedUserId
                               && n.Type == NotificationType.TaskDueSoon
                               && n.RelatedEntityId == relatedId,
                    cancellationToken);

            if (alreadyExists)
            {
                return;
            }

            await CreateNotificationAsync(new NotificationCreateRequest
            {
                UserId = task.AssignedUserId,
                Title = $"یادآوری مهلت تسک",
                Message = $"موعد انجام تسک «{task.Title}» در پروژه «{projectName ?? "پروژه"}» نزدیک است.",
                RelatedEntityId = relatedId,
                RelatedEntityType = nameof(TaskItem),
                Type = NotificationCreateType.TaskDueSoon,
                PayloadJson = JsonSerializer.Serialize(new
                {
                    taskId = task.Id,
                    dueDate = task.DueDate?.ToString("o")
                })
            }, cancellationToken);
        }

        private static Notification BuildNotification(NotificationCreateRequest request)
        {
            return new Notification
            {
                UserId = request.UserId,
                Title = request.Title,
                Message = request.Message,
                PayloadJson = request.PayloadJson,
                RelatedEntityId = request.RelatedEntityId,
                RelatedEntityType = request.RelatedEntityType,
                ExpiresAt = request.ExpiresAt,
                Type = MapType(request.Type),
                CreatedAt = DateTime.UtcNow
            };
        }

        private static NotificationType MapType(NotificationCreateType type)
        {
            return type switch
            {
                NotificationCreateType.TaskAssigned => NotificationType.TaskAssigned,
                NotificationCreateType.TaskDueSoon => NotificationType.TaskDueSoon,
                NotificationCreateType.TaskOverdue => NotificationType.TaskOverdue,
                NotificationCreateType.ProjectInvitation => NotificationType.ProjectInvitation,
                NotificationCreateType.CommentMention => NotificationType.CommentMention,
                NotificationCreateType.LeadInvitation => NotificationType.LeadInvitation,
                _ => NotificationType.General
            };
        }
    }
}

