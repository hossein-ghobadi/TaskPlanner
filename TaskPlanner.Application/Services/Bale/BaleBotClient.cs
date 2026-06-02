using System.Text.Json;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace TaskPlanner.Application.Services.Bale
{
    public interface IBaleBotClient
    {
        Task<BaleClientResult<IReadOnlyList<BaleUpdate>>> GetUpdatesAsync(string botToken, int offset, int timeoutSeconds, CancellationToken cancellationToken = default);
        Task<BaleClientResult<BaleUser>> GetMeAsync(string botToken, CancellationToken cancellationToken = default);
        Task<BaleClientResult<BaleChat>> GetChatAsync(string botToken, string chatId, CancellationToken cancellationToken = default);
        Task<BaleClientResult<BaleFile>> GetFileAsync(string botToken, string fileId, CancellationToken cancellationToken = default);
        Task<BaleClientResult<byte[]>> DownloadFileBytesAsync(string botToken, string filePath, CancellationToken cancellationToken = default);
        Task<BaleClientResult<BaleUserProfilePhotos>> GetUserProfilePhotosAsync(string botToken, long userId, CancellationToken cancellationToken = default);
        Task<BaleClientResult<BaleChatMember>> GetChatMemberAsync(string botToken, long chatId, long userId, CancellationToken cancellationToken = default);
    }

    public class BaleBotClient : IBaleBotClient
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<BaleBotClient> _logger;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true
        };

        public BaleBotClient(
            IHttpClientFactory httpClientFactory,
            IConfiguration configuration,
            ILogger<BaleBotClient> logger)
        {
            _httpClientFactory = httpClientFactory;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<BaleClientResult<IReadOnlyList<BaleUpdate>>> GetUpdatesAsync(string botToken, int offset, int timeoutSeconds, CancellationToken cancellationToken = default)
        {
            var result = await SendAsync<BaleUpdate[]>(
                botToken,
                "getUpdates",
                new Dictionary<string, string?>
                {
                    ["offset"] = offset.ToString(),
                    ["timeout"] = timeoutSeconds.ToString(),
                    ["limit"] = "100",
                    ["allowed_updates"] = "[\"message\",\"edited_message\",\"channel_post\",\"edited_channel_post\"]"
                },
                cancellationToken);

            if (!result.Success)
            {
                return BaleClientResult<IReadOnlyList<BaleUpdate>>.Fail(result.ErrorDescription, result.ErrorCode);
            }

            return BaleClientResult<IReadOnlyList<BaleUpdate>>.Ok(result.Data ?? Array.Empty<BaleUpdate>());
        }

        public Task<BaleClientResult<BaleUser>> GetMeAsync(string botToken, CancellationToken cancellationToken = default)
        {
            return SendAsync<BaleUser>(botToken, "getMe", null, cancellationToken);
        }

        public Task<BaleClientResult<BaleChat>> GetChatAsync(string botToken, string chatId, CancellationToken cancellationToken = default)
        {
            return SendAsync<BaleChat>(botToken, "getChat", new Dictionary<string, string?>
            {
                ["chat_id"] = chatId
            }, cancellationToken);
        }

        public Task<BaleClientResult<BaleFile>> GetFileAsync(string botToken, string fileId, CancellationToken cancellationToken = default)
        {
            return SendAsync<BaleFile>(botToken, "getFile", new Dictionary<string, string?>
            {
                ["file_id"] = fileId
            }, cancellationToken);
        }

        public async Task<BaleClientResult<byte[]>> DownloadFileBytesAsync(string botToken, string filePath, CancellationToken cancellationToken = default)
        {
            var token = BaleTokenHelper.Normalize(botToken);
            if (string.IsNullOrEmpty(token))
            {
                return BaleClientResult<byte[]>.Fail("توکن بازو خالی است.");
            }

            if (string.IsNullOrWhiteSpace(filePath))
            {
                return BaleClientResult<byte[]>.Fail("مسیر فایل بله خالی است.");
            }

            var baseUrl = _configuration[$"{BaleBotOptions.SectionName}:ApiBaseUrl"]?.Trim()
                ?? "https://tapi.bale.ai";
            baseUrl = baseUrl.TrimEnd('/');
            var url = $"{baseUrl}/file/bot{token}/{filePath.TrimStart('/')}";

            try
            {
                var client = _httpClientFactory.CreateClient("BaleBot");
                using var response = await client.GetAsync(url, cancellationToken);
                if (!response.IsSuccessStatusCode)
                {
                    var body = await response.Content.ReadAsStringAsync(cancellationToken);
                    _logger.LogWarning("Bale file download HTTP {Status}: {Body}", response.StatusCode, body);
                    return BaleClientResult<byte[]>.Fail($"خطا در دانلود فایل ({(int)response.StatusCode})");
                }

                var bytes = await response.Content.ReadAsByteArrayAsync(cancellationToken);
                return BaleClientResult<byte[]>.Ok(bytes);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bale file download failed for {Path}", filePath);
                return BaleClientResult<byte[]>.Fail("دانلود فایل از بله ناموفق بود.");
            }
        }

        public Task<BaleClientResult<BaleUserProfilePhotos>> GetUserProfilePhotosAsync(string botToken, long userId, CancellationToken cancellationToken = default)
        {
            return SendAsync<BaleUserProfilePhotos>(botToken, "getUserProfilePhotos", new Dictionary<string, string?>
            {
                ["user_id"] = userId.ToString(),
                ["limit"] = "1"
            }, cancellationToken);
        }

        public Task<BaleClientResult<BaleChatMember>> GetChatMemberAsync(
            string botToken,
            long chatId,
            long userId,
            CancellationToken cancellationToken = default)
        {
            return SendAsync<BaleChatMember>(botToken, "getChatMember", new Dictionary<string, string?>
            {
                ["chat_id"] = chatId.ToString(),
                ["user_id"] = userId.ToString()
            }, cancellationToken);
        }

        private async Task<BaleClientResult<T>> SendAsync<T>(string botToken, string method, Dictionary<string, string?>? queryParams, CancellationToken cancellationToken)
        {
            var token = BaleTokenHelper.Normalize(botToken);
            if (string.IsNullOrEmpty(token))
            {
                return BaleClientResult<T>.Fail("توکن بازو خالی است.");
            }

            var baseUrl = _configuration[$"{BaleBotOptions.SectionName}:ApiBaseUrl"]?.Trim()
                ?? "https://tapi.bale.ai";
            baseUrl = baseUrl.TrimEnd('/');

            var url = $"{baseUrl}/bot{token}/{method}";
            if (queryParams is { Count: > 0 })
            {
                var qs = string.Join("&", queryParams
                    .Where(p => p.Value != null)
                    .Select(p => $"{Uri.EscapeDataString(p.Key)}={Uri.EscapeDataString(p.Value!)}"));
                url += "?" + qs;
            }

            try
            {
                var client = _httpClientFactory.CreateClient("BaleBot");
                using var response = await client.GetAsync(url, cancellationToken);
                var json = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Bale API {Method} HTTP {Status}: {Body}", method, response.StatusCode, json);
                    return BaleClientResult<T>.Fail($"خطای ارتباط با سرور بله ({(int)response.StatusCode})");
                }

                var envelope = JsonSerializer.Deserialize<BaleApiResponse<T>>(json, JsonOptions);
                if (envelope == null)
                {
                    return BaleClientResult<T>.Fail("پاسخ نامعتبر از سرور بله.");
                }

                if (!envelope.Ok)
                {
                    _logger.LogWarning("Bale API {Method} error {Code}: {Description}", method, envelope.ErrorCode, envelope.Description);
                    return BaleClientResult<T>.Fail(envelope.Description ?? "خطا از سمت API بله", envelope.ErrorCode);
                }

                if (envelope.Result == null)
                {
                    return BaleClientResult<T>.Fail("پاسخ خالی از سرور بله.");
                }

                return BaleClientResult<T>.Ok(envelope.Result);
            }
            catch (TaskCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Bale API {Method} request failed", method);
                return BaleClientResult<T>.Fail("اتصال به سرور بله برقرار نشد. اینترنت یا فیلترینگ را بررسی کنید.");
            }
        }
    }
}
