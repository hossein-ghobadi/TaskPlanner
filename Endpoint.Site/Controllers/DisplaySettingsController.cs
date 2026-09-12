using System.Security.Claims;
using Endpoint.Site.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.Application.Services.DisplaySettingsService;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class DisplaySettingsController : Controller
    {
        private readonly IUserDisplaySettingsService _displaySettings;

        public DisplaySettingsController(IUserDisplaySettingsService displaySettings)
        {
            _displaySettings = displaySettings;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var isAdmin = User.IsInRole("ADMIN");
            var prefs = await _displaySettings.GetAsync(userId, cancellationToken);

            ViewData["Title"] = "تنظیمات نمایش";
            return View(new DisplaySettingsVm
            {
                ShowLeads = prefs.ShowLeads,
                ShowBoards = prefs.ShowBoards,
                ShowMindMaps = prefs.ShowMindMaps,
                ShowDesigns = prefs.ShowDesigns,
                CanManageAdminSections = isAdmin,
                ShowAdminUsers = prefs.ShowAdminUsers,
                ShowAdminLeaves = prefs.ShowAdminLeaves
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(DisplaySettingsVm model, CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Challenge();
            }

            var isAdmin = User.IsInRole("ADMIN");

            await _displaySettings.SaveAsync(
                userId,
                new UserDisplaySettingsDto
                {
                    ShowLeads = model.ShowLeads,
                    ShowBoards = model.ShowBoards,
                    ShowMindMaps = model.ShowMindMaps,
                    ShowDesigns = model.ShowDesigns,
                    ShowAdminUsers = model.ShowAdminUsers,
                    ShowAdminLeaves = model.ShowAdminLeaves
                },
                isAdmin,
                cancellationToken);

            TempData["SuccessMessage"] = "تنظیمات نمایش ذخیره شد.";
            return RedirectToAction(nameof(Index));
        }
    }
}
