using Microsoft.Extensions.Configuration;

namespace TaskPlanner.Application.Services.Bale
{
    public class BaleBotOptions
    {
        public const string SectionName = "Bale";

        /// <summary>توکن بازو — ترجیحاً از متغیر محیطی BALE_BOT_TOKEN خوانده شود.</summary>
        public string? BotToken { get; set; }

        public string ApiBaseUrl { get; set; } = "https://tapi.bale.ai";

        /// <summary>فاصله بین هر دور polling (ثانیه).</summary>
        public int PollIntervalSeconds { get; set; } = 3;

        /// <summary>timeout در getUpdates (ثانیه).</summary>
        public int LongPollTimeoutSeconds { get; set; } = 25;

        public bool IsConfigured =>
            !string.IsNullOrWhiteSpace(BotToken);

        public string ResolveToken(IConfiguration configuration)
        {
            var fromEnv = Environment.GetEnvironmentVariable("BALE_BOT_TOKEN");
            if (!string.IsNullOrWhiteSpace(fromEnv))
            {
                return fromEnv.Trim();
            }

            var fromConfig = configuration[$"{SectionName}:BotToken"];
            if (!string.IsNullOrWhiteSpace(fromConfig))
            {
                return fromConfig.Trim();
            }

            return BotToken?.Trim() ?? string.Empty;
        }
    }
}
