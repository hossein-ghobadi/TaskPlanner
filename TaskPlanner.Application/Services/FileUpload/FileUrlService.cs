namespace TaskPlanner.Application.Services.FileUpload
{
    public sealed class FileUrlService : IFileUrlService
    {
        private readonly string _baseUrl;
        private readonly string[] _stripPrefixes;

        public FileUrlService()
        {
            _baseUrl = NormalizeBase(
                Environment.GetEnvironmentVariable("FILE_BASE_URL")
                ?? Environment.GetEnvironmentVariable("STORAGE_BASE_URL")
                ?? string.Empty);

            var prefixes = new List<string>();
            if (!string.IsNullOrEmpty(_baseUrl))
            {
                prefixes.Add(_baseUrl);
            }

            var legacy = Environment.GetEnvironmentVariable("FILE_LEGACY_BASE_URLS");
            if (!string.IsNullOrWhiteSpace(legacy))
            {
                foreach (var part in legacy.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var normalized = NormalizeBase(part);
                    if (!string.IsNullOrEmpty(normalized))
                    {
                        prefixes.Add(normalized);
                    }
                }
            }

            // دامنه‌های قبلی پروژه (URL کامل قدیمی → مسیر نسبی)
            prefixes.Add("https://shidatis1.storage.iran.liara.space");
            prefixes.Add("http://shidatis1.storage.iran.liara.space");
            prefixes.Add("https://shidatis.com");
            prefixes.Add("http://shidatis.com");
            prefixes.Add("https://shidatis.ir");
            prefixes.Add("http://shidatis.ir");

            _stripPrefixes = prefixes
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderByDescending(p => p.Length)
                .ToArray();
        }

        public string BaseUrl => _baseUrl;

        public string ToStoragePath(string? urlOrPath)
        {
            if (string.IsNullOrWhiteSpace(urlOrPath))
            {
                return urlOrPath ?? string.Empty;
            }

            var value = urlOrPath.Trim();
            foreach (var prefix in _stripPrefixes)
            {
                if (value.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
                {
                    value = value[prefix.Length..];
                    break;
                }
            }

            return value.TrimStart('/');
        }

        public string ToPublicUrl(string? storagePath)
        {
            if (string.IsNullOrWhiteSpace(storagePath))
            {
                return storagePath ?? string.Empty;
            }

            var value = storagePath.Trim();

            // blob:/data: را دست نزن
            if (value.StartsWith("blob:", StringComparison.OrdinalIgnoreCase)
                || value.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            {
                return value;
            }

            // داده قدیمی (URL کامل) هم اول نسبی می‌شود، بعد با FILE_BASE_URL فعلی چسبانده می‌شود
            var relative = ToStoragePath(value);

            // اگر هنوز absolute بود (دامنه ناشناخته)، همان را برگردان
            if (relative.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                || relative.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
            {
                return relative;
            }

            // آپلود محلی روی خود اپ (wwwroot/uploads) — به CDN نچسبان
            var normalizedRelative = relative.TrimStart('/');
            if (normalizedRelative.StartsWith("uploads/", StringComparison.OrdinalIgnoreCase))
            {
                return "/" + normalizedRelative;
            }

            if (string.IsNullOrEmpty(_baseUrl))
            {
                return relative.StartsWith('/') ? relative : "/" + relative;
            }

            return $"{_baseUrl}/{normalizedRelative}";
        }

        private static string NormalizeBase(string? value)
        {
            return (value ?? string.Empty).Trim().TrimEnd('/');
        }
    }

    /// <summary>
    /// دسترسی استاتیک برای Viewها و کلاس‌های static (مثل BaleMessageParser).
    /// </summary>
    public static class FileUrls
    {
        private static readonly Lazy<IFileUrlService> Instance = new(() => new FileUrlService());

        public static string ToPublicUrl(string? storagePath) => Instance.Value.ToPublicUrl(storagePath);

        public static string ToStoragePath(string? urlOrPath) => Instance.Value.ToStoragePath(urlOrPath);
    }
}
