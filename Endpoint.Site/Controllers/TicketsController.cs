using System.Security.Claims;
using Endpoint.Site.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Domain.Entities.Users;
using TaskPlanner.Persistence.Contexts;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]/{id?}")]
    public class TicketsController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;

        public TicketsController(MVPTestDatabaseContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(
            int? projectId,
            int? id,
            int? featureId,
            string? askedToUserId,
            string? createdByUserId)
        {
            var pid = projectId ?? id;
            if (!pid.HasValue)
                return RedirectToAction("Index", "Projects");

            var projectIdVal = pid.Value;
            if (!await HasProjectAccessAsync(projectIdVal))
                return RedirectToAction("Index", "Projects");

            var project = await _context.Projects.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectIdVal);
            if (project == null)
                return NotFound();

            var userId = CurrentUserId();
            var isCreator = string.Equals(project.CreatorUserId, userId, StringComparison.Ordinal);

            var query = _context.ProjectTickets.AsNoTracking()
                .Where(t => t.ProjectId == projectIdVal)
                .Include(t => t.Feature)
                .Include(t => t.AskedToUser)
                .Include(t => t.CreatedByUser)
                .AsQueryable();

            // سازنده پروژه همه تیکت‌ها را می‌بیند؛ بقیه فقط تیکت‌های خودشان یا تیکت‌هایی که از آن‌ها سوال شده
            if (!isCreator)
            {
                query = query.Where(t =>
                    t.CreatedByUserId == userId ||
                    t.AskedToUserId == userId);
            }

            if (featureId.HasValue)
                query = query.Where(t => t.FeatureId == featureId.Value);

            if (!string.IsNullOrWhiteSpace(askedToUserId))
                query = query.Where(t => t.AskedToUserId == askedToUserId);

            if (!string.IsNullOrWhiteSpace(createdByUserId))
                query = query.Where(t => t.CreatedByUserId == createdByUserId);

            var tickets = await query
                .OrderByDescending(t => t.CreatedAt)
                .ToListAsync();

            var features = await _context.ProjectFeatures.AsNoTracking()
                .Where(f => f.ProjectId == projectIdVal)
                .OrderBy(f => f.Name)
                .Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.Name,
                    Selected = featureId == f.Id
                })
                .ToListAsync();

            features.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "همه فیچرها",
                Selected = !featureId.HasValue
            });

            var members = await GetProjectMemberOptionsAsync(projectIdVal);
            var memberOptions = members.Select(m => new SelectListItem
            {
                Value = m.UserId,
                Text = m.DisplayName
            }).ToList();

            ViewBag.ProjectId = projectIdVal;
            ViewData["Title"] = "تیکت‌های پروژه";

            return View(new TicketIndexVm
            {
                ProjectId = projectIdVal,
                ProjectName = project.Name,
                IsProjectCreator = isCreator,
                FeatureId = featureId,
                AskedToUserId = askedToUserId,
                CreatedByUserId = createdByUserId,
                FeatureOptions = features,
                MemberOptions = memberOptions,
                Tickets = tickets.Select(t => new TicketListItemVm
                {
                    Id = t.Id,
                    Title = t.Title,
                    Type = t.Type,
                    FeatureName = t.Feature?.Name,
                    AskedToUserName = DisplayName(t.AskedToUser),
                    CreatedByUserName = DisplayName(t.CreatedByUser),
                    Status = t.Status,
                    CreatedAt = t.CreatedAt
                }).ToList()
            });
        }

        public async Task<IActionResult> Create(int? projectId)
        {
            if (!projectId.HasValue)
                return RedirectToAction("Index", "Projects");

            if (!await HasProjectAccessAsync(projectId.Value))
                return RedirectToAction("Index", "Projects");

            var project = await _context.Projects.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectId.Value);
            if (project == null)
                return NotFound();

            ViewBag.ProjectId = projectId.Value;
            ViewBag.ProjectName = project.Name;
            ViewData["Title"] = "ثبت تیکت جدید";

            return View(await BuildCreateVmAsync(projectId.Value));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TicketCreateVm vm, int projectId)
        {
            if (!await HasProjectAccessAsync(projectId))
                return RedirectToAction("Index", "Projects");

            var project = await _context.Projects.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null)
                return NotFound();

            await ValidateCreateVmAsync(vm, projectId);

            if (!ModelState.IsValid)
            {
                ViewBag.ProjectId = projectId;
                ViewBag.ProjectName = project.Name;
                ViewData["Title"] = "ثبت تیکت جدید";
                return View(await BuildCreateVmAsync(projectId, vm));
            }

            var userId = CurrentUserId();
            var ticket = new ProjectTicket
            {
                ProjectId = projectId,
                FeatureId = vm.FeatureId,
                Title = vm.Title.Trim(),
                Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
                Type = vm.Type,
                AskedToUserId = vm.AskedToUserId,
                CreatedByUserId = userId,
                Status = TicketStatus.Open,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var initialBody = !string.IsNullOrWhiteSpace(vm.Description)
                ? vm.Description.Trim()
                : vm.Title.Trim();

            ticket.Messages.Add(new ProjectTicketMessage
            {
                AuthorUserId = userId!,
                Kind = TicketMessageKind.Question,
                Body = initialBody,
                CreatedAt = DateTime.UtcNow
            });

            _context.ProjectTickets.Add(ticket);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تیکت ثبت شد.";
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        public async Task<IActionResult> Details(int id)
        {
            var ticket = await _context.ProjectTickets.AsNoTracking()
                .Include(t => t.Project)
                .Include(t => t.Feature)
                .Include(t => t.AskedToUser)
                .Include(t => t.CreatedByUser)
                .Include(t => t.Messages)
                    .ThenInclude(m => m.AuthorUser)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
                return NotFound();

            if (!await HasProjectAccessAsync(ticket.ProjectId))
                return RedirectToAction("Index", "Projects");

            if (!await CanViewTicketAsync(ticket))
                return RedirectToAction(nameof(Index), new { projectId = ticket.ProjectId });

            // تیکت‌های قدیمی بدون پیام: اولین سوال از Description ساخته می‌شود
            if (ticket.Messages.Count == 0 &&
                (!string.IsNullOrWhiteSpace(ticket.Description) || !string.IsNullOrWhiteSpace(ticket.Title)))
            {
                var seedAuthor = ticket.CreatedByUserId ?? ticket.AskedToUserId;
                if (!string.IsNullOrEmpty(seedAuthor))
                {
                    _context.ProjectTicketMessages.Add(new ProjectTicketMessage
                    {
                        TicketId = ticket.Id,
                        AuthorUserId = seedAuthor,
                        Kind = TicketMessageKind.Question,
                        Body = !string.IsNullOrWhiteSpace(ticket.Description)
                            ? ticket.Description!
                            : ticket.Title,
                        CreatedAt = ticket.CreatedAt
                    });
                    await _context.SaveChangesAsync();

                    ticket = await _context.ProjectTickets.AsNoTracking()
                        .Include(t => t.Project)
                        .Include(t => t.Feature)
                        .Include(t => t.AskedToUser)
                        .Include(t => t.CreatedByUser)
                        .Include(t => t.Messages)
                            .ThenInclude(m => m.AuthorUser)
                        .FirstAsync(t => t.Id == id);
                }
            }

            var userId = CurrentUserId();
            var isCreator = string.Equals(ticket.Project.CreatorUserId, userId, StringComparison.Ordinal);
            var isAsker = string.Equals(ticket.CreatedByUserId, userId, StringComparison.Ordinal);
            var isAsked = string.Equals(ticket.AskedToUserId, userId, StringComparison.Ordinal);

            ViewBag.ProjectId = ticket.ProjectId;
            ViewData["Title"] = ticket.Title;

            return View(new TicketDetailsVm
            {
                Id = ticket.Id,
                ProjectId = ticket.ProjectId,
                ProjectName = ticket.Project.Name,
                Title = ticket.Title,
                Type = ticket.Type,
                FeatureId = ticket.FeatureId,
                FeatureName = ticket.Feature?.Name,
                AskedToUserId = ticket.AskedToUserId,
                AskedToUserName = DisplayName(ticket.AskedToUser),
                CreatedByUserId = ticket.CreatedByUserId,
                CreatedByUserName = DisplayName(ticket.CreatedByUser),
                Status = ticket.Status,
                CreatedAt = ticket.CreatedAt,
                UpdatedAt = ticket.UpdatedAt,
                CanAnswer = isAsked,
                CanAskFollowUp = isAsker,
                CanManage = isCreator || isAsker,
                Messages = ticket.Messages
                    .OrderBy(m => m.CreatedAt)
                    .ThenBy(m => m.Id)
                    .Select(m => new TicketMessageItemVm
                    {
                        Id = m.Id,
                        Kind = m.Kind,
                        Body = m.Body,
                        AuthorName = DisplayName(m.AuthorUser),
                        AuthorUserId = m.AuthorUserId,
                        CreatedAt = m.CreatedAt
                    }).ToList()
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddAnswer(TicketAddMessageVm vm)
        {
            var ticket = await _context.ProjectTickets
                .FirstOrDefaultAsync(t => t.Id == vm.TicketId);

            if (ticket == null)
                return NotFound();

            if (!await HasProjectAccessAsync(ticket.ProjectId))
                return RedirectToAction("Index", "Projects");

            var userId = CurrentUserId();
            if (!string.Equals(ticket.AskedToUserId, userId, StringComparison.Ordinal))
            {
                TempData["Error"] = "فقط فرد مورد سوال می‌تواند پاسخ دهد.";
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            if (string.IsNullOrWhiteSpace(vm.Body))
            {
                TempData["Error"] = "متن پاسخ الزامی است.";
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            _context.ProjectTicketMessages.Add(new ProjectTicketMessage
            {
                TicketId = ticket.Id,
                AuthorUserId = userId!,
                Kind = TicketMessageKind.Answer,
                Body = vm.Body.Trim(),
                CreatedAt = DateTime.UtcNow
            });

            ticket.Status = vm.Status ?? TicketStatus.Answered;
            if (ticket.Status == TicketStatus.Open)
                ticket.Status = TicketStatus.Answered;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "پاسخ ثبت شد.";
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddQuestion(TicketAddMessageVm vm)
        {
            var ticket = await _context.ProjectTickets
                .FirstOrDefaultAsync(t => t.Id == vm.TicketId);

            if (ticket == null)
                return NotFound();

            if (!await HasProjectAccessAsync(ticket.ProjectId))
                return RedirectToAction("Index", "Projects");

            var userId = CurrentUserId();
            if (!string.Equals(ticket.CreatedByUserId, userId, StringComparison.Ordinal))
            {
                TempData["Error"] = "فقط ایجادکننده تیکت می‌تواند سوال بعدی بپرسد.";
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            if (string.IsNullOrWhiteSpace(vm.Body))
            {
                TempData["Error"] = "متن سوال الزامی است.";
                return RedirectToAction(nameof(Details), new { id = ticket.Id });
            }

            _context.ProjectTicketMessages.Add(new ProjectTicketMessage
            {
                TicketId = ticket.Id,
                AuthorUserId = userId!,
                Kind = TicketMessageKind.Question,
                Body = vm.Body.Trim(),
                CreatedAt = DateTime.UtcNow
            });

            ticket.Status = TicketStatus.Open;
            ticket.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            TempData["Success"] = "سوال ثبت شد.";
            return RedirectToAction(nameof(Details), new { id = ticket.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var ticket = await _context.ProjectTickets
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
                return NotFound();

            if (!await HasProjectAccessAsync(ticket.ProjectId))
                return RedirectToAction("Index", "Projects");

            var userId = CurrentUserId();
            var isCreator = string.Equals(ticket.Project.CreatorUserId, userId, StringComparison.Ordinal);
            var isOwner = string.Equals(ticket.CreatedByUserId, userId, StringComparison.Ordinal);

            if (!isCreator && !isOwner)
            {
                TempData["Error"] = "شما مجاز به حذف این تیکت نیستید.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var projectId = ticket.ProjectId;
            _context.ProjectTickets.Remove(ticket);
            await _context.SaveChangesAsync();

            TempData["Success"] = "تیکت حذف شد.";
            return RedirectToAction(nameof(Index), new { projectId });
        }

        private async Task<TicketCreateVm> BuildCreateVmAsync(int projectId, TicketCreateVm? incoming = null)
        {
            var selectedFeatureId = incoming?.FeatureId;

            var features = await _context.ProjectFeatures.AsNoTracking()
                .Where(f => f.ProjectId == projectId)
                .OrderBy(f => f.Name)
                .Select(f => new SelectListItem
                {
                    Value = f.Id.ToString(),
                    Text = f.Name,
                    Selected = selectedFeatureId == f.Id
                })
                .ToListAsync();

            features.Insert(0, new SelectListItem
            {
                Value = "",
                Text = "— بدون فیچر —",
                Selected = !selectedFeatureId.HasValue
            });

            var members = await GetProjectMemberOptionsAsync(projectId);
            var selectedType = incoming?.Type ?? TicketType.Other;

            return new TicketCreateVm
            {
                Title = incoming?.Title ?? "",
                Description = incoming?.Description,
                Type = selectedType,
                FeatureId = incoming?.FeatureId,
                AskedToUserId = incoming?.AskedToUserId ?? "",
                FeatureOptions = features,
                TypeOptions = BuildTypeOptions(selectedType),
                MemberOptions = members.Select(m => new SelectListItem
                {
                    Value = m.UserId,
                    Text = m.DisplayName,
                    Selected = string.Equals(m.UserId, incoming?.AskedToUserId, StringComparison.Ordinal)
                }).ToList()
            };
        }

        private static List<SelectListItem> BuildTypeOptions(TicketType selected)
        {
            return new List<SelectListItem>
            {
                new() { Value = ((int)TicketType.SpecificationMissing).ToString(), Text = "نقص مشخصات (Specification Missing)", Selected = selected == TicketType.SpecificationMissing },
                new() { Value = ((int)TicketType.Bug).ToString(), Text = "باگ (Bug)", Selected = selected == TicketType.Bug },
                new() { Value = ((int)TicketType.CriticalBug).ToString(), Text = "باگ بحرانی (Critical Bug)", Selected = selected == TicketType.CriticalBug },
                new() { Value = ((int)TicketType.Improvement).ToString(), Text = "بهبود (Improvement)", Selected = selected == TicketType.Improvement },
                new() { Value = ((int)TicketType.Other).ToString(), Text = "سایر (Other)", Selected = selected == TicketType.Other }
            };
        }

        private async Task ValidateCreateVmAsync(TicketCreateVm vm, int projectId)
        {
            if (!Enum.IsDefined(typeof(TicketType), vm.Type))
                ModelState.AddModelError(nameof(vm.Type), "نوع تیکت معتبر نیست.");

            if (vm.FeatureId.HasValue)
            {
                var featureOk = await _context.ProjectFeatures.AsNoTracking()
                    .AnyAsync(f => f.Id == vm.FeatureId.Value && f.ProjectId == projectId);
                if (!featureOk)
                    ModelState.AddModelError(nameof(vm.FeatureId), "فیچر انتخاب‌شده معتبر نیست.");
            }

            if (!string.IsNullOrWhiteSpace(vm.AskedToUserId))
            {
                var members = await GetProjectMemberOptionsAsync(projectId);
                if (!members.Any(m => string.Equals(m.UserId, vm.AskedToUserId, StringComparison.Ordinal)))
                    ModelState.AddModelError(nameof(vm.AskedToUserId), "فرد مورد سوال باید عضو پروژه باشد.");
            }
        }

        private async Task<bool> CanViewTicketAsync(ProjectTicket ticket)
        {
            var userId = CurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return false;

            var isCreator = await _context.Projects.AsNoTracking()
                .AnyAsync(p => p.Id == ticket.ProjectId && p.CreatorUserId == userId);
            if (isCreator)
                return true;

            return string.Equals(ticket.CreatedByUserId, userId, StringComparison.Ordinal) ||
                   string.Equals(ticket.AskedToUserId, userId, StringComparison.Ordinal);
        }

        private async Task<List<(string UserId, string DisplayName)>> GetProjectMemberOptionsAsync(int projectId)
        {
            var project = await _context.Projects.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectId);

            var memberIds = await _context.ProjectMembers.AsNoTracking()
                .Where(m => m.ProjectId == projectId && m.UserId != null && m.UserId != "")
                .Select(m => m.UserId)
                .ToListAsync();

            var invitedIds = await _context.ProjectInvitations.AsNoTracking()
                .Where(i => i.ProjectId == projectId
                            && i.Status == InvitationStatus.Accepted
                            && i.InviteeId != null
                            && i.InviteeId != "")
                .Select(i => i.InviteeId!)
                .ToListAsync();

            var allIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in memberIds)
                allIds.Add(id);
            foreach (var id in invitedIds)
                allIds.Add(id);

            if (project != null && !string.IsNullOrEmpty(project.CreatorUserId))
                allIds.Add(project.CreatorUserId);

            var result = new List<(string UserId, string DisplayName)>();
            foreach (var uid in allIds)
            {
                var user = await _userManager.FindByIdAsync(uid);
                if (user == null)
                    continue;

                var name = DisplayName(user);
                if (project != null &&
                    string.Equals(uid, project.CreatorUserId, StringComparison.Ordinal))
                {
                    name += " — سازنده پروژه";
                }

                result.Add((uid, name));
            }

            return result.OrderBy(u => u.DisplayName).ToList();
        }

        private async Task<bool> HasProjectAccessAsync(int projectId)
        {
            var userId = CurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return false;

            var isMemberOrCreator = await _context.Projects.AsNoTracking()
                .AnyAsync(p => p.Id == projectId &&
                               (p.CreatorUserId == userId ||
                                p.Members.Any(m => m.UserId == userId)));
            if (isMemberOrCreator)
                return true;

            return await _context.ProjectInvitations.AsNoTracking()
                .AnyAsync(i => i.ProjectId == projectId
                               && i.InviteeId == userId
                               && i.Status == InvitationStatus.Accepted);
        }

        private string? CurrentUserId() =>
            User.FindFirstValue(ClaimTypes.NameIdentifier);

        private static string DisplayName(User? user)
        {
            if (user == null)
                return "—";

            if (!string.IsNullOrWhiteSpace(user.FullName))
                return user.FullName!;

            return user.UserName ?? user.Email ?? "—";
        }
    }
}
