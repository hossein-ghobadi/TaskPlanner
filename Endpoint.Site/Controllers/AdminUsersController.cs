using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskPlanner.Domain.Entities.Users;
using Endpoint.Site.Models;
using System.Security.Claims;
using TaskPlanner.Persistence.Contexts;

namespace Endpoint.Site.Controllers
{
    [Authorize(Roles = "ADMIN")]
    [Route("Admin/[controller]/[action]")]
    public class AdminUsersController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly MVPTestDatabaseContext _dbContext;
        private readonly ILogger<AdminUsersController> _logger;

        public AdminUsersController(
            UserManager<User> userManager,
            RoleManager<IdentityRole> roleManager,
            MVPTestDatabaseContext dbContext,
            ILogger<AdminUsersController> logger)
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _dbContext = dbContext;
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
            ViewBag.AllRoles = await _roleManager.Roles
                .OrderBy(r => r.Name)
                .Select(r => r.Name!)
                .ToListAsync();

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

            try
            {
                // پاکسازی وابستگی‌های اختیاری که با حذف کاربر conflict ایجاد می‌کنند
                var assignedTasks = await _dbContext.TaskItems
                    .Where(t => t.AssignedUserId == user.Id)
                    .ToListAsync();
                if (assignedTasks.Count > 0)
                {
                    foreach (var task in assignedTasks)
                    {
                        task.AssignedUserId = null;
                    }
                }

                // در بعضی دیتابیس‌های قدیمی ستون shadow با نام UserId1 وجود دارد.
                // خطای این بخش نباید حذف کاربر را متوقف کند.
                try
                {
                    await _dbContext.Database.ExecuteSqlInterpolatedAsync($@"
                        IF OBJECT_ID('Notifications', 'U') IS NOT NULL AND COL_LENGTH('Notifications', 'UserId1') IS NOT NULL
                        BEGIN
                            UPDATE [Notifications]
                            SET [UserId1] = NULL
                            WHERE [UserId1] = {user.Id}
                        END
                    ");
                }
                catch (Exception cleanupEx)
                {
                    _logger.LogWarning(cleanupEx, "Optional cleanup for Notifications.UserId1 failed for user {UserId}", user.Id);
                }

                if (assignedTasks.Count > 0)
                {
                    await _dbContext.SaveChangesAsync();
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
            catch (DbUpdateException ex)
            {
                _logger.LogWarning(ex, "Delete user {UserId} failed due to related data", user.Id);

                if (user.IsActive)
                {
                    user.IsActive = false;
                    var deactivateResult = await _userManager.UpdateAsync(user);
                    if (deactivateResult.Succeeded)
                    {
                        return Json(new
                        {
                            success = true,
                            message = "کاربر به دلیل داشتن اطلاعات وابسته حذف نشد، اما غیرفعال شد"
                        });
                    }
                }

                return Json(new
                {
                    success = false,
                    message = "به دلیل وجود اطلاعات وابسته، حذف کاربر امکان‌پذیر نیست"
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error while deleting user {UserId}", user.Id);
                var rootMessage = ex.GetBaseException().Message;
                return Json(new
                {
                    success = false,
                    message = $"خطای غیرمنتظره در حذف کاربر رخ داد: {rootMessage}"
                });
            }
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

        // POST: افزودن نقش به کاربر
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddRole(string id, string roleName)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(roleName))
            {
                return Json(new { success = false, message = "شناسه کاربر یا نقش نامعتبر است" });
            }

            roleName = roleName.Trim();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "کاربر یافت نشد" });
            }

            if (!await _roleManager.RoleExistsAsync(roleName))
            {
                return Json(new { success = false, message = "نقش انتخاب شده وجود ندارد" });
            }

            if (await _userManager.IsInRoleAsync(user, roleName))
            {
                return Json(new { success = false, message = "این نقش قبلا برای کاربر ثبت شده است" });
            }

            var result = await _userManager.AddToRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                return Json(new { success = true, message = "نقش با موفقیت افزوده شد" });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = $"خطا در افزودن نقش: {errors}" });
        }

        // POST: حذف نقش از کاربر
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveRole(string id, string roleName)
        {
            if (string.IsNullOrWhiteSpace(id) || string.IsNullOrWhiteSpace(roleName))
            {
                return Json(new { success = false, message = "شناسه کاربر یا نقش نامعتبر است" });
            }

            roleName = roleName.Trim();
            var user = await _userManager.FindByIdAsync(id);
            if (user == null)
            {
                return Json(new { success = false, message = "کاربر یافت نشد" });
            }

            if (!await _userManager.IsInRoleAsync(user, roleName))
            {
                return Json(new { success = false, message = "کاربر این نقش را ندارد" });
            }

            var result = await _userManager.RemoveFromRoleAsync(user, roleName);
            if (result.Succeeded)
            {
                return Json(new { success = true, message = "نقش با موفقیت حذف شد" });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = $"خطا در حذف نقش: {errors}" });
        }

        // POST: تعریف نقش جدید
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateRole(string roleName)
        {
            if (string.IsNullOrWhiteSpace(roleName))
            {
                return Json(new { success = false, message = "نام نقش نمی‌تواند خالی باشد" });
            }

            roleName = roleName.Trim();
            if (await _roleManager.RoleExistsAsync(roleName))
            {
                return Json(new { success = false, message = "این نقش قبلا تعریف شده است" });
            }

            var result = await _roleManager.CreateAsync(new IdentityRole(roleName));
            if (result.Succeeded)
            {
                return Json(new { success = true, message = "نقش جدید با موفقیت ایجاد شد" });
            }

            var errors = string.Join(", ", result.Errors.Select(e => e.Description));
            return Json(new { success = false, message = $"خطا در ایجاد نقش: {errors}" });
        }
    }
}

