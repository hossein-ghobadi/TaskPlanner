using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using TaskPlanner.Application.Services.TaskPlanner;

namespace TaskPlanner.Application.Services.FileUpload
{
    public class FileUploadService : IFileUploadService
    {
        private readonly string[] _allowedImageExtensions = { ".jpg", ".jpeg", ".png", ".gif", ".webp", ".svg" };
        private readonly string[] _allowedAudioExtensions = { ".mp3", ".wav", ".ogg", ".m4a", ".webm" };
        private readonly string[] _allowedDocumentExtensions = { ".pdf", ".doc", ".docx", ".xls", ".xlsx", ".txt", ".zip", ".rar" };
        
        private const long MaxFileSize = 10 * 1024 * 1024; // 10 MB
        private readonly IImageUploader _imageUploader;
        private readonly IVoiceUploader _voiceUploader;
        private readonly IDocumentUploader _documentUploader;
        private readonly IFileUrlService _fileUrlService;
        private readonly ILogger<FileUploadService> _logger;

        public FileUploadService(
            IImageUploader imageUploader,
            IVoiceUploader voiceUploader,
            IDocumentUploader documentUploader,
            IFileUrlService fileUrlService,
            ILogger<FileUploadService> logger)
        {
            _imageUploader = imageUploader;
            _voiceUploader = voiceUploader;
            _documentUploader = documentUploader;
            _fileUrlService = fileUrlService;
            _logger = logger;
        }

        public async Task<(bool Success, string FilePath, string Error)> UploadFileAsync(IFormFile file, string folder)
        {
            try
            {
                _logger.LogInformation("شروع آپلود فایل: {FileName}, سایز: {Size} بایت, پوشه: {Folder}", 
                    file?.FileName, file?.Length, folder);

                if (file == null || file.Length == 0)
                {
                    _logger.LogWarning("فایل خالی یا null است");
                    return (false, string.Empty, "فایلی انتخاب نشده است");
                }

                if (file.Length > MaxFileSize)
                {
                    _logger.LogWarning("حجم فایل {Size} بیشتر از حد مجاز {MaxSize} است", file.Length, MaxFileSize);
                    return (false, string.Empty, $"حجم فایل نباید بیشتر از {MaxFileSize / (1024 * 1024)} مگابایت باشد");
                }

                var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                
                if (!IsAllowedExtension(extension))
                {
                    _logger.LogWarning("فرمت فایل {Extension} مجاز نیست", extension);
                    return (false, string.Empty, "فرمت فایل مجاز نیست");
                }

                var fileType = GetFileType(file.FileName);
                string remoteUrl;
                
                switch (fileType)
                {
                    case "Image":
                        _logger.LogInformation("📸 آپلود تصویر به باکت...");
                        remoteUrl = await _imageUploader.UploadAsync(file, folder);
                        break;
                    case "Audio":
                        _logger.LogInformation("🎤audio آپلود فایل صوتی به باکت...");
                        remoteUrl = await _voiceUploader.UploadAsync(file, folder);
                        break;
                    case "Document":
                        _logger.LogInformation("📄 آپلود سند به باکت...");
                        remoteUrl = await _documentUploader.UploadAsync(file, folder);
                        break;
                    default:
                        _logger.LogInformation("📁 آپلود فایل عمومی به باکت...");
                        remoteUrl = await _documentUploader.UploadAsync(file, folder);
                        break;
                }

                // فقط مسیر نسبی در دیتابیس ذخیره می‌شود
                var storagePath = _fileUrlService.ToStoragePath(remoteUrl);
                
                _logger.LogInformation("✅ آپلود به باکت موفقیت‌آمیز: {Url} → ذخیره: {StoragePath}", remoteUrl, storagePath);
                return (true, storagePath, string.Empty);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ خطا در آپلود فایل {FileName}", file?.FileName);
                return (false, string.Empty, $"خطا در آپلود فایل: {ex.Message}");
            }
        }

        public string ToPublicUrl(string? storagePath) => _fileUrlService.ToPublicUrl(storagePath);

        public string ToStoragePath(string? urlOrPath) => _fileUrlService.ToStoragePath(urlOrPath);

        public bool DeleteFile(string filePath)
        {
            try
            {
                _logger.LogInformation("درخواست حذف فایل: {FilePath}", filePath);
                
                if (string.IsNullOrEmpty(filePath))
                    return false;

                if (filePath.StartsWith("http://", StringComparison.OrdinalIgnoreCase) || 
                    filePath.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
                {
                    _logger.LogInformation("فایل در باکت است - حذف نمی‌شود");
                    return true;
                }
                
                var fullPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", filePath.TrimStart('/'));
                
                if (File.Exists(fullPath))
                {
                    File.Delete(fullPath);
                    _logger.LogInformation("✅ فایل local حذف شد: {Path}", fullPath);
                    return true;
                }

                // مسیر نسبی remote در wwwroot نیست
                _logger.LogInformation("فایل local پیدا نشد (احتمالاً remote): {Path}", filePath);
                return true;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "خطا در حذف فایل: {FilePath}", filePath);
                return false;
            }
        }

        public string GetFileType(string fileName)
        {
            var extension = Path.GetExtension(fileName).ToLowerInvariant();
            
            if (_allowedImageExtensions.Contains(extension))
                return "Image";
            
            if (_allowedAudioExtensions.Contains(extension))
                return "Audio";
            
            if (_allowedDocumentExtensions.Contains(extension))
                return "Document";
            
            return "Other";
        }

        private bool IsAllowedExtension(string extension)
        {
            return _allowedImageExtensions.Contains(extension) ||
                   _allowedAudioExtensions.Contains(extension) ||
                   _allowedDocumentExtensions.Contains(extension);
        }
    }
}
