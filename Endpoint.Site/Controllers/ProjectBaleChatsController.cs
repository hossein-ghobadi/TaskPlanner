using Endpoint.Site.Models;
using TaskPlanner.Application.Services.Bale;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Persistence.Contexts;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class ProjectBaleChatsController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly IBaleBotClient _baleBotClient;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProjectBaleChatsController> _logger;

        public ProjectBaleChatsController(
            MVPTestDatabaseContext context,
            IBaleBotClient baleBotClient,
            IConfiguration configuration,
            ILogger<ProjectBaleChatsController> logger)
        {
            _context = context;
            _baleBotClient = baleBotClient;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        public async Task<IActionResult> GetLink(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            if (!await HasProjectAccess(projectId, userId))
            {
                return Forbid();
            }

            var row = await _context.ProjectBaleGroupLinks
                .AsNoTracking()
                .Where(l => l.ProjectId == projectId)
                .Select(l => new
                {
                    l.Id,
                    l.ProjectId,
                    DisplayName = l.ProjectChatGroup.Name,
                    l.BaleChatId,
                    l.BaleChatUsername,
                    l.BaleChatTitle,
                    l.IsEnabled,
                    l.BotToken
                })
                .FirstOrDefaultAsync();

            ProjectBaleGroupLinkVm? link = null;
            if (row != null)
            {
                link = new ProjectBaleGroupLinkVm
                {
                    Id = row.Id,
                    ProjectId = row.ProjectId,
                    DisplayName = row.DisplayName,
                    BaleChatId = row.BaleChatId,
                    BaleChatIdentifier = !string.IsNullOrWhiteSpace(row.BaleChatUsername)
                        ? "@" + row.BaleChatUsername
                        : row.BaleChatId.ToString(),
                    BaleChatTitle = row.BaleChatTitle,
                    IsEnabled = row.IsEnabled,
                    HasBotToken = !string.IsNullOrWhiteSpace(row.BotToken),
                    BotTokenHint = BaleTokenHelper.MaskHint(row.BotToken)
                };
            }

            return Json(new { link });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SaveLink([FromBody] ProjectBaleGroupLinkVm vm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest("اطلاعات اتصال گروه بله نامعتبر است.");
            }

            if (!await IsProjectCreator(vm.ProjectId, userId))
            {
                return Forbid();
            }

            var existingLink = await _context.ProjectBaleGroupLinks
                .Include(l => l.ProjectChatGroup)
                .FirstOrDefaultAsync(l => l.ProjectId == vm.ProjectId);

            var resolvedToken = BaleTokenHelper.Resolve(vm.BotToken, existingLink?.BotToken, _configuration);
            if (string.IsNullOrEmpty(resolvedToken))
            {
                return BadRequest("توکن بازوی بله را وارد کنید.");
            }

            var chatIdentifier = vm.BaleChatIdentifier?.Trim();
            if (string.IsNullOrWhiteSpace(chatIdentifier) && vm.BaleChatId != 0)
            {
                chatIdentifier = vm.BaleChatId.ToString();
            }

            if (string.IsNullOrWhiteSpace(chatIdentifier))
            {
                return BadRequest("شناسه عددی گروه یا @username کانال (مثل @shidatis_agency) را وارد کنید.");
            }

            var chatResult = await BaleChatResolver.ResolveChatAsync(_baleBotClient, resolvedToken, chatIdentifier);
            if (!chatResult.Success || chatResult.Data == null)
            {
                return BadRequest(chatResult.ErrorDescription ?? "گروه/کانال بله پیدا نشد.");
            }

            var resolvedChat = chatResult.Data;
            var baleChatId = resolvedChat.Id;
            var baleChatUsername = BaleChatResolver.NormalizeUsername(resolvedChat.Username);
            var baleChatTitle = string.IsNullOrWhiteSpace(vm.BaleChatTitle)
                ? resolvedChat.Title?.Trim()
                : vm.BaleChatTitle.Trim();

            var duplicateChat = await _context.ProjectBaleGroupLinks
                .AnyAsync(l => l.BaleChatId == baleChatId && l.ProjectId != vm.ProjectId);

            if (duplicateChat)
            {
                return BadRequest("این گروه بله قبلاً به پروژه دیگری متصل شده است.");
            }

            if (existingLink != null)
            {
                var chatChanged = existingLink.BaleChatId != baleChatId
                    || !string.Equals(existingLink.BaleChatUsername, baleChatUsername, StringComparison.OrdinalIgnoreCase);
                existingLink.BaleChatId = baleChatId;
                existingLink.BaleChatUsername = baleChatUsername;
                existingLink.BaleChatTitle = baleChatTitle;
                existingLink.IsEnabled = vm.IsEnabled;
                existingLink.BotToken = resolvedToken;
                if (chatChanged)
                {
                    existingLink.LastUpdateId = 0;
                }
                existingLink.ProjectChatGroup.Name = vm.DisplayName.Trim();
                existingLink.ProjectChatGroup.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { success = true, groupId = existingLink.ProjectChatGroupId, linkId = existingLink.Id });
            }

            var group = new ProjectChatGroup
            {
                ProjectId = vm.ProjectId,
                Name = vm.DisplayName.Trim(),
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ProjectChatGroups.Add(group);
            await _context.SaveChangesAsync();

            await EnsureAllProjectMembersInGroupAsync(vm.ProjectId, group.Id, userId);

            var link = new ProjectBaleGroupLink
            {
                ProjectId = vm.ProjectId,
                ProjectChatGroupId = group.Id,
                BaleChatId = baleChatId,
                BaleChatUsername = baleChatUsername,
                BaleChatTitle = baleChatTitle,
                BotToken = resolvedToken,
                IsEnabled = vm.IsEnabled,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow
            };
            _context.ProjectBaleGroupLinks.Add(link);
            await _context.SaveChangesAsync();

            return Json(new { success = true, groupId = group.Id, linkId = link.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLink(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            if (!await IsProjectCreator(projectId, userId))
            {
                return Forbid();
            }

            var link = await _context.ProjectBaleGroupLinks
                .Include(l => l.ProjectChatGroup)
                .FirstOrDefaultAsync(l => l.ProjectId == projectId);

            if (link == null)
            {
                return NotFound();
            }

            _context.ProjectBaleGroupLinks.Remove(link);
            link.ProjectChatGroup.IsArchived = true;
            link.ProjectChatGroup.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        [HttpGet]
        public async Task<IActionResult> DiscoverRecentGroups(string? botToken, int? projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            if (projectId.HasValue && !await HasProjectAccess(projectId.Value, userId))
            {
                return Forbid();
            }

            string? storedToken = null;
            if (projectId.HasValue)
            {
                storedToken = await _context.ProjectBaleGroupLinks
                    .Where(l => l.ProjectId == projectId.Value)
                    .Select(l => l.BotToken)
                    .FirstOrDefaultAsync();
            }

            var resolvedToken = BaleTokenHelper.Resolve(botToken, storedToken, _configuration);
            if (string.IsNullOrEmpty(resolvedToken))
            {
                return BadRequest("توکن بازوی بله را وارد کنید.");
            }

            var updatesResult = await _baleBotClient.GetUpdatesAsync(resolvedToken, 0, 0);
            if (!updatesResult.Success)
            {
                return BadRequest(updatesResult.ErrorDescription ?? "خطا در دریافت گروه‌ها از بله");
            }

            var updates = updatesResult.Data ?? Array.Empty<BaleUpdate>();
            var chats = updates
                .Select(u => BaleMessageParser.TryGetUpdateMessage(u, out _)?.Chat)
                .Where(c => c != null && c.Type is "group" or "supergroup" or "channel")
                .GroupBy(c => c!.Id)
                .Select(g => new ProjectBaleDiscoveredChatVm
                {
                    ChatId = g.Key,
                    Username = g.First()?.Username,
                    Title = g.First()?.Title,
                    Type = g.First()?.Type
                })
                .OrderBy(c => c.Title)
                .ToList();

            return Json(chats);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> TestBotToken([FromBody] ProjectBaleTestBotVm vm)
        {
            return GetBotStatusInternalAsync(vm.BotToken, vm.ProjectId);
        }

        [HttpGet]
        public Task<IActionResult> GetBotStatus(string? botToken, int? projectId)
        {
            return GetBotStatusInternalAsync(botToken, projectId);
        }

        private async Task<IActionResult> GetBotStatusInternalAsync(string? botToken, int? projectId)
        {
            try
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (string.IsNullOrWhiteSpace(userId))
                {
                    return Json(new { configured = false, error = "لطفاً دوباره وارد شوید." });
                }

                if (projectId.HasValue && !await IsProjectCreator(projectId.Value, userId))
                {
                    return Json(new { configured = false, error = "فقط سازنده پروژه می‌تواند تنظیمات بله را انجام دهد." });
                }

                var resolvedToken = BaleTokenHelper.Normalize(botToken);
                if (string.IsNullOrEmpty(resolvedToken) && projectId.HasValue)
                {
                    resolvedToken = BaleTokenHelper.Normalize(await _context.ProjectBaleGroupLinks
                        .Where(l => l.ProjectId == projectId.Value)
                        .Select(l => l.BotToken)
                        .FirstOrDefaultAsync());
                }

                if (string.IsNullOrEmpty(resolvedToken))
                {
                    resolvedToken = BaleTokenHelper.Normalize(new BaleBotOptions().ResolveToken(_configuration));
                }

                if (string.IsNullOrEmpty(resolvedToken))
                {
                    return Json(new { configured = false });
                }

                var meResult = await _baleBotClient.GetMeAsync(resolvedToken);
                if (!meResult.Success || meResult.Data == null)
                {
                    return Json(new
                    {
                        configured = false,
                        invalid = true,
                        error = meResult.ErrorDescription ?? "توکن نامعتبر است یا بازو در دسترس نیست."
                    });
                }

                return Json(new
                {
                    configured = true,
                    username = meResult.Data.Username,
                    displayName = meResult.Data.FirstName
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "GetBotStatus failed for project {ProjectId}", projectId);
                var message = ex is Microsoft.Data.SqlClient.SqlException
                    ? "ستون‌های دیتابیس بله اعمال نشده‌اند. دستور Update-Database را اجرا کنید."
                    : "خطای داخلی سرور هنگام بررسی توکن.";
                return Json(new { configured = false, error = message });
            }
        }

        private async Task EnsureAllProjectMembersInGroupAsync(int projectId, int groupId, string addedByUserId)
        {
            var projectMemberIds = await _context.ProjectMembers
                .Where(m => m.ProjectId == projectId && m.UserId != null)
                .Select(m => m.UserId!)
                .ToListAsync();

            var creatorId = await _context.Projects
                .Where(p => p.Id == projectId)
                .Select(p => p.CreatorUserId)
                .FirstOrDefaultAsync();

            var allIds = projectMemberIds
                .Append(creatorId ?? string.Empty)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToList();

            var existing = await _context.ProjectChatGroupMembers
                .Where(m => m.ProjectChatGroupId == groupId)
                .Select(m => m.UserId)
                .ToListAsync();

            var newMembers = allIds
                .Where(id => !existing.Contains(id))
                .Select(id => new ProjectChatGroupMember
                {
                    ProjectChatGroupId = groupId,
                    UserId = id,
                    AddedByUserId = addedByUserId,
                    AddedAt = DateTime.UtcNow
                })
                .ToList();

            if (newMembers.Count > 0)
            {
                _context.ProjectChatGroupMembers.AddRange(newMembers);
                await _context.SaveChangesAsync();
            }
        }

        private async Task<bool> HasProjectAccess(int projectId, string userId)
        {
            return await _context.Projects
                .AnyAsync(p => p.Id == projectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));
        }

        private async Task<bool> IsProjectCreator(int projectId, string userId)
        {
            return await _context.Projects
                .AnyAsync(p => p.Id == projectId && p.CreatorUserId == userId);
        }
    }
}
