using System;

namespace TaskPlanner.Application.Services.NotificationService
{
    public class NotificationDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string Type { get; set; } = null!;
        public bool IsRead { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? ReadAt { get; set; }
        public string? PayloadJson { get; set; }
        public string? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
    }

    public class NotificationCreateRequest
    {
        public string UserId { get; set; } = null!;
        public string Title { get; set; } = null!;
        public string Message { get; set; } = null!;
        public string? PayloadJson { get; set; }
        public string? RelatedEntityId { get; set; }
        public string? RelatedEntityType { get; set; }
        public NotificationCreateType Type { get; set; } = NotificationCreateType.General;
        public DateTime? ExpiresAt { get; set; }
    }

    public enum NotificationCreateType
    {
        General = 0,
        TaskAssigned = 1,
        TaskDueSoon = 2,
        TaskOverdue = 3,
        ProjectInvitation = 4,
        CommentMention = 5,
        LeadInvitation = 6
    }
}

