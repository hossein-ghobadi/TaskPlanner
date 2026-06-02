using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using TaskPlanner.Application.Interfaces.Contexts;
using TaskPlanner.Application.Services.TaskPlanner;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace TaskPlanner.Application.Services.Bale
{
    public interface IBaleMediaIngestService
    {
        Task<IReadOnlyList<ProjectChatMessageAttachment>> IngestMessageMediaAsync(
            string botToken,
            int messageEntityId,
            BaleMessage baleMessage,
            CancellationToken cancellationToken = default);

        Task<string?> ResolveSenderPhotoPathAsync(
            string botToken,
            int groupId,
            long? senderBaleId,
            IMVPTestDatabaseContext context,
            CancellationToken cancellationToken = default);
    }

    public sealed class BaleMediaIngestService : IBaleMediaIngestService
    {
        private readonly IBaleBotClient _baleBotClient;
        private readonly IImageUploader _imageUploader;
        private readonly ILogger<BaleMediaIngestService> _logger;

        public BaleMediaIngestService(
            IBaleBotClient baleBotClient,
            IImageUploader imageUploader,
            ILogger<BaleMediaIngestService> logger)
        {
            _baleBotClient = baleBotClient;
            _imageUploader = imageUploader;
            _logger = logger;
        }

        public async Task<IReadOnlyList<ProjectChatMessageAttachment>> IngestMessageMediaAsync(
            string botToken,
            int messageEntityId,
            BaleMessage baleMessage,
            CancellationToken cancellationToken = default)
        {
            var attachments = new List<ProjectChatMessageAttachment>();

            var photoFileId = GetLargestPhotoFileId(baleMessage.Photo);
            if (!string.IsNullOrEmpty(photoFileId))
            {
                var photoAttachment = await DownloadAndUploadImageAsync(
                    botToken,
                    photoFileId,
                    $"bale_photo_{baleMessage.MessageId}.jpg",
                    cancellationToken);
                if (photoAttachment != null)
                {
                    photoAttachment.ProjectChatMessageId = messageEntityId;
                    attachments.Add(photoAttachment);
                }
            }

            if (attachments.Count > 0)
            {
                return attachments;
            }

            var document = baleMessage.Document;
            if (document != null
                && !string.IsNullOrEmpty(document.FileId)
                && IsImageMime(document.MimeType))
            {
                var ext = Path.GetExtension(document.FileName ?? ".jpg");
                if (string.IsNullOrWhiteSpace(ext))
                {
                    ext = ".jpg";
                }

                var docAttachment = await DownloadAndUploadImageAsync(
                    botToken,
                    document.FileId,
                    $"bale_doc_{baleMessage.MessageId}{ext}",
                    cancellationToken);
                if (docAttachment != null)
                {
                    docAttachment.ProjectChatMessageId = messageEntityId;
                    attachments.Add(docAttachment);
                }
            }

            return attachments;
        }

        public async Task<string?> ResolveSenderPhotoPathAsync(
            string botToken,
            int groupId,
            long? senderBaleId,
            IMVPTestDatabaseContext context,
            CancellationToken cancellationToken = default)
        {
            if (!senderBaleId.HasValue)
            {
                return null;
            }

            var cached = await context.ProjectChatMessages
                .AsNoTracking()
                .Where(m =>
                    m.ProjectChatGroupId == groupId &&
                    m.ExternalSenderBaleId == senderBaleId &&
                    m.ExternalSenderPhotoPath != null)
                .Select(m => m.ExternalSenderPhotoPath)
                .FirstOrDefaultAsync(cancellationToken);

            if (!string.IsNullOrWhiteSpace(cached))
            {
                return cached;
            }

            var photosResult = await _baleBotClient.GetUserProfilePhotosAsync(botToken, senderBaleId.Value, cancellationToken);
            if (!photosResult.Success || photosResult.Data?.Photos == null || photosResult.Data.Photos.Count == 0)
            {
                return null;
            }

            var sizes = photosResult.Data.Photos[0];
            var fileId = GetLargestPhotoFileId(sizes);
            if (string.IsNullOrEmpty(fileId))
            {
                return null;
            }

            var attachment = await DownloadAndUploadImageAsync(
                botToken,
                fileId,
                $"bale_avatar_{senderBaleId}.jpg",
                cancellationToken);

            return attachment?.FilePath;
        }

        private async Task<ProjectChatMessageAttachment?> DownloadAndUploadImageAsync(
            string botToken,
            string fileId,
            string fileName,
            CancellationToken cancellationToken)
        {
            var fileResult = await _baleBotClient.GetFileAsync(botToken, fileId, cancellationToken);
            if (!fileResult.Success || string.IsNullOrWhiteSpace(fileResult.Data?.FilePath))
            {
                _logger.LogWarning("Bale getFile failed for {FileId}: {Error}", fileId, fileResult.ErrorDescription);
                return null;
            }

            var bytesResult = await _baleBotClient.DownloadFileBytesAsync(botToken, fileResult.Data.FilePath, cancellationToken);
            if (!bytesResult.Success || bytesResult.Data == null || bytesResult.Data.Length == 0)
            {
                _logger.LogWarning("Bale download failed for {FileId}: {Error}", fileId, bytesResult.ErrorDescription);
                return null;
            }

            try
            {
                var formFile = new ByteArrayFormFile(bytesResult.Data, fileName, "image/jpeg");
                var url = await _imageUploader.UploadAsync(formFile, "project-team-chat/bale", cancellationToken);
                return new ProjectChatMessageAttachment
                {
                    FileName = fileName,
                    FilePath = url,
                    FileType = "Image",
                    FileSize = bytesResult.Data.Length,
                    MimeType = "image/jpeg",
                    UploadedAt = DateTime.UtcNow
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Uploading Bale image {FileName} failed", fileName);
                return null;
            }
        }

        private static string? GetLargestPhotoFileId(List<BalePhotoSize>? sizes)
        {
            if (sizes == null || sizes.Count == 0)
            {
                return null;
            }

            var largest = sizes
                .Where(s => !string.IsNullOrEmpty(s.FileId))
                .OrderByDescending(s => s.Width * s.Height)
                .ThenByDescending(s => s.FileSize ?? 0)
                .FirstOrDefault();

            return largest?.FileId;
        }

        private static bool IsImageMime(string? mimeType)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                return false;
            }

            return mimeType.StartsWith("image/", StringComparison.OrdinalIgnoreCase);
        }
    }

    internal sealed class ByteArrayFormFile : IFormFile
    {
        private readonly byte[] _bytes;

        public ByteArrayFormFile(byte[] bytes, string fileName, string contentType)
        {
            _bytes = bytes;
            FileName = fileName;
            ContentType = contentType;
        }

        public string ContentType { get; }
        public string ContentDisposition => $"form-data; name=\"file\"; filename=\"{FileName}\"";
        public IHeaderDictionary Headers { get; } = new HeaderDictionary();
        public long Length => _bytes.Length;
        public string Name => "file";
        public string FileName { get; }

        public Stream OpenReadStream() => new MemoryStream(_bytes);

        public void CopyTo(Stream target) => target.Write(_bytes, 0, _bytes.Length);

        public Task CopyToAsync(Stream target, CancellationToken cancellationToken = default) =>
            target.WriteAsync(_bytes, cancellationToken).AsTask();
    }
}
