using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskPlanner.Domain.Entities.Users;
using Endpoint.Site.Models;
using System.Security.Claims;

namespace Endpoint.Site.Controllers
{
    //[Authorize(Roles = "ADMIN")]
    [Route("Admin/[controller]/[action]")]
    public class AdminUsersController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ILogger<AdminUsersController> _logger;

        public AdminUsersController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            ILogger<AdminUsersController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _logger = logger;
        }

        // GET: لیست کاربران
        public async Task<IActionResult> Index(string? searchTerm = null, int page = 1, int pageSize = 20)
        {
            var query = _userManager.Users.AsQueryable();

            // جستجو
            if (!string.IsNullOrWhiteSpace(searchTerm))
            {
                searchTerm = searchTerm.Trim();
                query = query.Where(u =>
                    u.UserName != null && u.UserName.Contains(searchTerm) ||
                    u.Email != null && u.Email.Contains(searchTerm) ||
                    u.FullName != null && u.FullName.Contains(searchTerm) ||
                    u.Phone != null && u.Phone.Contains(searchTerm));
            }

            var totalCount = await query.CountAsync();
            var users = await query
                .OrderByDescending(u => u.InsertTime)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .Select(u => new UserListVm
                {
                    Id = u.Id,
                    UserName = u.UserName ?? "",
                    Email = u.Email ?? "",
                    FullName = u.FullName ?? "",
                    Phone = u.Phone ?? "",
                    IsActive = u.IsActive,
                    InsertTime = u.InsertTime,
                    EmailConfirmed = u.EmailConfirmed
                })
                .ToListAsync();

            // دریافت نقش‌های کاربران
            foreach (var userVm in users)
            {
                var user = await _userManager.FindByIdAsync(userVm.Id);
                if (user != null)
                {
                    var roles = await _userManager.GetRolesAsync(user);
                    userVm.Roles = roles.ToList();
                }
            }

            ViewBag.SearchTerm = searchTerm;
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;
            ViewBag.TotalPages = (int)Math.Ceiling(totalCount / (double)pageSize);

            return View(users);
        }

        // GET: تغییر رمز عبور
        public async Task<IActionResult> ChangePassword(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return NotFound();
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return NotFound();
            }

            var model = new ChangePasswordVm
            {
                UserId = user.Id,
                UserName = user.UserName ?? "",
                FullName = user.FullName ?? ""
            };

            return View(model);
        }

        // POST: تغییر رمز عبور
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangePassword(ChangePasswordVm model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var user = await _userManager.FindByIdAsync(model.UserId);
            if (user == null)
            {
                ModelState.AddModelError(string.Empty, "کاربر یافت نشد");
                return View(model);
            }

            // تغییر رمز عبور - استفاده از ResetPasswordAsync که هم برای کاربران با رمز و هم بدون رمز کار می‌کند
            var token = await _userManager.GeneratePasswordResetTokenAsync(user);
            var resetPasswordResult = await _userManager.ResetPasswordAsync(user, token, model.NewPassword);
            
            if (!resetPasswordResult.Succeeded)
            {
                foreach (var error in resetPasswordResult.Errors)
                {
                    ModelState.AddModelError(string.Empty, error.Description);
                }
                return View(model);
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            _logger.LogInformation("Admin {AdminId} changed password for user {UserId}", currentUserId, user.Id);

            TempData["SuccessMessage"] = "رمز عبور با موفقیت تغییر کرد";
            return RedirectToAction(nameof(Index));
        }

        // POST: حذف کاربر
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "شناسه کاربر نامعتبر است" });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "کاربر یافت نشد" });
            }

            // جلوگیری از حذف خود ادمین
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (user.Id == currentUserId)
            {
                return Json(new { success = false, message = "شما نمی‌توانید حساب کاربری خود را حذف کنید" });
            }

            // بررسی اینکه آیا کاربر نقش ADMIN دارد
            var isAdmin = await _userManager.IsInRoleAsync(user, "ADMIN");
            if (isAdmin)
            {
                return Json(new { success = false, message = "نمی‌توان کاربر ادمین را حذف کرد" });
            }

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
            {
                _logger.LogInformation("Admin {AdminId} deleted user {UserId}", currentUserId, user.Id);
                return Json(new { success = true, message = "کاربر با موفقیت حذف شد" });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = $"خطا در حذف کاربر: {errors}" });
        }

        // POST: فعال/غیرفعال کردن کاربر
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleActive(string id)
        {
            if (string.IsNullOrEmpty(id))
            {
                return Json(new { success = false, message = "شناسه کاربر نامعتبر است" });
            }

            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "کاربر یافت نشد" });
            }

            user.IsActive = !user.IsActive;
            var result = await _userManager.UpdateAsync(user);

            if (result.Succeeded)
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                _logger.LogInformation("Admin {AdminId} toggled active status for user {UserId} to {IsActive}", 
                    currentUserId, user.Id, user.IsActive);
                
                return Json(new { 
                    success = true, 
                    message = user.IsActive ? "کاربر فعال شد" : "کاربر غیرفعال شد",
                    isActive = user.IsActive
                });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = $"خطا در تغییر وضعیت: {errors}" });
        }
    }
}

