using Microsoft.AspNetCore.Http;
using System.Threading.Tasks;

namespace TaskPlanner.Application.Services.FileUpload
{
    public interface IFileUploadService
    {
        Task<(bool Success, string FilePath, string Error)> UploadFileAsync(IFormFile file, string folder);
        bool DeleteFile(string filePath);
        string GetFileType(string fileName);

        /// <summary>
        /// چسباندن FILE_BASE_URL به مسیر ذخیره‌شده برای نمایش.
        /// </summary>
        string ToPublicUrl(string? storagePath);

        /// <summary>
        /// جدا کردن base URL برای ذخیره در دیتابیس.
        /// </summary>
        string ToStoragePath(string? urlOrPath);
    }
}
