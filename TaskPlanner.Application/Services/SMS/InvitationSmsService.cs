using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using TaskPlanner.Application.Services.SMS.Commands;

namespace TaskPlanner.Application.Services.SMS
{
    public interface IInvitationSmsService
    {
        Task TrySendProjectInvitationAsync(
            string phone,
            string projectName,
            string? inviterName,
            CancellationToken cancellationToken = default);
    }

    public class InvitationSmsService : IInvitationSmsService
    {
        public const string InvitePath = "/invite";

        private readonly ISmsPeerToPeerSendService _smsPeerToPeerSendService;
        private readonly SmsOptions _options;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<InvitationSmsService> _logger;

        public InvitationSmsService(
            ISmsPeerToPeerSendService smsPeerToPeerSendService,
            SmsOptions options,
            IHttpContextAccessor httpContextAccessor,
            ILogger<InvitationSmsService> logger)
        {
            _smsPeerToPeerSendService = smsPeerToPeerSendService;
            _options = options;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        public async Task TrySendProjectInvitationAsync(
            string phone,
            string projectName,
            string? inviterName,
            CancellationToken cancellationToken = default)
        {
            try
            {
                if (!_options.IsPeerToPeerConfigured)
                {
                    _logger.LogWarning(
                        "Project invitation SMS skipped: peer-to-peer SMS is not configured (ApiKey/SenderNumber)");
                    return;
                }

                var normalizedPhone = PhoneNumberNormalizer.Normalize(phone);
                if (string.IsNullOrWhiteSpace(normalizedPhone))
                {
                    _logger.LogWarning("Project invitation SMS skipped: empty phone");
                    return;
                }

                var inviteUrl = BuildInviteUrl();
                if (string.IsNullOrWhiteSpace(inviteUrl))
                {
                    _logger.LogWarning(
                        "Project invitation SMS skipped for {Phone}: public app URL is not configured",
                        normalizedPhone);
                    return;
                }

                var inviter = string.IsNullOrWhiteSpace(inviterName) ? "یک کاربر" : Truncate(inviterName.Trim(), 40);
                var project = Truncate(projectName?.Trim() ?? "پروژه", 50);
                var message =
                    $"{inviter} شما را به پروژه «{project}» دعوت کرد.\n" +
                    "برای قبول یا رد دعوت وارد شوید:\n" +
                    inviteUrl;

                var result = await _smsPeerToPeerSendService.SendAsync(new SmsPeerToPeerSendRequest
                {
                    SenderNumber = _options.SenderNumber ?? string.Empty,
                    Messages = new[] { message },
                    MobileNumbers = new[] { normalizedPhone },
                    SendToBlockedNumbers = true,
                    SendImmediately = true
                }, cancellationToken);

                var ok = result.isSuccess && (result.data?.Success ?? false);
                if (!ok)
                {
                    _logger.LogWarning(
                        "Project invitation SMS failed for {Phone}: {Message}",
                        normalizedPhone,
                        result.message);
                    return;
                }

                _logger.LogInformation("Project invitation SMS sent to {Phone}", normalizedPhone);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Project invitation SMS failed for phone {Phone}", phone);
            }
        }

        private string BuildInviteUrl()
        {
            var baseUrl = _options.PublicAppUrl;
            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                baseUrl = GetRequestBaseUrl();
            }

            if (string.IsNullOrWhiteSpace(baseUrl))
            {
                return string.Empty;
            }

            return $"{baseUrl.TrimEnd('/')}{InvitePath}";
        }

        private string GetRequestBaseUrl()
        {
            var request = _httpContextAccessor.HttpContext?.Request;
            if (request == null)
            {
                return string.Empty;
            }

            var scheme = FirstForwardedValue(request.Headers["X-Forwarded-Proto"]) ?? request.Scheme;
            var host = FirstForwardedValue(request.Headers["X-Forwarded-Host"]) ?? request.Host.Value;
            if (string.IsNullOrWhiteSpace(host))
            {
                return string.Empty;
            }

            return $"{scheme}://{host.Trim().TrimEnd('/')}";
        }

        private static string? FirstForwardedValue(string? header)
        {
            if (string.IsNullOrWhiteSpace(header))
            {
                return null;
            }

            var first = header.Split(',')[0].Trim();
            return string.IsNullOrWhiteSpace(first) ? null : first;
        }

        private static string Truncate(string value, int maxLength)
        {
            if (value.Length <= maxLength)
            {
                return value;
            }

            return value[..Math.Max(0, maxLength - 1)] + "…";
        }
    }
}
