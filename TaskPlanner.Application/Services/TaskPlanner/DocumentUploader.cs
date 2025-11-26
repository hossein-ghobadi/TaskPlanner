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

        private const string UploadUrl = "https://shidatis.com/api/upload/file"; // URL مخصوص اسناد

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

            using var client = _httpClientFactory.CreateClient();
            client.DefaultRequestHeaders.ExpectContinue = false;
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");

            using var form = new MultipartFormDataContent();
            var mime = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;

            var part = new ByteArrayContent(bytes);
            part.Headers.ContentType = new MediaTypeHeaderValue(mime);
            var safeName = Path.GetFileName(string.IsNullOrWhiteSpace(file.FileName) ? "upload.bin" : file.FileName);
            part.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = "\"file\"",
                FileName = $"\"{safeName}\""
            };
            form.Add(part);

            using var req = new HttpRequestMessage(HttpMethod.Post, UploadUrl)
            {
                Content = form,
                Version = new Version(1, 1),
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
            };

            var res = await client.PostAsync(UploadUrl, form, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            
            _logger.LogInformation("Response: {StatusCode} - {Body}", res.StatusCode, body);
            
            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"Upload failed ({(int)res.StatusCode}): {body}");
                
            var parsed = JsonSerializer.Deserialize<UploadResponse>(body, _jsonOptions)
                         ?? throw new InvalidOperationException("Invalid response (null)");
            if (string.IsNullOrWhiteSpace(parsed.data))
                throw new InvalidOperationException("Invalid response (data empty)");

            _logger.LogInformation("✅ آپلود موفق: {Url}", parsed.data);
            return parsed.data;
        }

        private static string GetSafeFileName(string? fileName)
        {
            if (string.IsNullOrWhiteSpace(fileName))
                return "document.pdf";

            var extension = Path.GetExtension(fileName);
            if (ContainsNonAscii(fileName))
            {
                return $"document_{DateTime.Now:yyyyMMdd_HHmmss}{extension}";
            }

            return fileName;
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
