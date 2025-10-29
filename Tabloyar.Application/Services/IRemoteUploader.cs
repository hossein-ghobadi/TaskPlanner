using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Net.Http.Headers;
using System.Text.Json;
using Microsoft.Extensions.Options;
using System.Net.Http;
using Microsoft.AspNetCore.Http;

namespace TaskPlanner.Application.Services
{
    public interface IRemoteUploader
    {
        Task<string> UploadAsync(IFormFile file, CancellationToken ct);

    }
    public class RemoteUploadOptions
    {
        public string Endpoint { get; set; } = "";
        public string? ApiKey { get; set; }
        public string FieldName { get; set; } = "file";
    }

    public class RemoteUploader : IRemoteUploader
    {
        private readonly IHttpClientFactory _hf;
        private readonly RemoteUploadOptions _opt;
        private static readonly JsonSerializerOptions _json = new(JsonSerializerDefaults.Web);

        public RemoteUploader(IHttpClientFactory hf, IOptions<RemoteUploadOptions> opt)
        {
            _hf = hf;
            _opt = opt.Value;
        }

        public async Task<string> UploadAsync(IFormFile file, CancellationToken ct = default)
        {
            if (file == null || file.Length == 0) throw new ArgumentException("Empty file");

            using var ms = new MemoryStream();
            await file.CopyToAsync(ms, ct);
            var bytes = ms.ToArray();

            using var client = _hf.CreateClient();
            client.DefaultRequestHeaders.ExpectContinue = false;
            client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            if (!string.IsNullOrWhiteSpace(_opt.ApiKey))
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", _opt.ApiKey);

            using var form = new MultipartFormDataContent();
            var mime = string.IsNullOrWhiteSpace(file.ContentType) ? "application/octet-stream" : file.ContentType;

            var part = new ByteArrayContent(bytes);
            part.Headers.ContentType = new MediaTypeHeaderValue(mime);
            var safeName = Path.GetFileName(string.IsNullOrWhiteSpace(file.FileName) ? "upload.bin" : file.FileName);
            part.Headers.ContentDisposition = new ContentDispositionHeaderValue("form-data")
            {
                Name = $"\"{_opt.FieldName}\"",
                FileName = $"\"{safeName}\""
            };
            form.Add(part);

            using var req = new HttpRequestMessage(HttpMethod.Post, _opt.Endpoint)
            {
                Content = form,
                Version = new Version(1, 1),
                VersionPolicy = HttpVersionPolicy.RequestVersionOrLower
            };
            client.BaseAddress = new Uri("https://tabloradin.com"); // همیشه با http/https شروع بشه

            var res = await client.PostAsync("/api/image-upload?type=TryOn", form, ct);
            var body = await res.Content.ReadAsStringAsync(ct);
            if (!res.IsSuccessStatusCode)
                throw new InvalidOperationException($"Upload failed ({(int)res.StatusCode}): {body}");
            var parsed = JsonSerializer.Deserialize<ImageUploadResponse>(body, _json)
                         ?? throw new InvalidOperationException("Invalid response (null)");
            if (string.IsNullOrWhiteSpace(parsed.image_url))
                throw new InvalidOperationException("Invalid response (image_url empty)");

            Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>  res Status {res.IsSuccessStatusCode}");
            Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>  res Status {res.IsSuccessStatusCode}");
            Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>  res Status {res.IsSuccessStatusCode}");
            Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>  res Status {res.IsSuccessStatusCode}");
            Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>  res Status {res.IsSuccessStatusCode}");
            Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>  res Status {res.IsSuccessStatusCode}");

            Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>  res Status {res.IsSuccessStatusCode}");
            return parsed.image_url; // ← URL نهایی فایل
        }





        private sealed class ImageUploadResponse
        {
            public string? image_url { get; set; }

        }
    }
}
