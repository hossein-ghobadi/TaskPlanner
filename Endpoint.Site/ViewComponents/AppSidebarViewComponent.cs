using System.Security.Claims;
using Endpoint.Site.Models;
using Microsoft.AspNetCore.Mvc;

namespace Endpoint.Site.ViewComponents
{
    public class AppSidebarViewComponent : ViewComponent
    {
        public Task<IViewComponentResult> InvokeAsync()
        {
            var antiforgeryToken = ViewContext.ViewBag.AntiforgeryToken as string;

            var vm = new AppSidebarVm
            {
                AntiforgeryToken = antiforgeryToken,
                UserName = UserClaimsPrincipal.Identity?.Name,
                IsAdmin = UserClaimsPrincipal.IsInRole("ADMIN")
            };

            return Task.FromResult<IViewComponentResult>(View(vm));
        }
    }
}
