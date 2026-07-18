namespace TaskPlanner.Application.Services.FileUpload
{
    public interface IFileUrlService
    {
        string BaseUrl { get; }

        /// <summary>
        /// مسیر نسبی برای ذخیره در دیتابیس (بدون base URL).
        /// </summary>
        string ToStoragePath(string? urlOrPath);

        /// <summary>
        /// URL عمومی برای نمایش/دانلود (base URL از .env + مسیر ذخیره‌شده).
        /// </summary>
        string ToPublicUrl(string? storagePath);
    }
}
