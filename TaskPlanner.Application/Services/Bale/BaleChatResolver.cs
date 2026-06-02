using System.Text.RegularExpressions;

namespace TaskPlanner.Application.Services.Bale
{
    public static class BaleChatResolver
    {
        private static readonly Regex UsernameRegex = new(@"^@?[a-zA-Z][a-zA-Z0-9_]{4,}$", RegexOptions.Compiled);

        public static string? NormalizeUsername(string? value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return null;
            }

            var trimmed = value.Trim();
            if (trimmed.StartsWith('@'))
            {
                trimmed = trimmed[1..];
            }

            return string.IsNullOrWhiteSpace(trimmed) ? null : trimmed.ToLowerInvariant();
        }

        public static bool LooksLikeUsername(string input)
        {
            var trimmed = input.Trim();
            if (trimmed.StartsWith('@'))
            {
                return UsernameRegex.IsMatch(trimmed);
            }

            return !long.TryParse(trimmed, out _) && UsernameRegex.IsMatch("@" + trimmed);
        }

        public static string ToApiChatId(string input)
        {
            var trimmed = input.Trim();
            if (LooksLikeUsername(trimmed))
            {
                return trimmed.StartsWith('@') ? trimmed : "@" + trimmed;
            }

            return trimmed;
        }

        public static async Task<BaleClientResult<BaleChat>> ResolveChatAsync(
            IBaleBotClient client,
            string botToken,
            string chatIdentifier,
            CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(chatIdentifier))
            {
                return BaleClientResult<BaleChat>.Fail("شناسه گروه یا کانال بله را وارد کنید.");
            }

            var apiChatId = ToApiChatId(chatIdentifier);
            var chatResult = await client.GetChatAsync(botToken, apiChatId, cancellationToken);
            if (!chatResult.Success || chatResult.Data == null)
            {
                return BaleClientResult<BaleChat>.Fail(
                    chatResult.ErrorDescription ?? "گروه/کانال پیدا نشد. بازو را ادمین کانال کنید و دوباره تلاش کنید.",
                    chatResult.ErrorCode);
            }

            var chatType = chatResult.Data.Type?.ToLowerInvariant();
            if (chatType is not ("group" or "supergroup" or "channel"))
            {
                return BaleClientResult<BaleChat>.Fail("فقط گروه، سوپرگروه یا کانال بله پشتیبانی می‌شود.");
            }

            return chatResult;
        }
    }
}
