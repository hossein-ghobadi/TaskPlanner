using Microsoft.Extensions.Configuration;

namespace TaskPlanner.Application.Services.SMS
{
    public class SmsOptions
    {
        public const string SectionName = "Sms";

        private const string DefaultSendUrl = "https://api.limosms.com/api/sendcode";
        private const string DefaultCheckUrl = "https://api.limosms.com/api/checkcode";

        public string ApiBaseUrl { get; set; } = "https://api.limosms.com/api";

        public string SendUrl { get; set; } = DefaultSendUrl;

        public string CheckUrl { get; set; } = DefaultCheckUrl;

        public string? ApiKey { get; set; }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(ApiKey) &&
            !string.IsNullOrWhiteSpace(SendUrl) &&
            !string.IsNullOrWhiteSpace(CheckUrl);

        public string ResolveApiKey(IConfiguration configuration)
        {
            var fromEnv = Environment.GetEnvironmentVariable("SMS_ApiKey")
                ?? Environment.GetEnvironmentVariable("LIMOSMS_API_KEY");
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                return fromEnv.Trim();
            }

            var fromConfig = configuration[$"{SectionName}:ApiKey"];
            if (!string.IsNullOrWhiteSpace(fromConfig))
            {
                return fromConfig.Trim();
            }

            return ApiKey?.Trim() ?? string.Empty;
        }

        public string ResolveSendUrl(IConfiguration configuration)
        {
            var fromEnv = Environment.GetEnvironmentVariable("SMS_Send_Url");
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                return fromEnv.Trim();
            }

            var fromConfig = configuration[$"{SectionName}:SendUrl"];
            if (!string.IsNullOrWhiteSpace(fromConfig))
            {
                return fromConfig.Trim();
            }

            if (!string.IsNullOrWhiteSpace(SendUrl) && SendUrl != DefaultSendUrl)
            {
                return SendUrl.Trim();
            }

            return $"{ApiBaseUrl.TrimEnd('/')}/sendcode";
        }

        public string ResolveCheckUrl(IConfiguration configuration)
        {
            var fromEnv = Environment.GetEnvironmentVariable("SMS_CHECK_Url");
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                return fromEnv.Trim();
            }

            var fromConfig = configuration[$"{SectionName}:CheckUrl"];
            if (!string.IsNullOrWhiteSpace(fromConfig))
            {
                return fromConfig.Trim();
            }

            if (!string.IsNullOrWhiteSpace(CheckUrl) && CheckUrl != DefaultCheckUrl)
            {
                return CheckUrl.Trim();
            }

            return $"{ApiBaseUrl.TrimEnd('/')}/checkcode";
        }
    }
}
