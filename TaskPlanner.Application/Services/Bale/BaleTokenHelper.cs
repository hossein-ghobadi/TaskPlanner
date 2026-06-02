using Microsoft.Extensions.Configuration;

namespace TaskPlanner.Application.Services.Bale
{
    public static class BaleTokenHelper
    {
        public static string? Normalize(string? token)
        {
            if (string.IsNullOrWhiteSpace(token))
            {
                return null;
            }

            return token.Trim();
        }

        public static string? Resolve(string? fromRequest, string? fromDatabase, IConfiguration configuration)
        {
            var normalized = Normalize(fromRequest);
            if (!string.IsNullOrEmpty(normalized))
            {
                return normalized;
            }

            normalized = Normalize(fromDatabase);
            if (!string.IsNullOrEmpty(normalized))
            {
                return normalized;
            }

            var options = new BaleBotOptions();
            return Normalize(options.ResolveToken(configuration));
        }

        public static string? MaskHint(string? token)
        {
            var normalized = Normalize(token);
            if (string.IsNullOrEmpty(normalized) || normalized.Length < 8)
            {
                return null;
            }

            return "…" + normalized[^6..];
        }
    }
}
