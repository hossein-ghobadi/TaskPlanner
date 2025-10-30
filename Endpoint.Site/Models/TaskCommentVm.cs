using System;
using System.Collections.Generic;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Models
{
    public class TaskCommentVm
    {
        public int Id { get; set; }
        public string? Message { get; set; }
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsEdited { get; set; }
        public bool IsCurrentUser { get; set; }
        public List<TaskCommentAttachment> Attachments { get; set; } = new List<TaskCommentAttachment>();
    }
}

