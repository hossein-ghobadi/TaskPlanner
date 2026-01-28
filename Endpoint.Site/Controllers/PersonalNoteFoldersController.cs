using Endpoint.Site.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Domain.Entities.Users;
using TaskPlanner.Persistence.Contexts;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class PersonalNoteFoldersController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;

        public PersonalNoteFoldersController(MVPTestDatabaseContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 📁 ایجاد پوشه جدید
        [HttpGet]
        public async Task<IActionResult> Create(int? parentFolderId = null)
        {
            var vm = new PersonalNoteFolderCreateVm
            {
                ParentFolderId = parentFolderId
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PersonalNoteFolderCreateVm vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی اینکه اگر ParentFolderId مشخص شده، متعلق به کاربر باشد
            if (vm.ParentFolderId.HasValue)
            {
                var parentFolder = await _context.PersonalNoteFolders
                    .FirstOrDefaultAsync(f => f.Id == vm.ParentFolderId.Value && f.UserId == userIdClaim);

                if (parentFolder == null)
                {
                    ModelState.AddModelError("ParentFolderId", "پوشه والد یافت نشد یا به آن دسترسی ندارید.");
                    ViewBag.Folders = await GetFoldersSelectListAsync(userIdClaim, vm.ParentFolderId);
                    return View(vm);
                }

                // بررسی چرخه (پوشه نمی‌تواند والد خودش باشد)
                if (await HasCircularReferenceAsync(vm.ParentFolderId.Value, null))
                {
                    ModelState.AddModelError("ParentFolderId", "نمی‌توانید پوشه را در زیرمجموعه خودش قرار دهید.");
                    ViewBag.Folders = await GetFoldersSelectListAsync(userIdClaim, vm.ParentFolderId);
                    return View(vm);
                }
            }

            var folder = new PersonalNoteFolder
            {
                Name = vm.Name,
                ParentFolderId = vm.ParentFolderId,
                UserId = userIdClaim!,
                Color = vm.Color,
                CreatedAt = DateTime.UtcNow
            };

            _context.PersonalNoteFolders.Add(folder);
            await _context.SaveChangesAsync();

            TempData["Success"] = "پوشه با موفقیت ایجاد شد.";
            return RedirectToAction("Index", "PersonalNotes");
        }

        // 📁 ویرایش پوشه
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var folder = await _context.PersonalNoteFolders
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

            if (folder == null)
            {
                TempData["Error"] = "پوشه یافت نشد یا به آن دسترسی ندارید.";
                return RedirectToAction("Index", "PersonalNotes");
            }

            var vm = new PersonalNoteFolderEditVm
            {
                Id = folder.Id,
                Name = folder.Name,
                ParentFolderId = folder.ParentFolderId,
                Color = folder.Color
            };

            // لیست پوشه‌های موجود برای انتخاب به عنوان والد (به جز خود پوشه)
            ViewBag.Folders = await GetFoldersSelectListAsync(userId, folder.ParentFolderId, id);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PersonalNoteFolderEditVm vm)
        {
            if (!ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ViewBag.Folders = await GetFoldersSelectListAsync(userId, vm.ParentFolderId, vm.Id);
                return View(vm);
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var folder = await _context.PersonalNoteFolders
                .FirstOrDefaultAsync(f => f.Id == vm.Id && f.UserId == userIdClaim);

            if (folder == null)
            {
                TempData["Error"] = "پوشه یافت نشد یا به آن دسترسی ندارید.";
                return RedirectToAction("Index", "PersonalNotes");
            }

            // بررسی اینکه اگر ParentFolderId تغییر کرده، متعلق به کاربر باشد
            if (vm.ParentFolderId.HasValue && vm.ParentFolderId != folder.ParentFolderId)
            {
                var parentFolder = await _context.PersonalNoteFolders
                    .FirstOrDefaultAsync(f => f.Id == vm.ParentFolderId.Value && f.UserId == userIdClaim);

                if (parentFolder == null)
                {
                    ModelState.AddModelError("ParentFolderId", "پوشه والد یافت نشد یا به آن دسترسی ندارید.");
                    ViewBag.Folders = await GetFoldersSelectListAsync(userIdClaim, vm.ParentFolderId, vm.Id);
                    return View(vm);
                }

                // بررسی چرخه (پوشه نمی‌تواند والد خودش باشد)
                if (await HasCircularReferenceAsync(vm.ParentFolderId.Value, vm.Id))
                {
                    ModelState.AddModelError("ParentFolderId", "نمی‌توانید پوشه را در زیرمجموعه خودش قرار دهید.");
                    ViewBag.Folders = await GetFoldersSelectListAsync(userIdClaim, vm.ParentFolderId, vm.Id);
                    return View(vm);
                }
            }

            folder.Name = vm.Name;
            folder.ParentFolderId = vm.ParentFolderId;
            folder.Color = vm.Color;
            folder.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "پوشه با موفقیت ویرایش شد.";
            return RedirectToAction("Index", "PersonalNotes");
        }

        // 📁 حذف پوشه
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var folder = await _context.PersonalNoteFolders
                .Include(f => f.Children)
                .Include(f => f.Notes)
                .FirstOrDefaultAsync(f => f.Id == id && f.UserId == userId);

            if (folder == null)
            {
                TempData["Error"] = "پوشه یافت نشد یا به آن دسترسی ندارید.";
                return RedirectToAction("Index", "PersonalNotes");
            }

            // بررسی اینکه پوشه "زباله" قابل حذف نیست
            if (IsTrashFolder(folder))
            {
                TempData["Error"] = "پوشه \"زباله\" قابل حذف نیست.";
                return RedirectToAction("Index", "PersonalNotes");
            }

            return View(folder);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // تمام پوشه‌های کاربر را می‌گیریم تا بتوانیم زیر درخت را محاسبه کنیم
            var allFolders = await _context.PersonalNoteFolders
                .Where(f => f.UserId == userId)
                .ToListAsync();

            var rootFolder = allFolders.FirstOrDefault(f => f.Id == id);
            if (rootFolder == null)
            {
                TempData["Error"] = "پوشه یافت نشد یا به آن دسترسی ندارید.";
                return RedirectToAction("Index", "PersonalNotes");
            }

            // بررسی اینکه پوشه "زباله" قابل حذف نیست
            if (IsTrashFolder(rootFolder))
            {
                TempData["Error"] = "پوشه \"زباله\" قابل حذف نیست.";
                return RedirectToAction("Index", "PersonalNotes");
            }

            // محاسبه تمام پوشه‌های درخت (پوشه و تمام زیرپوشه‌ها)
            var folderIdsToDelete = new HashSet<int>();
            var stack = new Stack<int>();
            stack.Push(rootFolder.Id);
            folderIdsToDelete.Add(rootFolder.Id);

            while (stack.Count > 0)
            {
                var currentId = stack.Pop();
                var children = allFolders
                    .Where(f => f.ParentFolderId == currentId)
                    .Select(f => f.Id);

                foreach (var childId in children)
                {
                    if (folderIdsToDelete.Add(childId))
                    {
                        stack.Push(childId);
                    }
                }
            }

            // دریافت یا ایجاد پوشه "زباله"
            var trashFolder = await GetOrCreateTrashFolderAsync(userId);

            // تمام یادداشت‌های موجود در این پوشه‌ها (شامل پوشه اصلی و تمام زیرپوشه‌ها) را به پوشه "زباله" منتقل می‌کنیم
            var notesInTree = await _context.PersonalNotes
                .Where(n => n.UserId == userId && n.FolderId.HasValue && folderIdsToDelete.Contains(n.FolderId.Value))
                .ToListAsync();

            var notesCount = notesInTree.Count;
            foreach (var note in notesInTree)
            {
                note.FolderId = trashFolder.Id;
                note.UpdatedAt = DateTime.UtcNow; // به‌روزرسانی تاریخ برای نشان دادن تغییر
            }

            // تمام پوشه‌های این درخت را حذف می‌کنیم
            var foldersToDelete = allFolders.Where(f => folderIdsToDelete.Contains(f.Id)).ToList();
            var foldersCount = foldersToDelete.Count;
            
            _context.PersonalNoteFolders.RemoveRange(foldersToDelete);

            await _context.SaveChangesAsync();

            // پیام موفقیت با جزئیات
            var successMessage = $"پوشه و {foldersCount - 1} زیرپوشه با موفقیت حذف شدند";
            if (notesCount > 0)
            {
                successMessage += $" و {notesCount} یادداشت به پوشه \"زباله\" منتقل شد.";
            }
            else
            {
                successMessage += ".";
            }

            TempData["Success"] = successMessage;
            return RedirectToAction("Index", "PersonalNotes");
        }

        // Helper: لیست پوشه‌ها برای SelectList
        private async Task<List<SelectListItem>> GetFoldersSelectListAsync(string userId, int? excludeParentId = null, int? excludeFolderId = null)
        {
            var folders = await _context.PersonalNoteFolders
                .Where(f => f.UserId == userId)
                .OrderBy(f => f.Name)
                .ToListAsync();

            var selectList = new List<SelectListItem>
            {
                new SelectListItem { Text = "بدون پوشه والد", Value = "", Selected = !excludeParentId.HasValue }
            };

            foreach (var folder in folders)
            {
                // پوشه فعلی را از لیست حذف کن
                if (excludeFolderId.HasValue && folder.Id == excludeFolderId.Value)
                    continue;

                // اگر پوشه والد مشخص شده، آن را انتخاب کن
                bool isSelected = excludeParentId.HasValue && folder.Id == excludeParentId.Value;

                selectList.Add(new SelectListItem
                {
                    Text = folder.Name,
                    Value = folder.Id.ToString(),
                    Selected = isSelected
                });
            }

            return selectList;
        }

        // Helper: بررسی چرخه در ساختار پوشه‌ها
        private async Task<bool> HasCircularReferenceAsync(int parentFolderId, int? currentFolderId)
        {
            if (!currentFolderId.HasValue)
                return false;

            var parentFolder = await _context.PersonalNoteFolders
                .FirstOrDefaultAsync(f => f.Id == parentFolderId);

            if (parentFolder == null)
                return false;

            // اگر پوشه والد، خودش یا یکی از اجداد پوشه فعلی باشد، چرخه وجود دارد
            var currentId = currentFolderId.Value;
            var visited = new HashSet<int> { currentId };

            while (parentFolder != null)
            {
                if (visited.Contains(parentFolder.Id))
                    return true;

                visited.Add(parentFolder.Id);

                if (parentFolder.ParentFolderId.HasValue)
                {
                    parentFolder = await _context.PersonalNoteFolders
                        .FirstOrDefaultAsync(f => f.Id == parentFolder.ParentFolderId.Value);
                }
                else
                {
                    break;
                }
            }

            return false;
        }

        // Helper: دریافت یا ایجاد پوشه "زباله"
        private async Task<PersonalNoteFolder> GetOrCreateTrashFolderAsync(string userId)
        {
            const string trashFolderName = "زباله";
            
            var trashFolder = await _context.PersonalNoteFolders
                .FirstOrDefaultAsync(f => f.UserId == userId && f.Name == trashFolderName);

            if (trashFolder == null)
            {
                trashFolder = new PersonalNoteFolder
                {
                    Name = trashFolderName,
                    UserId = userId,
                    Color = "#6c757d", // رنگ خاکستری
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.PersonalNoteFolders.Add(trashFolder);
                await _context.SaveChangesAsync();
            }

            return trashFolder;
        }

        // Helper: بررسی اینکه آیا پوشه "زباله" است
        private bool IsTrashFolder(PersonalNoteFolder folder)
        {
            return folder.Name == "زباله";
        }
    }
}

