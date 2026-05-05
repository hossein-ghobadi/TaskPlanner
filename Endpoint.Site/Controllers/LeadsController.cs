using Endpoint.Site.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Globalization;
using System.Security.Claims;
using TaskPlanner.Application.Services.LeadService;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Domain.Entities.Users;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class LeadsController : Controller
    {
        private readonly ILeadService _leadService;
        private readonly UserManager<User> _userManager;

        public LeadsController(ILeadService leadService, UserManager<User> userManager)
        {
            _leadService = leadService;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var items = await _leadService.GetMyLeadsAsync(userId);
            return View(items);
        }

        public IActionResult Create()
        {
            return View(new LeadFormVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LeadFormVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                await _leadService.CreateAsync(new CreateLeadDto
                {
                    Title = vm.Title,
                    CompanyName = vm.CompanyName,
                    ContactName = vm.ContactName,
                    Phone = vm.Phone,
                    Email = vm.Email,
                    Notes = vm.Notes,
                    Source = vm.Source,
                    Status = vm.Status
                }, userId);

                TempData["Success"] = "لید با موفقیت ثبت شد.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, ex.Message);
                return View(vm);
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var lead = await _leadService.GetDetailsAsync(id, userId);
            if (lead == null)
                return NotFound();

            if (lead.IsOwner)
            {
                try
                {
                    ViewBag.CollaboratorChoices = await _leadService.GetAvailableCollaboratorsForLeadAsync(id, userId);
                }
                catch
                {
                    ViewBag.CollaboratorChoices = Array.Empty<TaskPlanner.Application.Services.ProjectService.UserSelectDto>();
                }
            }

            return View(lead);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var lead = await _leadService.GetDetailsAsync(id, userId);
            if (lead == null)
                return NotFound();

            if (lead.Status == LeadPipelineStatus.Converted)
            {
                TempData["Error"] = "لید تبدیل‌شده قابل ویرایش نیست.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!lead.IsOwner)
            {
                TempData["Error"] = "فقط مالک لید می‌تواند آن را ویرایش کند.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var vm = new LeadFormVm
            {
                Id = lead.Id,
                Title = lead.Title,
                CompanyName = lead.CompanyName,
                ContactName = lead.ContactName,
                Phone = lead.Phone,
                Email = lead.Email,
                Notes = lead.Notes,
                Source = lead.Source,
                Status = lead.Status
            };
            return View(vm);
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> EditModal(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            var lead = await _leadService.GetDetailsAsync(id, userId);
            if (lead == null)
                return Content("<div class=\"alert alert-warning mb-0\">لید یافت نشد.</div>", "text/html; charset=utf-8");

            if (lead.Status == LeadPipelineStatus.Converted)
                return Content("<div class=\"alert alert-info mb-0\">لید تبدیل‌شده را نمی‌توان ویرایش کرد.</div>", "text/html; charset=utf-8");

            if (!lead.IsOwner)
                return Content("<div class=\"alert alert-warning mb-0\">فقط مالک لید می‌تواند ویرایش کند. شما به‌عنوان همکار فقط مشاهده دارید.</div>", "text/html; charset=utf-8");

            var vm = new LeadFormVm
            {
                Id = lead.Id,
                Title = lead.Title,
                CompanyName = lead.CompanyName,
                ContactName = lead.ContactName,
                Phone = lead.Phone,
                Email = lead.Email,
                Notes = lead.Notes,
                Source = lead.Source,
                Status = lead.Status
            };
            return PartialView("_LeadEditModal", vm);
        }

        [HttpPost("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LeadFormVm vm, [FromForm] string? returnTo)
        {
            if (id != vm.Id)
                return BadRequest();

            var isAjaxList = string.Equals(returnTo, "list", StringComparison.OrdinalIgnoreCase)
                && string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

            if (!ModelState.IsValid)
            {
                if (isAjaxList)
                    return PartialView("_LeadEditModal", vm);
                return View(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                await _leadService.UpdateAsync(new UpdateLeadDto
                {
                    Id = id,
                    Title = vm.Title,
                    CompanyName = vm.CompanyName,
                    ContactName = vm.ContactName,
                    Phone = vm.Phone,
                    Email = vm.Email,
                    Notes = vm.Notes,
                    Source = vm.Source,
                    Status = vm.Status
                }, userId);

                if (isAjaxList)
                    return Json(new { ok = true, message = "لید به‌روزرسانی شد." });

                TempData["Success"] = "لید به‌روزرسانی شد.";
                return RedirectToAction(nameof(Details), new { id });
            }
            catch (Exception ex)
            {
                if (isAjaxList)
                    return new JsonResult(new { ok = false, message = ex.Message }) { StatusCode = 400 };

                ModelState.AddModelError(string.Empty, ex.Message);
                return View(vm);
            }
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Convert([Bind(Prefix = "")] LeadConvertVm vm)
        {
            if (!ModelState.IsValid)
            {
                TempData["Error"] = "اطلاعات تبدیل نامعتبر است.";
                return RedirectToAction(nameof(Details), new { id = vm.LeadId });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                var projectId = await _leadService.ConvertToProjectAsync(vm.LeadId, userId, vm.ProjectName);
                TempData["Success"] = "پروژه از روی لید ایجاد شد.";
                return RedirectToAction("Details", "Projects", new { id = projectId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Details), new { id = vm.LeadId });
            }
        }

        [HttpPost("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                await _leadService.DeleteAsync(id, userId);
                TempData["Success"] = "لید حذف شد.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLeadMember([FromForm] int leadId, [FromForm] string memberUserId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                await _leadService.AddLeadMemberAsync(leadId, memberUserId, userId);
                TempData["Success"] = "همکار به لید اضافه شد.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = leadId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveLeadMember([FromForm] int leadId, [FromForm] string memberUserId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                await _leadService.RemoveLeadMemberAsync(leadId, memberUserId, userId);
                TempData["Success"] = "همکار از لید حذف شد.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = leadId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteLeadByPhone([FromForm] int leadId, [FromForm] string phone)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                await _leadService.InviteUserToLeadAsync(leadId, phone ?? string.Empty, userId);
                TempData["Success"] = "دعوت لید ارسال شد.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = leadId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelLeadInvite([FromForm] int invitationId, [FromForm] int leadId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                await _leadService.CancelLeadInvitationAsync(invitationId, userId);
                TempData["Success"] = "دعوت لغو شد.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = leadId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondLeadInvite(int id, bool accept)
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
                await _leadService.RespondToLeadInvitationAsync(id, userId, user.Phone, accept);
                TempData["Success"] = accept ? "دعوت لید پذیرفته شد." : "دعوت لید رد شد.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction("MyInvitations", "Invitations");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddSession([FromForm] int leadId, [FromForm] string meetingAtPersian, [FromForm] string? notes)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                var localMeetingTime = ParsePersianDateTime(meetingAtPersian);
                await _leadService.AddSessionAsync(new CreateLeadSessionDto
                {
                    LeadId = leadId,
                    ScheduledAt = localMeetingTime,
                    Notes = notes
                }, userId);
                TempData["Success"] = "جلسه لید ثبت شد.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = leadId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSession([FromForm] int leadId, [FromForm] int sessionId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                await _leadService.RemoveSessionAsync(sessionId, userId);
                TempData["Success"] = "جلسه حذف شد.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = leadId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddLeadNote([FromForm] int leadId, [FromForm] string title, [FromForm] string? content)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                await _leadService.AddNoteAsync(new CreateLeadNoteDto
                {
                    LeadId = leadId,
                    Title = title,
                    Content = content
                }, userId);
                TempData["Success"] = "یادداشت لید ثبت شد.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = leadId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteLeadNote([FromForm] int leadId, [FromForm] int noteId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier)!;
            try
            {
                await _leadService.RemoveNoteAsync(noteId, userId);
                TempData["Success"] = "یادداشت لید حذف شد.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = leadId });
        }

        private static DateTime ParsePersianDateTime(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                throw new InvalidOperationException("زمان جلسه را وارد کنید.");

            var normalized = ToLatinDigits(input.Trim())
                .Replace('-', '/')
                .Replace('٫', ':')
                .Replace('،', ':');

            var parts = normalized.Split(' ', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
                throw new InvalidOperationException("فرمت زمان جلسه معتبر نیست. نمونه صحیح: 1405/02/15 14:30");

            var dateParts = parts[0].Split('/');
            var timeParts = parts[1].Split(':');
            if (dateParts.Length != 3 || timeParts.Length != 2)
                throw new InvalidOperationException("فرمت زمان جلسه معتبر نیست. نمونه صحیح: 1405/02/15 14:30");

            var year = int.Parse(dateParts[0], CultureInfo.InvariantCulture);
            var month = int.Parse(dateParts[1], CultureInfo.InvariantCulture);
            var day = int.Parse(dateParts[2], CultureInfo.InvariantCulture);
            var hour = int.Parse(timeParts[0], CultureInfo.InvariantCulture);
            var minute = int.Parse(timeParts[1], CultureInfo.InvariantCulture);

            var persianCalendar = new PersianCalendar();
            var localTime = persianCalendar.ToDateTime(year, month, day, hour, minute, 0, 0);
            return DateTime.SpecifyKind(localTime, DateTimeKind.Local);
        }

        private static string ToLatinDigits(string value)
        {
            return value
                .Replace('۰', '0').Replace('۱', '1').Replace('۲', '2').Replace('۳', '3').Replace('۴', '4')
                .Replace('۵', '5').Replace('۶', '6').Replace('۷', '7').Replace('۸', '8').Replace('۹', '9')
                .Replace('٠', '0').Replace('١', '1').Replace('٢', '2').Replace('٣', '3').Replace('٤', '4')
                .Replace('٥', '5').Replace('٦', '6').Replace('٧', '7').Replace('٨', '8').Replace('٩', '9');
        }
    }
}
