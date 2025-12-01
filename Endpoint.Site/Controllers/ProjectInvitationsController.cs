using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Domain.Entities.Users;
using TaskPlanner.Persistence.Contexts;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Controllers
{

    [Authorize]
    [Route("[controller]/[action]")]
    public class ProjectInvitationsController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;

        public ProjectInvitationsController(MVPTestDatabaseContext context, UserManager<User> userManager)
        {
            _userManager = userManager;

            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }
        [HttpGet]
        public async Task<IActionResult> MyInvitations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var sent = await _context.ProjectInvitations
                .Where(i => i.InviterId == userId && i.ProjectId == null)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();
            if (string.IsNullOrEmpty(userId))
                return null;

            var user = await _userManager.FindByIdAsync(userId);
            var received = await _context.ProjectInvitations
                .Where(i => i.ProjectId == null && i.Status == InvitationStatus.Pending && i.InviteePhone == user.Phone)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            ViewBag.SentInvitations = sent;
            ViewBag.ReceivedInvitations = received;

            return View();
        }


        // قبول/رد
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondToInvite(int id, bool accept)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _userManager.FindByIdAsync(currentUserId);
            
            if (currentUser == null)
            {
                TempData["Error"] = "کاربر یافت نشد.";
                return RedirectToAction("Index", "Projects");
            }

            var invite = await _context.ProjectInvitations
                .Include(i => i.Project)
                .FirstOrDefaultAsync(i => i.Id == id);
                
            if (invite == null)
            {
                TempData["Error"] = "دعوت پروژه یافت نشد.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی اینکه دعوت برای کاربر فعلی است
            if (invite.InviteePhone != currentUser.Phone)
            {
                TempData["Error"] = "شما مجاز به پاسخ به این دعوت نیستید.";
                return RedirectToAction("Index", "Projects");
            }

            invite.Status = accept ? InvitationStatus.Accepted : InvitationStatus.Rejected;
            invite.RespondedAt = DateTime.UtcNow;
            
            // تنظیم InviteeId در صورت خالی بودن
            if (string.IsNullOrEmpty(invite.InviteeId))
            {
                invite.InviteeId = currentUserId;
            }

            if (accept && invite.ProjectId != null)
            {
                // بررسی اینکه کاربر قبلاً عضو پروژه نیست
                var existingMember = await _context.ProjectMembers
                    .FirstOrDefaultAsync(m => m.ProjectId == invite.ProjectId.Value && m.UserId == currentUserId);
                
                if (existingMember == null)
                {
                    _context.ProjectMembers.Add(new ProjectMember
                    {
                        ProjectId = invite.ProjectId.Value,
                        UserId = currentUserId
                    });
                }
            }

            _context.Update(invite);
            await _context.SaveChangesAsync();

            TempData["Success"] = accept ? "دعوت پروژه پذیرفته شد ✅" : "دعوت پروژه رد شد ❌";
            return RedirectToAction("Index", "Projects");
        }


    }
}
