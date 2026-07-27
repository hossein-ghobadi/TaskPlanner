using Endpoint.Site.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskPlanner.Application.Services.CrmService;
using TaskPlanner.Application.Services.ProjectService;
using TaskPlanner.Domain.Entities.Users;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class CrmController : Controller
    {
        private readonly ICrmService _crmService;
        private readonly UserManager<User> _userManager;

        public CrmController(ICrmService crmService, UserManager<User> userManager)
        {
            _crmService = crmService;
            _userManager = userManager;
        }

        /// <summary>مدیریت CRM شخصی کاربر (دعوت و اعضا).</summary>
        [HttpGet]
        public async Task<IActionResult> Manage(int? crmId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var personal = await _crmService.GetOrCreatePersonalCrmAsync(userId);
            var targetCrmId = crmId ?? personal.Id;

            var access = await _crmService.GetCrmAccessAsync(targetCrmId, userId);
            if (access == null)
                return NotFound();

            if (!access.CanManageMembers)
            {
                TempData["Error"] = "فقط مالک CRM می‌تواند اعضا و دعوت‌ها را مدیریت کند.";
                return RedirectToAction("Index", "Leads");
            }

            var crms = await _crmService.GetAccessibleCrmsAsync(userId);
            var owned = crms.Where(c => c.IsOwner).ToList();
            var current = owned.FirstOrDefault(c => c.Id == targetCrmId)
                ?? owned.FirstOrDefault(c => c.Id == personal.Id)
                ?? owned.FirstOrDefault();

            if (current == null)
                return NotFound();

            IReadOnlyList<UserSelectDto> collaboratorChoices;
            try
            {
                collaboratorChoices = await _crmService.GetAvailableCollaboratorsAsync(current.Id, userId);
            }
            catch
            {
                collaboratorChoices = Array.Empty<UserSelectDto>();
            }

            var members = await _crmService.GetMembersAsync(current.Id, userId);
            var pending = await _crmService.GetPendingInvitationsAsync(current.Id, userId);

            var vm = new CrmManageVm
            {
                CrmId = current.Id,
                DisplayLabel = current.DisplayLabel,
                OwnedCrms = owned,
                Members = members.Select(m => new CrmManageMemberVm
                {
                    UserId = m.UserId,
                    DisplayName = m.DisplayName
                }).ToList(),
                PendingInvitations = pending.Select(i => new CrmManageInviteVm
                {
                    Id = i.Id,
                    InviteePhone = i.InviteePhone,
                    InviteeDisplayName = i.InviteeDisplayName
                }).ToList(),
                CollaboratorChoices = collaboratorChoices.ToList()
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddMember([FromForm] int crmId, [FromForm] string memberUserId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                await _crmService.AddMemberAsync(crmId, memberUserId, userId);
                TempData["Success"] = "همکار به CRM اضافه شد.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Manage), new { crmId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember([FromForm] int crmId, [FromForm] string memberUserId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                await _crmService.RemoveMemberAsync(crmId, memberUserId, userId);
                TempData["Success"] = "همکار از CRM حذف شد.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Manage), new { crmId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteByPhone([FromForm] int crmId, [FromForm] string phone)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                await _crmService.InviteByPhoneAsync(crmId, phone ?? string.Empty, userId);
                TempData["Success"] = "دعوت به CRM ارسال شد.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Manage), new { crmId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelInvite([FromForm] int invitationId, [FromForm] int crmId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                await _crmService.CancelInvitationAsync(invitationId, userId);
                TempData["Success"] = "دعوت CRM لغو شد.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Manage), new { crmId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondInvite(int id, bool accept)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var user = await _userManager.FindByIdAsync(userId);
            if (user == null || string.IsNullOrWhiteSpace(user.Phone))
            {
                TempData["Error"] = "شماره موبایل حساب شما ثبت نشده است.";
                return RedirectToAction("MyInvitations", "Invitations");
            }

            try
            {
                await _crmService.RespondToInvitationAsync(id, userId, user.Phone, accept);
                TempData["Success"] = accept ? "دعوت به CRM پذیرفته شد." : "دعوت به CRM رد شد.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("MyInvitations", "Invitations");
        }
    }
}
