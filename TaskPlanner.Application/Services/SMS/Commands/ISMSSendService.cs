using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using TaskPlanner.Application.Services.SMS;
using TaskPlanner.Common.CommonDto;

namespace TaskPlanner.Application.Services.SMS.Commands
{
    public interface ISmsSendService
    {
        Task<ResultDto<bool>> SendAsync(RequestSmsSendDto request, CancellationToken cancellationToken = default);
    }

    public class SmsSendService : ISmsSendService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SmsOptions _options;
        private readonly ILogger<SmsSendService> _logger;

        public SmsSendService(
            IHttpClientFactory httpClientFactory,
            SmsOptions options,
            ILogger<SmsSendService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options;
            _logger = logger;
        }

        public async Task<ResultDto<bool>> SendAsync(RequestSmsSendDto request, CancellationToken cancellationToken = default)
        {
            if (!_options.IsConfigured)
            {
                return new ResultDto<bool>
                {
                    isSuccess = false,
                    data = false,
                    message = "کلید API سرویس پیامک تنظیم نشده است."
                };
            }

            try
            {
                var client = _httpClientFactory.CreateClient("LimoSms");
                client.DefaultRequestHeaders.Remove("ApiKey");
                client.DefaultRequestHeaders.Add("ApiKey", _options.ApiKey);

                var payload = new
                {
                    Mobile = request.PhoneNumber,
                    Footer = string.Empty
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync(_options.SendUrl, content, cancellationToken);
                var resultContent = await response.Content.ReadAsStringAsync(cancellationToken);
                var apiResponse = JsonConvert.DeserializeObject<SmsApiResponse>(resultContent);

                if (apiResponse == null)
                {
                    return new ResultDto<bool>
                    {
                        isSuccess = false,
                        data = false,
                        message = "پاسخ نامعتبر از سرویس پیامک دریافت شد."
                    };
                }

                return new ResultDto<bool>
                {
                    isSuccess = apiResponse.Success,
                    data = apiResponse.Success,
                    message = apiResponse.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SMS send failed for phone {Phone}", request.PhoneNumber);
                return new ResultDto<bool>
                {
                    isSuccess = false,
                    data = false,
                    message = "خطا در ارسال پیامک. لطفاً دوباره تلاش کنید."
                };
            }
        }
    }
}
