using Microsoft.AspNetCore.Http;
using System.Threading;
using System.Threading.Tasks;

namespace TaskPlanner.Application.Services.TaskPlanner
{
    /// <summary>
    /// سرویس آپلود فایل‌های TaskPlanner به باکت
    /// </summary>
    public interface ITaskPlannerFileUploader
    {
        /// <summary>
        /// آپلود فایل به باکت در پوشه TaskPlanner
        /// </summary>
        /// <param name="file">فایل برای آپلود</param>
        /// <param name="subFolder">زیرپوشه (مثل: task-comments, project-notes, personal-notes)</param>
        /// <param name="ct">Cancellation Token</param>
        /// <returns>URL نهایی فایل در باکت</returns>
        Task<string> UploadAsync(IFormFile file, string subFolder, CancellationToken ct = default);
    }

    /// <summary>
    /// سرویس آپلود تصاویر
    /// </summary>
    public interface IImageUploader
    {
        Task<string> UploadAsync(IFormFile file, string folder, CancellationToken ct = default);
    }

    /// <summary>
    /// سرویس آپلود فایل‌های صوتی
    /// </summary>
    public interface IVoiceUploader
    {
        Task<string> UploadAsync(IFormFile file, string folder, CancellationToken ct = default);
    }

    /// <summary>
    /// سرویس آپلود اسناد
    /// </summary>
    public interface IDocumentUploader
    {
        Task<string> UploadAsync(IFormFile file, string folder, CancellationToken ct = default);
    }
}

