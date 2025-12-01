using Endpoint.Site.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskPlanner.Domain.Entities.Users;
using TaskPlanner.Persistence.Contexts;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class InvitationsController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;

        public InvitationsController(MVPTestDatabaseContext context, UserManager<User> userManager)
        {
            _userManager = userManager;

            _context = context;
        }
        public IActionResult Index()
        {
            return View();
        }



        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteSystemUser(string phone)
        {
            var inviterId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(phone))
            {
                TempData["Error"] = "شماره تلفن وارد نشده است.";
                return RedirectToAction("MyInvitations");
            }

            phone = phone.Trim();

            var inviter = await _userManager.FindByIdAsync(inviterId);
            if (inviter != null && !string.IsNullOrWhiteSpace(inviter.Phone) &&
                string.Equals(inviter.Phone, phone, StringComparison.OrdinalIgnoreCase))
            {
                TempData["Error"] = "نمی‌توانید خودتان را دعوت کنید.";
                return RedirectToAction("MyInvitations");
            }

            // بررسی تکرار دعوت در انتظار
            var exists = await _context.ProjectInvitations
                .AnyAsync(i => i.InviterId == inviterId && i.InviteePhone == phone && i.ProjectId == null && i.Status == InvitationStatus.Pending);

            if (exists)
            {
                TempData["Error"] = "برای این شماره قبلاً دعوت در انتظار ارسال شده است.";
                return RedirectToAction("MyInvitations");
            }

            var alreadyAccepted = await _context.ProjectInvitations
                .AnyAsync(i => i.InviteePhone == phone && i.ProjectId == null && i.Status == InvitationStatus.Accepted);

            if (alreadyAccepted)
            {
                TempData["Error"] = "این کاربر قبلاً دعوت شما یا فرد دیگری را پذیرفته است.";
                return RedirectToAction("MyInvitations");
            }

            // بررسی وجود کاربر در سیستم
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Phone == phone);

            // ✅ ثبت دعوت (حتی اگر کاربر هنوز در سیستم نباشد)
            var invite = new ProjectInvitation
            {
                InviterId = inviterId,
                InviteePhone = phone,
                InviteeId = user?.Id ?? string.Empty,  // اگر کاربر وجود دارد ثبت شود
                ProjectId = null,                      // null ⇒ دعوت به سیستم
                Status = InvitationStatus.Pending
            };

            _context.ProjectInvitations.Add(invite);
            await _context.SaveChangesAsync();

            TempData["Success"] = user != null
                ? $"دعوت برای {user.FullName ?? phone} ارسال شد."
                : $"کاربر {phone} هنوز ثبت‌نام نکرده است. دعوت در حالت انتظار باقی می‌ماند.";

            return RedirectToAction("MyInvitations");
        }

        // 📜 لیست دعوت‌های من (دریافتی و ارسالی)
        //[HttpGet]
        //public async Task<IActionResult> MyInvitations([FromServices] UserManager<User> _userManager)
        //{
        //    var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        //    if (userId == null)
        //    {
        //        TempData["Error"] = "کاربر لاگین نشده است.";
        //        return RedirectToAction("Index", "Home");
        //    }

        //    // ✅ دعوت‌هایی که کاربر ارسال کرده
        //    var sent = await _context.ProjectInvitations
        //        .Where(i => i.InviterId == userId)
        //        .OrderByDescending(i => i.CreatedAt)
        //        .ToListAsync();

        //    // ✅ دعوت‌هایی که کاربر دریافت کرده (با استفاده از شماره موبایل)
        //    var myUser = await _userManager.FindByIdAsync(userId);
        //    var myPhone = myUser?.PhoneNumber;

        //    var received = string.IsNullOrEmpty(myPhone)
        //        ? new List<ProjectInvitation>()
        //        : await _context.ProjectInvitations
        //            .Where(i => i.InviteePhone == myPhone)
        //            .OrderByDescending(i => i.CreatedAt)
        //            .ToListAsync();

        //    // ✅ ساخت lookup از نام کاربران برای InviterId
        //    var allUsers = await _userManager.Users
        //        .Select(u => new
        //        {
        //            u.Id,
        //            DisplayName = string.IsNullOrEmpty(u.FullName) ? u.UserName : u.FullName
        //        })
        //        .ToListAsync();

        //    // دیکشنری سریع lookup برای نام‌ها
        //    ViewBag.UserLookup = allUsers.ToDictionary(u => u.Id, u => u.DisplayName);

        //    ViewBag.SentInvitations = sent;
        //    ViewBag.ReceivedInvitations = received;

        //    return View();
        //}
        [HttpGet]
        public async Task<IActionResult> MyInvitations()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var user = await _userManager.FindByIdAsync(userId);
            var phone = user?.Phone;

            var projectInvitations = await _context.ProjectInvitations
                .Include(i => i.Project)
                .Where(i => i.InviteePhone == phone && i.ProjectId != null)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var systemInvitations = await _context.ProjectInvitations
                .Where(i => i.InviteePhone == phone && i.ProjectId == null)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var sentProjectInvitations = await _context.ProjectInvitations
                .Include(i => i.Project)
                .Where(i => i.InviterId == userId && i.ProjectId != null)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var sentSystemInvitations = await _context.ProjectInvitations
                .Where(i => i.InviterId == userId && i.ProjectId == null)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            // 📍 دیکشنری از نام کاربران (دعوت‌کنندگان و دعوت‌شوندگان)
            var inviterIds = projectInvitations.Select(i => i.InviterId)
                              .Concat(systemInvitations.Select(i => i.InviterId))
                              .Concat(sentProjectInvitations.Select(i => i.InviterId))
                              .Concat(sentSystemInvitations.Select(i => i.InviterId))
                              .Distinct()
                              .ToList();

            var inviteeIds = sentProjectInvitations.Where(i => !string.IsNullOrEmpty(i.InviteeId))
                              .Select(i => i.InviteeId)
                              .Concat(sentSystemInvitations.Where(i => !string.IsNullOrEmpty(i.InviteeId))
                                  .Select(i => i.InviteeId))
                              .Distinct()
                              .ToList();

            var allUserIds = inviterIds.Concat(inviteeIds).Distinct().ToList();

            var users = await _userManager.Users
                .Where(u => allUserIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u =>
                    !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : (u.UserName ?? "کاربر ناشناس"));

            // 👇 نگهداری در ViewBag برای استفاده در ویو
            ViewBag.UserLookup = users;

            var model = new CombinedInvitationsVm
            {
                ProjectInvitations = projectInvitations,
                UserInvitations = systemInvitations,
                SentProjectInvitations = sentProjectInvitations,
                SentSystemInvitations = sentSystemInvitations
            };

            return View(model);
        }


        // ✅ قبول یا رد دعوت
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RespondToSystemInvite(int id, bool accept)
        {
            var invite = await _context.ProjectInvitations.FindAsync(id);
            if (invite == null || invite.ProjectId != null)
            {
                TempData["Error"] = "دعوت معتبر نیست.";
                return RedirectToAction("MyInvitations");
            }

            invite.Status = accept ? InvitationStatus.Accepted : InvitationStatus.Rejected;
            invite.RespondedAt = DateTime.UtcNow;
            _context.Update(invite);
            await _context.SaveChangesAsync();

            TempData["Success"] = accept ? "دعوت عضویت پذیرفته شد ✅" : "دعوت عضویت رد شد ❌";
            return RedirectToAction("MyInvitations");
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteSystemInvite(int id)
        {
            var inviterId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var invite = await _context.ProjectInvitations
                .FirstOrDefaultAsync(i => i.Id == id && i.ProjectId == null && i.InviterId == inviterId);

            if (invite == null)
            {
                TempData["Error"] = "دعوت مورد نظر یافت نشد.";
                return RedirectToAction("MyInvitations");
            }

            if (invite.Status != InvitationStatus.Pending)
            {
                TempData["Error"] = "فقط دعوت‌های در انتظار قابل حذف هستند.";
                return RedirectToAction("MyInvitations");
            }

            _context.ProjectInvitations.Remove(invite);
            await _context.SaveChangesAsync();

            TempData["Success"] = "دعوت با موفقیت حذف شد.";
            return RedirectToAction("MyInvitations");
        }

        /// <summary>
        /// حذف عضو از پروژه (از طریق صفحه دعوت‌ها)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMemberFromProject(int projectId, string memberUserId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberUserId))
            {
                TempData["Error"] = "شناسه کاربر نامعتبر است.";
                return RedirectToAction("MyInvitations");
            }

            // بررسی اینکه دعوت توسط کاربر فعلی ارسال شده و پذیرفته شده است
            var invitation = await _context.ProjectInvitations
                .FirstOrDefaultAsync(i => i.ProjectId == projectId 
                    && i.InviterId == currentUserId 
                    && i.InviteeId == memberUserId 
                    && i.Status == InvitationStatus.Accepted);

            if (invitation == null)
            {
                TempData["Error"] = "دعوت مورد نظر یافت نشد یا شما مجاز به حذف این عضو نیستید.";
                return RedirectToAction("MyInvitations");
            }

            // بررسی اینکه پروژه وجود دارد
            var project = await _context.Projects.FindAsync(projectId);
            if (project == null)
            {
                TempData["Error"] = "پروژه یافت نشد.";
                return RedirectToAction("MyInvitations");
            }

            // بررسی دسترسی: فقط سازنده پروژه یا دعوت‌کننده می‌تواند عضو را حذف کند
            if (project.CreatorUserId != currentUserId && invitation.InviterId != currentUserId)
            {
                TempData["Error"] = "شما مجاز به حذف این عضو نیستید.";
                return RedirectToAction("MyInvitations");
            }

            try
            {
                // حذف عضو از ProjectMembers
                var member = await _context.ProjectMembers
                    .FirstOrDefaultAsync(m => m.ProjectId == projectId && m.UserId == memberUserId);

                if (member != null)
                {
                    _context.ProjectMembers.Remove(member);
                }

                // حذف دعوت یا تغییر وضعیت آن
                _context.ProjectInvitations.Remove(invitation);

                // حذف تسک‌های مرتبط با این کاربر (اختیاری - بسته به نیاز کسب‌وکار)
                // در اینجا فقط عضو را حذف می‌کنیم و تسک‌ها بدون مسئول می‌مانند

                await _context.SaveChangesAsync();

                TempData["Success"] = "عضو با موفقیت از پروژه حذف شد.";
                return RedirectToAction("MyInvitations");
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"خطا در حذف عضو: {ex.Message}";
                return RedirectToAction("MyInvitations");
            }
        }

    }
}
