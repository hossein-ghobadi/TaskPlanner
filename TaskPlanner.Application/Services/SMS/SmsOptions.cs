using Microsoft.Extensions.Configuration;

namespace TaskPlanner.Application.Services.SMS
{
    public class SmsOptions
    {
        public const string SectionName = "Sms";

        private const string DefaultSendUrl = "https://api.limosms.com/api/sendcode";
        private const string DefaultCheckUrl = "https://api.limosms.com/api/checkcode";
        private const string DefaultPeerToPeerSendUrl = "https://api.limosms.com/api/sendpeertopeersms";

        public string ApiBaseUrl { get; set; } = "https://api.limosms.com/api";

        public string SendUrl { get; set; } = DefaultSendUrl;

        public string CheckUrl { get; set; } = DefaultCheckUrl;

        /// <summary>آدرس API ارسال peer-to-peer لیمو (دعوت و پیامک متنی).</summary>
        public string PeerToPeerSendUrl { get; set; } = DefaultPeerToPeerSendUrl;

        public string? ApiKey { get; set; }

        /// <summary>شماره خط اختصاصی لیمو اس‌ام‌اس برای پیامک متنی (دعوت و مشابه).</summary>
        public string? SenderNumber { get; set; }

        /// <summary>آدرس عمومی سایت (بدون اسلش انتهایی) برای لینک داخل پیامک.</summary>
        public string? PublicAppUrl { get; set; }

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(ApiKey) &&
            !string.IsNullOrWhiteSpace(SendUrl) &&
            !string.IsNullOrWhiteSpace(CheckUrl);

        public bool IsPeerToPeerConfigured =>
            !string.IsNullOrWhiteSpace(ApiKey) &&
            !string.IsNullOrWhiteSpace(PeerToPeerSendUrl) &&
            !string.IsNullOrWhiteSpace(SenderNumber);

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

        public string ResolvePeerToPeerSendUrl(IConfiguration configuration)
        {
            var fromEnv = Environment.GetEnvironmentVariable("SMS_Bulk_Send_Url")
                ?? Environment.GetEnvironmentVariable("SMS_Message_Url");
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                return fromEnv.Trim();
            }

            var fromConfig = configuration[$"{SectionName}:PeerToPeerSendUrl"]
                ?? configuration[$"{SectionName}:MessageSendUrl"];
            if (!string.IsNullOrWhiteSpace(fromConfig))
            {
                return fromConfig.Trim();
            }

            if (!string.IsNullOrWhiteSpace(PeerToPeerSendUrl) && PeerToPeerSendUrl != DefaultPeerToPeerSendUrl)
            {
                return PeerToPeerSendUrl.Trim();
            }

            return $"{ApiBaseUrl.TrimEnd('/')}/sendpeertopeersms";
        }

        public string ResolveSenderNumber(IConfiguration configuration)
        {
            var fromEnv = Environment.GetEnvironmentVariable("SMS_Sender_Number")
                ?? Environment.GetEnvironmentVariable("SMS_SenderNumber");
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                return fromEnv.Trim();
            }

            return (configuration[$"{SectionName}:SenderNumber"] ?? SenderNumber)?.Trim() ?? string.Empty;
        }

        public string ResolvePublicAppUrl(IConfiguration configuration)
        {
            var fromEnv = Environment.GetEnvironmentVariable("PUBLIC_APP_URL")
                ?? Environment.GetEnvironmentVariable("APP_PUBLIC_URL");
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                return fromEnv.Trim().TrimEnd('/');
            }

            var fromConfig = configuration[$"{SectionName}:PublicAppUrl"]
                ?? configuration["PublicAppUrl"];
            if (!string.IsNullOrWhiteSpace(fromConfig))
            {
                return fromConfig.Trim().TrimEnd('/');
            }

            return PublicAppUrl?.Trim().TrimEnd('/') ?? string.Empty;
        }
    }
}
