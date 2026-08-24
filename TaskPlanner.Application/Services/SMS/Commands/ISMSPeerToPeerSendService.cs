using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System.Text;
using TaskPlanner.Application.Services.SMS;
using TaskPlanner.Common.CommonDto;

namespace TaskPlanner.Application.Services.SMS.Commands
{
    public interface ISmsPeerToPeerSendService
    {
        Task<ResultDto<SmsPeerToPeerSendResultDto>> SendAsync(
            SmsPeerToPeerSendRequest request,
            CancellationToken cancellationToken = default);
    }

    public class SmsPeerToPeerSendService : ISmsPeerToPeerSendService
    {
        private static readonly JsonSerializerSettings SerializeSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new DefaultContractResolver()
        };

        private static readonly JsonSerializerSettings DeserializeSettings = new()
        {
            NullValueHandling = NullValueHandling.Ignore,
            ContractResolver = new DefaultContractResolver
            {
                NamingStrategy = new CamelCaseNamingStrategy
                {
                    ProcessDictionaryKeys = false,
                    OverrideSpecifiedNames = false
                }
            }
        };

        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SmsOptions _options;
        private readonly ILogger<SmsPeerToPeerSendService> _logger;

        public SmsPeerToPeerSendService(
            IHttpClientFactory httpClientFactory,
            SmsOptions options,
            ILogger<SmsPeerToPeerSendService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options;
            _logger = logger;
        }

        public async Task<ResultDto<SmsPeerToPeerSendResultDto>> SendAsync(
            SmsPeerToPeerSendRequest request,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(_options.ApiKey))
                {
                    return Fail("کلید API سرویس پیامک تنظیم نشده است.");
                }

                if (string.IsNullOrWhiteSpace(_options.PeerToPeerSendUrl))
                {
                    return Fail("آدرس ارسال پیامک peer-to-peer تنظیم نشده است.");
                }

                if (request.MobileNumbers == null || request.MobileNumbers.Length == 0)
                {
                    return Fail("شماره موبایل وارد نشده است.");
                }

                if (request.Messages == null || request.Messages.Length != request.MobileNumbers.Length)
                {
                    return Fail("تعداد پیام‌ها باید با تعداد شماره موبایل برابر باشد.");
                }

                var payload = new PeerToPeerSmsRequest
                {
                    SenderNumber = request.SenderNumber,
                    Message = request.Messages,
                    MobileNumber = request.MobileNumbers,
                    SendToBlocksNumber = request.SendToBlockedNumbers,
                    SendTimeSpan = ResolveSendTimeSpan(request)
                };

                var client = _httpClientFactory.CreateClient("LimoSms");
                client.DefaultRequestHeaders.Remove("ApiKey");
                client.DefaultRequestHeaders.Add("ApiKey", _options.ApiKey);

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload, SerializeSettings),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync(_options.PeerToPeerSendUrl, content, cancellationToken);
                var resultContent = await response.Content.ReadAsStringAsync(cancellationToken);

                if (!response.IsSuccessStatusCode)
                {
                    _logger.LogWarning(
                        "Peer-to-peer SMS HTTP {StatusCode}: {Body}",
                        (int)response.StatusCode,
                        resultContent);
                    return Fail($"پاسخ HTTP {(int)response.StatusCode} از سرویس پیامک", resultContent);
                }

                var apiResponse = JsonConvert.DeserializeObject<PeerToPeerSmsResponse>(resultContent, DeserializeSettings)
                    ?? JsonConvert.DeserializeObject<PeerToPeerSmsResponse>(resultContent);

                if (apiResponse == null)
                {
                    return Fail("پاسخ نامعتبر از سرویس پیامک دریافت شد.", resultContent);
                }

                var providerMessage = string.IsNullOrWhiteSpace(apiResponse.Message)
                    ? "ارسال پیامک ناموفق بود."
                    : apiResponse.Message;

                return new ResultDto<SmsPeerToPeerSendResultDto>
                {
                    isSuccess = apiResponse.Success,
                    message = providerMessage,
                    temp = resultContent,
                    data = new SmsPeerToPeerSendResultDto
                    {
                        Success = apiResponse.Success,
                        Message = providerMessage,
                        MessageIds = apiResponse.MessageId ?? Array.Empty<string>()
                    }
                };
            }
            catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
            {
                throw;
            }
            catch (TaskCanceledException)
            {
                return Fail("زمان اتصال به سرویس پیامک به پایان رسید.");
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Peer-to-peer SMS network error");
                return Fail("خطای شبکه در ارسال پیامک.", ex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Peer-to-peer SMS failed");
                return Fail("خطا در ارسال پیامک. لطفاً دوباره تلاش کنید.", ex.Message);
            }
        }

        /// <summary>
        /// طبق مستند LimoSMS: اگر SendTimeSpan خالی باشد، پیام در لحظه ارسال می‌شود.
        /// </summary>
        private static double? ResolveSendTimeSpan(SmsPeerToPeerSendRequest request)
        {
            if (request.SendImmediately)
            {
                return null;
            }

            if (request.SendTimeSpan is null or <= 0)
            {
                return null;
            }

            return request.SendTimeSpan;
        }

        private static ResultDto<SmsPeerToPeerSendResultDto> Fail(string message, string? details = null) =>
            new()
            {
                isSuccess = false,
                message = message,
                temp = details,
                data = new SmsPeerToPeerSendResultDto
                {
                    Success = false,
                    Message = message
                }
            };
    }

    internal class PeerToPeerSmsRequest
    {
        [JsonProperty("SenderNumber")]
        public string SenderNumber { get; set; } = string.Empty;

        [JsonProperty("Message")]
        public string[] Message { get; set; } = Array.Empty<string>();

        [JsonProperty("MobileNumber")]
        public string[] MobileNumber { get; set; } = Array.Empty<string>();

        [JsonProperty("SendToBlocksNumber")]
        public bool SendToBlocksNumber { get; set; }

        [JsonProperty("SendTimeSpan")]
        public double? SendTimeSpan { get; set; }
    }

    internal class PeerToPeerSmsResponse
    {
        [JsonProperty("Success")]
        public bool Success { get; set; }

        [JsonProperty("Message")]
        public string? Message { get; set; }

        [JsonProperty("MessageId")]
        public string[]? MessageId { get; set; }
    }

    public class SmsPeerToPeerSendRequest
    {
        public string SenderNumber { get; set; } = string.Empty;
        public string[] Messages { get; set; } = Array.Empty<string>();
        public string[] MobileNumbers { get; set; } = Array.Empty<string>();
        public bool SendToBlockedNumbers { get; set; } = true;

        /// <summary>ارسال فوری — SendTimeSpan به API ارسال نمی‌شود</summary>
        public bool SendImmediately { get; set; } = true;

        /// <summary>زمان‌بندی اختیاری (Unix timestamp) — فقط وقتی SendImmediately=false</summary>
        public double? SendTimeSpan { get; set; }
    }

    public class SmsPeerToPeerSendResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public string[] MessageIds { get; set; } = Array.Empty<string>();
    }
}
