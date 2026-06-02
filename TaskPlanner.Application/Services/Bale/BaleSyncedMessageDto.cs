namespace TaskPlanner.Application.Services.Bale
{
    public sealed class BaleSyncedMessageDto
    {
        public int Id { get; set; }
        public int GroupId { get; set; }
        public string UserId { get; set; } = null!;
        public string UserName { get; set; } = null!;
        public string Message { get; set; } = null!;
        public DateTime CreatedAt { get; set; }
        public bool IsExternal { get; set; } = true;
        public string? ExternalProvider { get; set; }
        public long? ExternalSenderBaleId { get; set; }
        public string? ExternalSenderUsername { get; set; }
        public string? ExternalSenderFirstName { get; set; }
        public string? ExternalSenderLastName { get; set; }
        public string? ExternalSenderPhone { get; set; }
        public bool ExternalIsChannelSender { get; set; }
        public string? ExternalSenderPhotoPath { get; set; }
        public List<BaleSyncedMessageAttachmentDto> Attachments { get; set; } = new();
    }

    public sealed class BaleSyncedMessageAttachmentDto
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
