using System;
using TaskPlanner.Domain.Entities.Users;

namespace TaskPlanner.Domain.Entities.Notifications
{
    public class Notification
    {
        public int Id { get; set; }
        public string UserId { get; set; } = null!;
        public User User { get; set; } = null!;
        public NotificationType Type { get; set; } = NotificationType.General;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? PayloadJson { get; set; }
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ReadAt { get; set; }
        public DateTime? ExpiresAt { get; set; }
        public string? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
    }

    public enum NotificationType
    {
        General = 0,
        TaskAssigned = 1,
        TaskDueSoon = 2,
        TaskOverdue = 3,
        ProjectInvitation = 4,
        CommentMention = 5
    }
}

