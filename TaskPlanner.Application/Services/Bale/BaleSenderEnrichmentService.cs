using System.Text.RegularExpressions;
using Microsoft.Extensions.Logging;

namespace TaskPlanner.Application.Services.Bale
{
    public interface IBaleSenderEnrichmentService
    {
        Task<BaleSenderInfo> ResolveAsync(
            string botToken,
            BaleMessage message,
            CancellationToken cancellationToken = default);
    }

    public sealed class BaleSenderEnrichmentService : IBaleSenderEnrichmentService
    {
        private static readonly Regex PhoneInLabelRegex = new(
            @"(?:موبایل|تلفن|شماره|mobile|phone)\s*[:：]?\s*([0-9۰-۹]{10,14})",
            RegexOptions.IgnoreCase | RegexOptions.Compiled | RegexOptions.CultureInvariant);

        private static readonly Regex IranianMobileRegex = new(
            @"(?<!\d)(09[0-9۰-۹]{9})(?!\d)",
            RegexOptions.Compiled);

        private readonly IBaleBotClient _baleBotClient;
        private readonly ILogger<BaleSenderEnrichmentService> _logger;

        public BaleSenderEnrichmentService(
            IBaleBotClient baleBotClient,
            ILogger<BaleSenderEnrichmentService> logger)
        {
            _baleBotClient = baleBotClient;
            _logger = logger;
        }

        public async Task<BaleSenderInfo> ResolveAsync(
            string botToken,
            BaleMessage message,
            CancellationToken cancellationToken = default)
        {
            if (message.Contact != null)
            {
                var contactInfo = BuildFromContact(message.Contact);
                contactInfo.HasKnownAuthor = true;
                return contactInfo;
            }

            var channelTitle = message.SenderChat?.Title?.Trim()
                ?? message.Chat?.Title?.Trim();
            var isChannelContext = IsChannelContext(message);

            if (IsRealHumanSender(message.From, message.SenderChat, message.Chat))
            {
                var fromUser = await EnrichFromUserAsync(
                    botToken,
                    message.Chat?.Id,
                    message.From!,
                    cancellationToken);
                fromUser.Phone ??= TryExtractPhoneFromText(message.Text ?? message.Caption);
                fromUser.ChannelTitle = channelTitle;
                fromUser.IsChannelSender = false;
                fromUser.HasKnownAuthor = true;
                fromUser.DisplayName = BuildDisplayName(fromUser);
                return fromUser;
            }

            var authorSignature = message.AuthorSignature?.Trim();
            if (!string.IsNullOrWhiteSpace(authorSignature))
            {
                return new BaleSenderInfo
                {
                    FirstName = authorSignature,
                    ChannelTitle = channelTitle,
                    IsChannelSender = isChannelContext,
                    HasKnownAuthor = true,
                    DisplayName = authorSignature,
                    Phone = TryExtractPhoneFromText(message.Text ?? message.Caption)
                };
            }

            if (message.From != null && !message.From.IsBot && !string.IsNullOrWhiteSpace(message.From.FirstName))
            {
                var fallbackFrom = await EnrichFromUserAsync(
                    botToken,
                    message.Chat?.Id,
                    message.From,
                    cancellationToken);
                fallbackFrom.ChannelTitle = channelTitle;
                fallbackFrom.HasKnownAuthor = true;
                fallbackFrom.DisplayName = BuildDisplayName(fallbackFrom);
                return fallbackFrom;
            }

            _logger.LogDebug(
                "Channel post without author: chat={ChatId} senderChat={SenderChatId} from={FromId} signature={Signature}",
                message.Chat?.Id,
                message.SenderChat?.Id,
                message.From?.Id,
                authorSignature);

            var unknownAuthor = new BaleSenderInfo
            {
                ChannelTitle = channelTitle,
                IsChannelSender = isChannelContext,
                HasKnownAuthor = false,
                Phone = TryExtractPhoneFromText(message.Text ?? message.Caption)
            };
            unknownAuthor.DisplayName = isChannelContext ? "ادمین کانال" : "کاربر بله";
            return unknownAuthor;
        }

        private static bool IsChannelContext(BaleMessage message)
        {
            return string.Equals(message.Chat?.Type, "channel", StringComparison.OrdinalIgnoreCase)
                || string.Equals(message.SenderChat?.Type, "channel", StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// در پست کانال، from گاهی مقدار ساختگی دارد؛ فقط کاربر واقعی را قبول می‌کنیم.
        /// </summary>
        private static bool IsRealHumanSender(BaleUser? from, BaleChat? senderChat, BaleChat? chat)
        {
            if (from == null || from.IsBot || from.Id <= 0)
            {
                return false;
            }

            if (senderChat != null && from.Id == senderChat.Id)
            {
                return false;
            }

            if (chat != null && from.Id == chat.Id)
            {
                return false;
            }

            return !string.IsNullOrWhiteSpace(from.FirstName)
                || !string.IsNullOrWhiteSpace(from.LastName)
                || !string.IsNullOrWhiteSpace(from.Username);
        }

        private async Task<BaleSenderInfo> EnrichFromUserAsync(
            string botToken,
            long? chatId,
            BaleUser from,
            CancellationToken cancellationToken)
        {
            var info = new BaleSenderInfo
            {
                BaleUserId = from.Id,
                FirstName = from.FirstName?.Trim(),
                LastName = from.LastName?.Trim(),
                Username = BaleChatResolver.NormalizeUsername(from.Username)
            };

            if (!chatId.HasValue || from.IsBot)
            {
                return info;
            }

            try
            {
                var memberResult = await _baleBotClient.GetChatMemberAsync(
                    botToken,
                    chatId.Value,
                    from.Id,
                    cancellationToken);

                if (memberResult.Success && memberResult.Data?.User != null)
                {
                    var user = memberResult.Data.User;
                    info.FirstName = user.FirstName?.Trim() ?? info.FirstName;
                    info.LastName = user.LastName?.Trim() ?? info.LastName;
                    info.Username = BaleChatResolver.NormalizeUsername(user.Username) ?? info.Username;
                }
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "getChatMember failed for user {UserId} in chat {ChatId}", from.Id, chatId);
            }

            return info;
        }

        private static BaleSenderInfo BuildFromContact(BaleContact contact)
        {
            var info = new BaleSenderInfo
            {
                BaleUserId = contact.UserId,
                FirstName = contact.FirstName?.Trim(),
                LastName = contact.LastName?.Trim(),
                Phone = NormalizePhone(contact.PhoneNumber)
            };
            info.DisplayName = BuildDisplayName(info);
            return info;
        }

        internal static string BuildDisplayName(BaleSenderInfo info)
        {
            var parts = new List<string>();
            if (!string.IsNullOrWhiteSpace(info.FirstName))
            {
                parts.Add(info.FirstName.Trim());
            }
            if (!string.IsNullOrWhiteSpace(info.LastName))
            {
                parts.Add(info.LastName.Trim());
            }

            var fullName = string.Join(" ", parts);
            if (!string.IsNullOrWhiteSpace(fullName))
            {
                if (!string.IsNullOrWhiteSpace(info.Username))
                {
                    return $"{fullName} (@{info.Username})";
                }

                return fullName;
            }

            if (!string.IsNullOrWhiteSpace(info.Username))
            {
                return $"@{info.Username}";
            }

            if (info.IsChannelSender && !info.HasKnownAuthor)
            {
                return "ادمین کانال";
            }

            return "کاربر بله";
        }

        internal static string? TryExtractPhoneFromText(string? text)
        {
            if (string.IsNullOrWhiteSpace(text))
            {
                return null;
            }

            var normalized = NormalizePersianDigits(text);
            var labelMatch = PhoneInLabelRegex.Match(normalized);
            if (labelMatch.Success)
            {
                return NormalizePhone(labelMatch.Groups[1].Value);
            }

            var mobileMatch = IranianMobileRegex.Match(normalized);
            if (mobileMatch.Success)
            {
                return NormalizePhone(mobileMatch.Groups[1].Value);
            }

            return null;
        }

        private static string NormalizePersianDigits(string input)
        {
            var chars = input.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (chars[i] is >= '۰' and <= '۹')
                {
                    chars[i] = (char)('0' + (chars[i] - '۰'));
                }
            }

            return new string(chars);
        }

        private static string? NormalizePhone(string? raw)
        {
            if (string.IsNullOrWhiteSpace(raw))
            {
                return null;
            }

            var digits = new string(raw.Where(char.IsDigit).ToArray());
            if (digits.Length < 10)
            {
                return null;
            }

            if (digits.StartsWith("98") && digits.Length >= 12)
            {
                digits = "0" + digits[2..];
            }

            return digits;
        }
    }
}
