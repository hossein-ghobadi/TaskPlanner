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
            return View(MapToVm(prefs, isAdmin));
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
                    ShowAdminLeaves = model.ShowAdminLeaves,
                    ShowProjectTasks = model.ShowProjectTasks,
                    ShowProjectKanban = model.ShowProjectKanban,
                    ShowProjectSprints = model.ShowProjectSprints,
                    ShowProjectFeatures = model.ShowProjectFeatures,
                    ShowProjectTickets = model.ShowProjectTickets,
                    ShowProjectGallery = model.ShowProjectGallery,
                    ShowProjectCategories = model.ShowProjectCategories
                },
                isAdmin,
                cancellationToken);

            TempData["SuccessMessage"] = "تنظیمات نمایش ذخیره شد.";
            return RedirectToAction(nameof(Index));
        }

        private static DisplaySettingsVm MapToVm(UserDisplaySettingsDto prefs, bool isAdmin) => new()
        {
            ShowLeads = prefs.ShowLeads,
            ShowBoards = prefs.ShowBoards,
            ShowMindMaps = prefs.ShowMindMaps,
            ShowDesigns = prefs.ShowDesigns,
            CanManageAdminSections = isAdmin,
            ShowAdminUsers = prefs.ShowAdminUsers,
            ShowAdminLeaves = prefs.ShowAdminLeaves,
            ShowProjectTasks = prefs.ShowProjectTasks,
            ShowProjectKanban = prefs.ShowProjectKanban,
            ShowProjectSprints = prefs.ShowProjectSprints,
            ShowProjectFeatures = prefs.ShowProjectFeatures,
            ShowProjectTickets = prefs.ShowProjectTickets,
            ShowProjectGallery = prefs.ShowProjectGallery,
            ShowProjectCategories = prefs.ShowProjectCategories
        };
    }
}
