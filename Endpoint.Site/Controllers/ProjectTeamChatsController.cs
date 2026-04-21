using Endpoint.Site.Hubs;
using Endpoint.Site.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Domain.Entities.Users;
using TaskPlanner.Persistence.Contexts;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class ProjectTeamChatsController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IHubContext<ProjectChatHub> _hubContext;

        public ProjectTeamChatsController(
            MVPTestDatabaseContext context,
            UserManager<User> userManager,
            IHubContext<ProjectChatHub> hubContext)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
        }

        [HttpGet]
        public async Task<IActionResult> GetGroups(int projectId)
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

            var groups = await _context.ProjectChatGroups
                .Where(g => g.ProjectId == projectId && !g.IsArchived)
                .OrderByDescending(g => g.UpdatedAt)
                .Select(g => new
                {
                    g.Id,
                    g.ProjectId,
                    g.Name,
                    g.CreatedByUserId,
                    g.CreatedAt,
                    MembersCount = g.Members.Count,
                    IsMember = g.Members.Any(m => m.UserId == userId)
                })
                .ToListAsync();

            var creatorIds = groups.Select(g => g.CreatedByUserId).Distinct().ToList();
            var creatorLookup = await _userManager.Users
                .Where(u => creatorIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName! : (u.UserName ?? "کاربر"));

            var projectCreatorId = await _context.Projects
                .Where(p => p.Id == projectId)
                .Select(p => p.CreatorUserId)
                .FirstOrDefaultAsync();

            var result = groups
                .Where(g => g.IsMember)
                .Select(g => new ProjectChatGroupItemVm
                {
                    Id = g.Id,
                    ProjectId = g.ProjectId,
                    Name = g.Name,
                    CreatedByUserId = g.CreatedByUserId,
                    CreatedByName = creatorLookup.TryGetValue(g.CreatedByUserId, out var creatorName) ? creatorName : "کاربر",
                    MembersCount = g.MembersCount,
                    CreatedAt = g.CreatedAt,
                    CanManageMembers = g.CreatedByUserId == userId || projectCreatorId == userId
                })
                .ToList();

            return Json(result);
        }

        [HttpGet]
        public async Task<IActionResult> GetMessages(int groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var group = await _context.ProjectChatGroups
                .Include(g => g.Project)
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsArchived);

            if (group == null)
            {
                return NotFound();
            }

            var hasAccess = await _context.ProjectChatGroupMembers
                .AnyAsync(m => m.ProjectChatGroupId == groupId && m.UserId == userId);

            if (!hasAccess)
            {
                return Forbid();
            }

            var canManageAllMessages = group.CreatedByUserId == userId || group.Project.CreatorUserId == userId;

            var messages = await _context.ProjectChatMessages
                .Where(m => m.ProjectChatGroupId == groupId && !m.IsDeleted)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ProjectChatMessageVm
                {
                    Id = m.Id,
                    GroupId = m.ProjectChatGroupId,
                    UserId = m.UserId,
                    UserName = m.UserName,
                    Message = m.Message,
                    CreatedAt = m.CreatedAt,
                    IsCurrentUser = m.UserId == userId,
                    CanDelete = m.UserId == userId || canManageAllMessages
                })
                .ToListAsync();

            return Json(messages);
        }

        [HttpGet]
        public async Task<IActionResult> GetGroupMembers(int groupId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var group = await _context.ProjectChatGroups
                .Include(g => g.Project)
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsArchived);

            if (group == null)
            {
                return NotFound();
            }

            var isGroupMember = await _context.ProjectChatGroupMembers
                .AnyAsync(m => m.ProjectChatGroupId == groupId && m.UserId == userId);

            if (!isGroupMember)
            {
                return Forbid();
            }

            var memberIds = await _context.ProjectChatGroupMembers
                .Where(m => m.ProjectChatGroupId == groupId)
                .Select(m => m.UserId)
                .ToListAsync();

            var members = await _userManager.Users
                .Where(u => memberIds.Contains(u.Id))
                .Select(u => new ProjectChatGroupMemberVm
                {
                    UserId = u.Id,
                    DisplayName = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName! : (u.UserName ?? "کاربر")
                })
                .ToListAsync();

            return Json(members.OrderBy(m => m.DisplayName));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateGroup([FromBody] ProjectChatGroupCreateVm vm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            if (!ModelState.IsValid)
            {
                return BadRequest("اطلاعات گروه نامعتبر است.");
            }

            if (!await HasProjectAccess(vm.ProjectId, userId))
            {
                return Forbid();
            }

            var group = new ProjectChatGroup
            {
                ProjectId = vm.ProjectId,
                Name = vm.Name.Trim(),
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.ProjectChatGroups.Add(group);
            await _context.SaveChangesAsync();

            var requestedMembers = vm.MemberUserIds?
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct()
                .ToList() ?? new List<string>();

            if (!requestedMembers.Contains(userId))
            {
                requestedMembers.Add(userId);
            }

            var projectMemberIds = await _context.ProjectMembers
                .Where(m => m.ProjectId == vm.ProjectId && m.UserId != null)
                .Select(m => m.UserId)
                .ToListAsync();

            var projectCreatorId = await _context.Projects
                .Where(p => p.Id == vm.ProjectId)
                .Select(p => p.CreatorUserId)
                .FirstOrDefaultAsync();

            var allowedIds = projectMemberIds
                .Append(projectCreatorId ?? string.Empty)
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Distinct()
                .ToHashSet();

            var members = requestedMembers
                .Where(allowedIds.Contains)
                .Select(id => new ProjectChatGroupMember
                {
                    ProjectChatGroupId = group.Id,
                    UserId = id,
                    AddedByUserId = userId,
                    AddedAt = DateTime.UtcNow
                })
                .ToList();

            _context.ProjectChatGroupMembers.AddRange(members);
            await _context.SaveChangesAsync();

            return Json(new { success = true, groupId = group.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SendMessage([FromBody] ProjectChatSendMessageVm vm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var messageText = vm.Message?.Trim();
            if (string.IsNullOrWhiteSpace(messageText))
            {
                return BadRequest("پیام نمی‌تواند خالی باشد.");
            }

            var group = await _context.ProjectChatGroups
                .FirstOrDefaultAsync(g => g.Id == vm.GroupId && !g.IsArchived);

            if (group == null)
            {
                return NotFound();
            }

            var isMember = await _context.ProjectChatGroupMembers
                .AnyAsync(m => m.ProjectChatGroupId == vm.GroupId && m.UserId == userId);

            if (!isMember)
            {
                return Forbid();
            }

            var currentUser = await _userManager.GetUserAsync(User);
            var userName = !string.IsNullOrWhiteSpace(currentUser?.FullName)
                ? currentUser.FullName!
                : currentUser?.UserName ?? "کاربر";

            var message = new ProjectChatMessage
            {
                ProjectChatGroupId = vm.GroupId,
                UserId = userId,
                UserName = userName,
                Message = messageText,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProjectChatMessages.Add(message);
            group.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var result = new ProjectChatMessageVm
            {
                Id = message.Id,
                GroupId = message.ProjectChatGroupId,
                UserId = message.UserId,
                UserName = message.UserName,
                Message = message.Message,
                CreatedAt = message.CreatedAt,
                IsCurrentUser = false,
                CanDelete = false
            };

            await _hubContext.Clients
                .Group(ProjectChatHub.GetSignalRGroupName(vm.GroupId))
                .SendAsync("projectGroupMessageCreated", result);

            return Json(result);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteMessage(int messageId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var message = await _context.ProjectChatMessages
                .Include(m => m.ProjectChatGroup)
                    .ThenInclude(g => g.Project)
                .FirstOrDefaultAsync(m => m.Id == messageId && !m.IsDeleted);

            if (message == null)
            {
                return NotFound();
            }

            var isAllowed = message.UserId == userId
                || message.ProjectChatGroup.CreatedByUserId == userId
                || message.ProjectChatGroup.Project.CreatorUserId == userId;

            if (!isAllowed)
            {
                return Forbid();
            }

            message.IsDeleted = true;
            message.ProjectChatGroup.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _hubContext.Clients
                .Group(ProjectChatHub.GetSignalRGroupName(message.ProjectChatGroupId))
                .SendAsync("projectGroupMessageDeleted", new
                {
                    groupId = message.ProjectChatGroupId,
                    messageId = message.Id
                });

            return Json(new { success = true });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMembers([FromBody] ProjectChatAddMembersVm vm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var group = await _context.ProjectChatGroups
                .Include(g => g.Project)
                .FirstOrDefaultAsync(g => g.Id == vm.GroupId && !g.IsArchived);

            if (group == null)
            {
                return NotFound();
            }

            if (!await CanManageGroupMembers(group, userId))
            {
                return Forbid();
            }

            var requestedIds = vm.UserIds
                .Where(id => !string.IsNullOrWhiteSpace(id))
                .Select(id => id.Trim())
                .Distinct()
                .ToList();

            if (!requestedIds.Any())
            {
                return BadRequest("حداقل یک عضو انتخاب کنید.");
            }

            var projectMemberIds = await _context.ProjectMembers
                .Where(m => m.ProjectId == group.ProjectId)
                .Select(m => m.UserId)
                .ToListAsync();

            var allowedIds = projectMemberIds
                .Append(group.Project.CreatorUserId)
                .Distinct()
                .ToHashSet();

            var existingIds = await _context.ProjectChatGroupMembers
                .Where(m => m.ProjectChatGroupId == vm.GroupId)
                .Select(m => m.UserId)
                .ToListAsync();

            var newMembers = requestedIds
                .Where(id => allowedIds.Contains(id) && !existingIds.Contains(id))
                .Select(id => new ProjectChatGroupMember
                {
                    ProjectChatGroupId = vm.GroupId,
                    UserId = id,
                    AddedByUserId = userId,
                    AddedAt = DateTime.UtcNow
                })
                .ToList();

            if (newMembers.Any())
            {
                _context.ProjectChatGroupMembers.AddRange(newMembers);
                group.UpdatedAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
            }

            return Json(new { success = true, addedCount = newMembers.Count });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int groupId, string memberUserId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            if (string.IsNullOrWhiteSpace(memberUserId))
            {
                return BadRequest("عضو نامعتبر است.");
            }

            var group = await _context.ProjectChatGroups
                .Include(g => g.Project)
                .FirstOrDefaultAsync(g => g.Id == groupId && !g.IsArchived);

            if (group == null)
            {
                return NotFound();
            }

            if (!await CanManageGroupMembers(group, userId))
            {
                return Forbid();
            }

            if (memberUserId == group.CreatedByUserId)
            {
                return BadRequest("نمی‌توانید سازنده گروه را حذف کنید.");
            }

            var membership = await _context.ProjectChatGroupMembers
                .FirstOrDefaultAsync(m => m.ProjectChatGroupId == groupId && m.UserId == memberUserId);

            if (membership == null)
            {
                return NotFound();
            }

            _context.ProjectChatGroupMembers.Remove(membership);
            group.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        private async Task<bool> HasProjectAccess(int projectId, string userId)
        {
            return await _context.Projects
                .AnyAsync(p => p.Id == projectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));
        }

        private async Task<bool> CanManageGroupMembers(ProjectChatGroup group, string userId)
        {
            if (group.CreatedByUserId == userId || group.Project.CreatorUserId == userId)
            {
                return true;
            }

            // Allow current group members to manage members if they are project creator.
            var isProjectCreator = await _context.Projects
                .AnyAsync(p => p.Id == group.ProjectId && p.CreatorUserId == userId);
            return isProjectCreator;
        }
    }
}
