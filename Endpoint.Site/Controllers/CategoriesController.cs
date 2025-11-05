using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskPlanner.Persistence.Contexts;
using TaskPlanner.Domain.Entities.TaskPlanner;
using System.Security.Claims;
using Endpoint.Site.Models;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]/{id?}")]
    public class CategoriesController : Controller
    {
        private readonly MVPTestDatabaseContext _context;

        public CategoriesController(MVPTestDatabaseContext context)
        {
            _context = context;
        }

        // GET: لیست دسته‌بندی‌ها - دیگر استفاده نمی‌شود، فقط redirect به پروژه‌ها
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Projects");
        }

        // GET: Create
        public async Task<IActionResult> Create(int? projectId)
        {
            if (!projectId.HasValue)
                return RedirectToAction("Index", "Projects");

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // بررسی دسترسی به پروژه
            var hasAccess = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId) ||
                await _context.Projects
                .AnyAsync(p => p.Id == projectId && p.CreatorUserId == userId);
            
            if (!hasAccess)
                return RedirectToAction("Index", "Projects");

            var project = await _context.Projects.FindAsync(projectId.Value);
            ViewBag.ProjectId = projectId.Value;
            ViewBag.ProjectName = project?.Name ?? "پروژه";
            
            return View();
        }

        // POST: Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CategoryCreateVm vm, int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // بررسی دسترسی به پروژه
            var hasAccess = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId) ||
                await _context.Projects
                .AnyAsync(p => p.Id == projectId && p.CreatorUserId == userId);
            
            if (!hasAccess)
                return RedirectToAction("Index", "Projects");

            if (ModelState.IsValid)
            {
                var category = new TaskCategory
                {
                    Name = vm.Name,
                    ProjectId = projectId,
                    CreatorUserId = userId,
                    CreatedAt = DateTime.UtcNow
                };
                
                _context.Add(category);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Projects", new { id = projectId });
            }
            
            var project = await _context.Projects.FindAsync(projectId);
            ViewBag.ProjectId = projectId;
            ViewBag.ProjectName = project?.Name ?? "پروژه";
            return View(vm);
        }

        // GET: Edit
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var category = await _context.TaskCategories
                .Include(c => c.Project)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (category == null) return NotFound();
            
            // بررسی دسترسی
            var hasAccess = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == category.ProjectId && pm.UserId == userId) ||
                await _context.Projects
                .AnyAsync(p => p.Id == category.ProjectId && p.CreatorUserId == userId);
            
            if (!hasAccess)
                return RedirectToAction("Index", "Projects");
            
            var vm = new CategoryEditVm
            {
                Id = category.Id,
                Name = category.Name
            };
            
            ViewBag.ProjectId = category.ProjectId;
            ViewBag.ProjectName = category.Project?.Name ?? "پروژه";
            return View(vm);
        }

        // POST: Edit
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(CategoryEditVm vm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var existingCategory = await _context.TaskCategories
                .Include(c => c.Project)
                .FirstOrDefaultAsync(c => c.Id == vm.Id);
            
            if (existingCategory == null) return NotFound();
            
            // بررسی دسترسی
            var hasAccess = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == existingCategory.ProjectId && pm.UserId == userId) ||
                await _context.Projects
                .AnyAsync(p => p.Id == existingCategory.ProjectId && p.CreatorUserId == userId);
            
            if (!hasAccess)
                return RedirectToAction("Index", "Projects");
            
            if (ModelState.IsValid)
            {
                // به‌روزرسانی فقط Name
                existingCategory.Name = vm.Name;
                
                _context.Update(existingCategory);
                await _context.SaveChangesAsync();
                return RedirectToAction("Details", "Projects", new { id = existingCategory.ProjectId });
            }
            
            ViewBag.ProjectId = existingCategory.ProjectId;
            ViewBag.ProjectName = existingCategory.Project?.Name ?? "پروژه";
            return View(vm);
        }

        // GET: Delete
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var category = await _context.TaskCategories
                .Include(c => c.Project)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (category == null) return NotFound();
            
            // بررسی دسترسی
            var hasAccess = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == category.ProjectId && pm.UserId == userId) ||
                await _context.Projects
                .AnyAsync(p => p.Id == category.ProjectId && p.CreatorUserId == userId);
            
            if (!hasAccess)
                return RedirectToAction("Index", "Projects");
            
            ViewBag.ProjectName = category.Project?.Name ?? "پروژه";
            return View(category);
        }

        // POST: Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var category = await _context.TaskCategories
                .Include(c => c.Project)
                .FirstOrDefaultAsync(c => c.Id == id);
            
            if (category == null) return NotFound();
            
            // بررسی دسترسی
            var hasAccess = await _context.ProjectMembers
                .AnyAsync(pm => pm.ProjectId == category.ProjectId && pm.UserId == userId) ||
                await _context.Projects
                .AnyAsync(p => p.Id == category.ProjectId && p.CreatorUserId == userId);
            
            if (!hasAccess)
                return RedirectToAction("Index", "Projects");
            
            var projectId = category.ProjectId;
            _context.TaskCategories.Remove(category);
            await _context.SaveChangesAsync();
            
            return RedirectToAction("Details", "Projects", new { id = projectId });
        }
    }
}
