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
using TaskPlanner.Application.Services.FileUpload;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class ProjectTeamChatsController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IHubContext<ProjectChatHub> _hubContext;
        private readonly IFileUploadService _fileUploadService;

        public ProjectTeamChatsController(
            MVPTestDatabaseContext context,
            UserManager<User> userManager,
            IHubContext<ProjectChatHub> hubContext,
            IFileUploadService fileUploadService)
        {
            _context = context;
            _userManager = userManager;
            _hubContext = hubContext;
            _fileUploadService = fileUploadService;
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

            var hasAccess = await _context.ProjectChatGroupMembers
                .AnyAsync(m => m.ProjectChatGroupId == groupId && m.UserId == userId);

            if (!hasAccess)
            {
                return Forbid();
            }

            var messages = await _context.ProjectChatMessages
                .Include(m => m.Attachments)
                .Where(m => m.ProjectChatGroupId == groupId && !m.IsDeleted)
                .OrderBy(m => m.CreatedAt)
                .Select(m => new ProjectChatMessageVm
                {
                    Id = m.Id,
                    GroupId = m.ProjectChatGroupId,
                    UserId = m.UserId,
                    UserName = m.UserName,
                    Message = m.Message,
                    ReplyToMessageId = m.ReplyToMessageId,
                    ReplyPreviewUserName = m.ReplyToMessage == null
                        ? null
                        : m.ReplyToMessage.UserName,
                    ReplyPreviewMessage = m.ReplyToMessage == null
                        ? null
                        : (m.ReplyToMessage.IsDeleted
                            ? "پیام حذف شده"
                            : (!string.IsNullOrWhiteSpace(m.ReplyToMessage.Message)
                                ? m.ReplyToMessage.Message
                                : (m.ReplyToMessage.Attachments.Any()
                                    ? "فایل"
                                    : "(بدون متن)"))),
                    CreatedAt = m.CreatedAt,
                    IsCurrentUser = m.UserId == userId,
                    Attachments = m.Attachments
                        .Select(a => new ProjectChatMessageAttachmentVm
                        {
                            Id = a.Id,
                            FileName = a.FileName,
                            FilePath = a.FilePath,
                            FileType = a.FileType,
                            FileSize = a.FileSize,
                            MimeType = a.MimeType,
                            UploadedAt = a.UploadedAt
                        }).ToList()
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
        public async Task<IActionResult> SendMessage([FromForm] ProjectChatSendMessageVm vm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var messageText = vm.Message?.Trim();
            if (string.IsNullOrWhiteSpace(messageText) && (vm.Attachments == null || !vm.Attachments.Any()))
            {
                return BadRequest("حداقل یک پیام یا فایل باید ارسال شود.");
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

            ProjectChatMessage? repliedMessage = null;
            if (vm.ReplyToMessageId.HasValue)
            {
                repliedMessage = await _context.ProjectChatMessages
                    .FirstOrDefaultAsync(m => m.Id == vm.ReplyToMessageId.Value
                        && m.ProjectChatGroupId == vm.GroupId);
                if (repliedMessage == null)
                {
                    return BadRequest("پیام مرجع برای پاسخ معتبر نیست.");
                }
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
                Message = messageText ?? string.Empty,
                ReplyToMessageId = vm.ReplyToMessageId,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProjectChatMessages.Add(message);
            group.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var uploadedAttachments = new List<ProjectChatMessageAttachment>();
            if (vm.Attachments != null && vm.Attachments.Any())
            {
                var uploadErrors = new List<string>();
                foreach (var file in vm.Attachments)
                {
                    var uploadResult = await _fileUploadService.UploadFileAsync(file, "project-team-chat");
                    if (uploadResult.Success)
                    {
                        var attachment = new ProjectChatMessageAttachment
                        {
                            ProjectChatMessageId = message.Id,
                            FileName = file.FileName,
                            FilePath = uploadResult.FilePath,
                            FileType = _fileUploadService.GetFileType(file.FileName),
                            FileSize = file.Length,
                            MimeType = NormalizeMimeType(file.ContentType),
                            UploadedAt = DateTime.UtcNow
                        };
                        uploadedAttachments.Add(attachment);
                    }
                    else
                    {
                        uploadErrors.Add($"{file.FileName}: {uploadResult.Error}");
                    }
                }

                if (uploadErrors.Any())
                {
                    _context.ProjectChatMessages.Remove(message);
                    await _context.SaveChangesAsync();
                    return BadRequest($"خطا در آپلود فایل‌ها:\n{string.Join("\n", uploadErrors)}");
                }

                _context.ProjectChatMessageAttachments.AddRange(uploadedAttachments);
                await _context.SaveChangesAsync();
            }

            var result = new ProjectChatMessageVm
            {
                Id = message.Id,
                GroupId = message.ProjectChatGroupId,
                UserId = message.UserId,
                UserName = message.UserName,
                Message = message.Message,
                ReplyToMessageId = message.ReplyToMessageId,
                ReplyPreviewUserName = repliedMessage?.UserName,
                ReplyPreviewMessage = repliedMessage == null
                    ? null
                    : (!string.IsNullOrWhiteSpace(repliedMessage.Message)
                        ? repliedMessage.Message
                        : "فایل"),
                CreatedAt = message.CreatedAt,
                IsCurrentUser = true,
                Attachments = uploadedAttachments.Select(a => new ProjectChatMessageAttachmentVm
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileType = a.FileType,
                    FileSize = a.FileSize,
                    MimeType = a.MimeType,
                    UploadedAt = a.UploadedAt
                }).ToList()
            };

            await _hubContext.Clients
                .Group(ProjectChatHub.GetSignalRGroupName(vm.GroupId))
                .SendAsync("projectGroupMessageCreated", result);

            return Json(result);
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
                .Include(m => m.Attachments)
                .FirstOrDefaultAsync(m => m.Id == messageId);

            if (message == null || message.IsDeleted)
            {
                return NotFound();
            }

            var group = message.ProjectChatGroup;
            var isMessageOwner = message.UserId == userId;
            var canManage = await CanManageGroupMembers(group, userId);
            if (!isMessageOwner && !canManage)
            {
                return Forbid();
            }

            foreach (var attachment in message.Attachments.ToList())
            {
                _fileUploadService.DeleteFile(attachment.FilePath);
                _context.ProjectChatMessageAttachments.Remove(attachment);
            }

            message.IsDeleted = true;
            group.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            await _hubContext.Clients
                .Group(ProjectChatHub.GetSignalRGroupName(group.Id))
                .SendAsync("projectGroupMessageDeleted", new
                {
                    messageId = message.Id
                });

            return Json(new
            {
                success = true,
                messageId = message.Id
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(int attachmentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userId))
            {
                return Unauthorized();
            }

            var attachment = await _context.ProjectChatMessageAttachments
                .Include(a => a.ProjectChatMessage)
                    .ThenInclude(m => m.ProjectChatGroup)
                        .ThenInclude(g => g.Project)
                .FirstOrDefaultAsync(a => a.Id == attachmentId);

            if (attachment == null)
            {
                return NotFound();
            }

            var message = attachment.ProjectChatMessage;
            var group = message.ProjectChatGroup;

            var isMessageOwner = message.UserId == userId;
            var canManage = await CanManageGroupMembers(group, userId);
            if (!isMessageOwner && !canManage)
            {
                return Forbid();
            }

            _fileUploadService.DeleteFile(attachment.FilePath);
            _context.ProjectChatMessageAttachments.Remove(attachment);
            await _context.SaveChangesAsync();

            var hasRemainingAttachments = await _context.ProjectChatMessageAttachments
                .AnyAsync(a => a.ProjectChatMessageId == message.Id);
            var hasText = !string.IsNullOrWhiteSpace(message.Message);

            var messageDeleted = false;
            if (!hasText && !hasRemainingAttachments)
            {
                message.IsDeleted = true;
                messageDeleted = true;
                await _context.SaveChangesAsync();
            }

            await _hubContext.Clients
                .Group(ProjectChatHub.GetSignalRGroupName(group.Id))
                .SendAsync("projectGroupAttachmentDeleted", new
                {
                    attachmentId = attachmentId,
                    messageId = message.Id,
                    messageDeleted
                });

            return Json(new
            {
                success = true,
                attachmentId = attachmentId,
                messageId = message.Id,
                messageDeleted
            });
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

        private static string? NormalizeMimeType(string? mimeType)
        {
            if (string.IsNullOrWhiteSpace(mimeType))
            {
                return null;
            }

            var cleaned = mimeType.Trim();
            var baseType = cleaned.Split(';', 2, StringSplitOptions.TrimEntries | StringSplitOptions.RemoveEmptyEntries).FirstOrDefault();
            return string.IsNullOrWhiteSpace(baseType) ? cleaned : baseType;
        }
    }
}
