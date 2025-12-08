using TaskPlanner.Domain.Entities.Boards;

namespace Endpoint.Site.Models
{
    public class BoardTaskCommentVm
    {
        public int Id { get; set; }
        public string? Message { get; set; }
        public int BoardTaskId { get; set; }
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsEdited { get; set; }
        public bool IsDeleted { get; set; }
        public List<BoardTaskCommentAttachmentVm> Attachments { get; set; } = new();
    }

    public class BoardTaskCommentAttachmentVm
    {
        public int Id { get; set; }
        public string FileName { get; set; } = null!;
        public string FilePath { get; set; } = null!;
        public string FileType { get; set; } = null!;
        public long FileSize { get; set; }
        public string? MimeType { get; set; }
        public DateTime UploadedAt { get; set; }
    }
}

