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
    public class ProjectNoteFoldersController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;

        public ProjectNoteFoldersController(MVPTestDatabaseContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 📁 ایجاد پوشه جدید
        [HttpGet]
        public async Task<IActionResult> Create(int projectId, int? parentFolderId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی به پروژه
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == projectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            var vm = new ProjectNoteFolderCreateVm
            {
                ProjectId = projectId,
                ParentFolderId = parentFolderId
            };

            ViewBag.ProjectId = projectId;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectNoteFolderCreateVm vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.ProjectId = vm.ProjectId;
                return View(vm);
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی به پروژه
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == vm.ProjectId && 
                    (p.CreatorUserId == userIdClaim || p.Members.Any(m => m.UserId == userIdClaim)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی اینکه اگر ParentFolderId مشخص شده، متعلق به همان پروژه باشد
            if (vm.ParentFolderId.HasValue)
            {
                var parentFolder = await _context.ProjectNoteFolders
                    .FirstOrDefaultAsync(f => f.Id == vm.ParentFolderId.Value && f.ProjectId == vm.ProjectId);

                if (parentFolder == null)
                {
                    ModelState.AddModelError("ParentFolderId", "پوشه والد یافت نشد یا متعلق به این پروژه نیست.");
                    ViewBag.Folders = await GetFoldersSelectListAsync(vm.ProjectId, userIdClaim, vm.ParentFolderId);
                    ViewBag.ProjectId = vm.ProjectId;
                    return View(vm);
                }

                // بررسی چرخه (پوشه نمی‌تواند والد خودش باشد)
                if (await HasCircularReferenceAsync(vm.ParentFolderId.Value, null, vm.ProjectId))
                {
                    ModelState.AddModelError("ParentFolderId", "نمی‌توانید پوشه را در زیرمجموعه خودش قرار دهید.");
                    ViewBag.Folders = await GetFoldersSelectListAsync(vm.ProjectId, userIdClaim, vm.ParentFolderId);
                    ViewBag.ProjectId = vm.ProjectId;
                    return View(vm);
                }
            }

            var folder = new ProjectNoteFolder
            {
                Name = vm.Name,
                ProjectId = vm.ProjectId,
                ParentFolderId = vm.ParentFolderId,
                CreatorUserId = userIdClaim!,
                Color = vm.Color,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProjectNoteFolders.Add(folder);
            await _context.SaveChangesAsync();

            TempData["Success"] = "پوشه با موفقیت ایجاد شد.";
            return RedirectToAction("Index", "ProjectNotes", new { projectId = vm.ProjectId });
        }

        // 📁 ویرایش پوشه
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var folder = await _context.ProjectNoteFolders
                .Include(f => f.Project)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (folder == null)
            {
                TempData["Error"] = "پوشه یافت نشد.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی دسترسی به پروژه
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == folder.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            var vm = new ProjectNoteFolderEditVm
            {
                Id = folder.Id,
                ProjectId = folder.ProjectId,
                Name = folder.Name,
                ParentFolderId = folder.ParentFolderId,
                Color = folder.Color
            };

            // لیست پوشه‌های موجود برای انتخاب به عنوان والد (به جز خود پوشه)
            ViewBag.Folders = await GetFoldersSelectListAsync(folder.ProjectId, userId, folder.ParentFolderId, id);
            ViewBag.ProjectId = folder.ProjectId;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProjectNoteFolderEditVm vm)
        {
            if (!ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ViewBag.Folders = await GetFoldersSelectListAsync(vm.ProjectId, userId, vm.ParentFolderId, vm.Id);
                ViewBag.ProjectId = vm.ProjectId;
                return View(vm);
            }

            var userIdClaim = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var folder = await _context.ProjectNoteFolders
                .FirstOrDefaultAsync(f => f.Id == vm.Id && f.ProjectId == vm.ProjectId);

            if (folder == null)
            {
                TempData["Error"] = "پوشه یافت نشد یا متعلق به این پروژه نیست.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی دسترسی به پروژه
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == vm.ProjectId && 
                    (p.CreatorUserId == userIdClaim || p.Members.Any(m => m.UserId == userIdClaim)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی اینکه اگر ParentFolderId تغییر کرده، متعلق به همان پروژه باشد
            if (vm.ParentFolderId.HasValue && vm.ParentFolderId != folder.ParentFolderId)
            {
                var parentFolder = await _context.ProjectNoteFolders
                    .FirstOrDefaultAsync(f => f.Id == vm.ParentFolderId.Value && f.ProjectId == vm.ProjectId);

                if (parentFolder == null)
                {
                    ModelState.AddModelError("ParentFolderId", "پوشه والد یافت نشد یا متعلق به این پروژه نیست.");
                    ViewBag.Folders = await GetFoldersSelectListAsync(vm.ProjectId, userIdClaim, vm.ParentFolderId, vm.Id);
                    ViewBag.ProjectId = vm.ProjectId;
                    return View(vm);
                }

                // بررسی چرخه (پوشه نمی‌تواند والد خودش باشد)
                if (await HasCircularReferenceAsync(vm.ParentFolderId.Value, vm.Id, vm.ProjectId))
                {
                    ModelState.AddModelError("ParentFolderId", "نمی‌توانید پوشه را در زیرمجموعه خودش قرار دهید.");
                    ViewBag.Folders = await GetFoldersSelectListAsync(vm.ProjectId, userIdClaim, vm.ParentFolderId, vm.Id);
                    ViewBag.ProjectId = vm.ProjectId;
                    return View(vm);
                }
            }

            folder.Name = vm.Name;
            folder.ParentFolderId = vm.ParentFolderId;
            folder.Color = vm.Color;
            folder.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "پوشه با موفقیت ویرایش شد.";
            return RedirectToAction("Index", "ProjectNotes", new { projectId = vm.ProjectId });
        }

        // 📁 حذف پوشه
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var folder = await _context.ProjectNoteFolders
                .Include(f => f.Children)
                .Include(f => f.Notes)
                .Include(f => f.Project)
                .FirstOrDefaultAsync(f => f.Id == id);

            if (folder == null)
            {
                TempData["Error"] = "پوشه یافت نشد.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی دسترسی به پروژه
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == folder.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            return View(folder);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // تمام پوشه‌های این پروژه را می‌گیریم تا بتوانیم زیر درخت را محاسبه کنیم
            var allFolders = await _context.ProjectNoteFolders
                .Where(f => f.ProjectId == _context.ProjectNoteFolders
                    .Where(x => x.Id == id)
                    .Select(x => x.ProjectId)
                    .FirstOrDefault())
                .ToListAsync();

            var rootFolder = allFolders.FirstOrDefault(f => f.Id == id);
            if (rootFolder == null)
            {
                TempData["Error"] = "پوشه یافت نشد.";
                return RedirectToAction("Index", "Projects");
            }

            var projectId = rootFolder.ProjectId;

            // بررسی دسترسی به پروژه
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == projectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
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

            // تمام یادداشت‌های موجود در این پوشه‌ها را بدون پوشه می‌کنیم
            var notesInTree = await _context.ProjectNotes
                .Where(n => n.ProjectId == projectId && n.FolderId.HasValue && folderIdsToDelete.Contains(n.FolderId.Value))
                .ToListAsync();

            foreach (var note in notesInTree)
            {
                note.FolderId = null;
            }

            // تمام پوشه‌های این درخت را حذف می‌کنیم
            var foldersToDelete = allFolders.Where(f => folderIdsToDelete.Contains(f.Id)).ToList();
            _context.ProjectNoteFolders.RemoveRange(foldersToDelete);
            await _context.SaveChangesAsync();

            TempData["Success"] = "پوشه و تمام زیرپوشه‌های آن با موفقیت حذف شدند و یادداشت‌هایشان بدون پوشه شدند.";
            return RedirectToAction("Index", "ProjectNotes", new { projectId });
        }

        // Helper: لیست پوشه‌ها برای SelectList
        private async Task<List<SelectListItem>> GetFoldersSelectListAsync(int projectId, string userId, int? excludeParentId = null, int? excludeFolderId = null)
        {
            var folders = await _context.ProjectNoteFolders
                .Where(f => f.ProjectId == projectId)
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
        private async Task<bool> HasCircularReferenceAsync(int parentFolderId, int? currentFolderId, int projectId)
        {
            if (!currentFolderId.HasValue)
                return false;

            var parentFolder = await _context.ProjectNoteFolders
                .FirstOrDefaultAsync(f => f.Id == parentFolderId && f.ProjectId == projectId);

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
                    parentFolder = await _context.ProjectNoteFolders
                        .FirstOrDefaultAsync(f => f.Id == parentFolder.ParentFolderId.Value && f.ProjectId == projectId);
                }
                else
                {
                    break;
                }
            }

            return false;
        }
    }
}

