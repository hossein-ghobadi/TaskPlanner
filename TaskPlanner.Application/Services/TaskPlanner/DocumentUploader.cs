using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace TaskPlanner.Application.Services.TaskPlanner
{
    public class DocumentUploader : IDocumentUploader
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly ILogger<DocumentUploader> _logger;
        private static readonly JsonSerializerOptions _jsonOptions = new(JsonSerializerDefaults.Web);

        // endpoint اصلی اسناد روی shidatis.com فعلاً 500 می‌دهد؛ چند مسیر جایگزین امتحان می‌شود
        private static readonly string[] UploadUrls =
        {
            "https://shidatis.com/api/upload/file",
            "https://shidatis.com/api/upload/image",
            "https://shidatis.ir/api/upload/file",
            "https://shidatis.ir/api/upload/image"
        };

        public DocumentUploader(IHttpClientFactory httpClientFactory, ILogger<DocumentUploader> logger)
        {
            _httpClientFactory = httpClientFactory;
            _logger = logger;
        }

        public async Task<string> UploadAsync(IFormFile file, string folder, CancellationToken ct = default)
        {
            if (file == null || file.Length == 0)
                throw new ArgumentException("Empty file");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            var bytes = ms.ToArray();

            var mime = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;
            var safeName = GetSafeFileName(file.FileName);
            var errors = new List<string>();

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.ExpectContinue = false;
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            client.Timeout = TimeSpan.FromMinutes(2);

            foreach (var uploadUrl in UploadUrls)
            {
                try
                {
                    using var form = new MultipartFormDataContent();
                    var part = new ByteArrayContent(bytes);
                    part.Headers.ContentType = new MediaTypeHeaderValue(mime);
                    part.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
                    {
                        Name = "\"file\"",
                        FileName = $"\"{safeName}\""
                    };
                    form.Add(part);

                    // بعضی APIها پوشه را هم می‌خواهند
                    if (!string.IsNullOrWhiteSpace(folder))
                    {
                        form.Add(new StringContent(folder), "folder");
                    }

                    var res = await client.PostAsync(uploadUrl, form, ct);
                    var body = await res.Content.ReadAsStringAsync(ct);

                    _logger.LogInformation("Document upload via {Url}: {StatusCode} - {Body}",
                        uploadUrl, res.StatusCode, body);

                    if (!res.IsSuccessStatusCode)
                    {
                        errors.Add($"{uploadUrl} => {(int)res.StatusCode}");
                        continue;
                    }

                    var parsed = JsonSerializer.Deserialize<UploadResponse>(body, _jsonOptions);
                    var url = parsed?.data;
                    if (string.IsNullOrWhiteSpace(url))
                    {
                        errors.Add($"{uploadUrl} => empty data");
                        continue;
                    }

                    _logger.LogInformation("✅ آپلود سند موفق از {Url}: {FileUrl}", uploadUrl, url);
                    return url;
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Document upload failed via {Url}", uploadUrl);
                    errors.Add($"{uploadUrl} => {ex.Message}");
                }
            }

            try
            {
                var localUrl = await SaveLocalFallbackAsync(bytes, folder, safeName, ct);
                _logger.LogWarning(
                    "Remote document upload failed; saved locally at {LocalUrl}. Errors: {Errors}",
                    localUrl,
                    string.Join(" | ", errors));
                return localUrl;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Local document fallback also failed");
                throw new InvalidOperationException(
                    "آپلود فایل ناموفق بود. " + string.Join(" | ", errors), ex);
            }
        }

        private static async Task<string> SaveLocalFallbackAsync(byte[] bytes, string folder, string safeName, CancellationToken ct)
        {
            var safeFolder = string.IsNullOrWhiteSpace(folder)
                ? "documents"
                : string.Concat(folder.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_'));
            if (string.IsNullOrWhiteSpace(safeFolder))
                safeFolder = "documents";

            var webRoot = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot", "uploads", safeFolder);
            Directory.CreateDirectory(webRoot);

            var localName = $"{Guid.NewGuid():N}{Path.GetExtension(safeName)}";
            var localPath = Path.Combine(webRoot, localName);
            await File.WriteAllBytesAsync(localPath, bytes, ct);

            // مسیر نسبی اپ — در ToPublicUrl دست‌نخورده می‌ماند
            return $"/uploads/{safeFolder}/{localName}";
        }

        private static string GetSafeFileName(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return $"document_{DateTime.UtcNow:yyyyMMdd_HHmmss}.bin";

            var extension = Path.GetExtension(fileName);
            if (string.IsNullOrWhiteSpace(extension))
                extension = ".bin";

            // نام فارسی/غیر ASCII باعث خطای بعضی APIهای آپلود می‌شود
            if (ContainsNonAscii(fileName))
            {
                return $"document_{DateTime.UtcNow:yyyyMMdd_HHmmss}{extension.ToLowerInvariant()}";
            }

            var baseName = Path.GetFileNameWithoutExtension(fileName);
            var cleaned = new string(baseName.Where(c => char.IsLetterOrDigit(c) || c == '-' || c == '_').ToArray());
            if (string.IsNullOrWhiteSpace(cleaned))
                cleaned = "document";

            return $"{cleaned}{extension.ToLowerInvariant()}";
        }

        private static bool ContainsNonAscii(string input)
        {
            if (string.IsNullOrEmpty(input)) return false;
            return input.Any(c => c > 127);
        }

        private sealed class UploadResponse
        {
            public string? data { get; set; }
            public string? message { get; set; }
            public int status { get; set; }
        }
    }
}
