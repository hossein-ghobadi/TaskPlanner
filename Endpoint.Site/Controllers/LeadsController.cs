using Endpoint.Site.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskPlanner.Application.Services.LeadService;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class LeadsController : Controller
    {
        private readonly ILeadService _leadService;

        public LeadsController(ILeadService leadService)
        {
            _leadService = leadService;
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
    }
}
