using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.Text;
using TaskPlanner.Application.Services.SMS;
using TaskPlanner.Common.CommonDto;

namespace TaskPlanner.Application.Services.SMS.Commands
{
    public interface ISmsCheckService
    {
        Task<ResultDto<bool>> CheckAsync(RequestSmsCheckDto request, CancellationToken cancellationToken = default);
    }

    public class SmsCheckService : ISmsCheckService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly SmsOptions _options;
        private readonly ILogger<SmsCheckService> _logger;

        public SmsCheckService(
            IHttpClientFactory httpClientFactory,
            SmsOptions options,
            ILogger<SmsCheckService> logger)
        {
            _httpClientFactory = httpClientFactory;
            _options = options;
            _logger = logger;
        }

        public async Task<ResultDto<bool>> CheckAsync(RequestSmsCheckDto request, CancellationToken cancellationToken = default)
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
                    Code = request.Code
                };

                var content = new StringContent(
                    JsonConvert.SerializeObject(payload),
                    Encoding.UTF8,
                    "application/json");

                var response = await client.PostAsync(_options.CheckUrl, content, cancellationToken);
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
                _logger.LogError(ex, "SMS check failed for phone {Phone}", request.PhoneNumber);
                return new ResultDto<bool>
                {
                    isSuccess = false,
                    data = false,
                    message = "خطا در تأیید کد پیامک. لطفاً دوباره تلاش کنید."
                };
            }
        }
    }
}
