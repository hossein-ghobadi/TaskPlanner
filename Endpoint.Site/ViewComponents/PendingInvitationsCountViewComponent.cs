using System.Security.Claims;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Domain.Entities.Users;
using TaskPlanner.Persistence.Contexts;

namespace Endpoint.Site.ViewComponents
{
    public class PendingInvitationsCountViewComponent : ViewComponent
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;

        public PendingInvitationsCountViewComponent(
            MVPTestDatabaseContext context,
            UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        public async Task<IViewComponentResult> InvokeAsync()
        {
            var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return View(0);
            }

            var phone = await _userManager.Users
                .AsNoTracking()
                .Where(u => u.Id == userId)
                .Select(u => u.Phone)
                .FirstOrDefaultAsync();

            if (string.IsNullOrWhiteSpace(phone))
            {
                return View(0);
            }

            var projectPending = await _context.ProjectInvitations
                .AsNoTracking()
                .CountAsync(i => i.InviteePhone == phone && i.Status == InvitationStatus.Pending);

            var crmPending = await _context.CrmInvitations
                .AsNoTracking()
                .CountAsync(i => i.InviteePhone == phone && i.Status == InvitationStatus.Pending);

            return View(projectPending + crmPending);
        }
    }
}
