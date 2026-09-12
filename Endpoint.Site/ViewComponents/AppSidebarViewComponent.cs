using System.Security.Claims;
using Endpoint.Site.Models;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.Application.Services.DisplaySettingsService;

namespace Endpoint.Site.ViewComponents
{
    public class AppSidebarViewComponent : ViewComponent
    {
        private readonly IUserDisplaySettingsService _displaySettings;

        public AppSidebarViewComponent(IUserDisplaySettingsService displaySettings)
        {
            _displaySettings = displaySettings;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            var isAdmin = UserClaimsPrincipal.IsInRole("ADMIN");
            var prefs = string.IsNullOrEmpty(userId)
                ? new UserDisplaySettingsDto()
                : await _displaySettings.GetAsync(userId);

            var antiforgeryToken = ViewContext.ViewBag.AntiforgeryToken as string;

            var vm = new AppSidebarVm
            {
                AntiforgeryToken = antiforgeryToken,
                UserName = UserClaimsPrincipal.Identity?.Name,
                IsAdmin = isAdmin,
                ShowLeads = prefs.ShowLeads,
                ShowBoards = prefs.ShowBoards,
                ShowMindMaps = prefs.ShowMindMaps,
                ShowDesigns = prefs.ShowDesigns,
                ShowAdminUsers = isAdmin && prefs.ShowAdminUsers,
                ShowAdminLeaves = isAdmin && prefs.ShowAdminLeaves
            };

            return View(vm);
        }
    }
}
