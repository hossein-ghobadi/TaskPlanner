using Endpoint.Site.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskPlanner.Persistence.Contexts;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class WorkflowStatusController : Controller
    {
        private readonly MVPTestDatabaseContext _context;

        public WorkflowStatusController(MVPTestDatabaseContext context)
        {
            _context = context;
        }

        // GET: لیست وضعیت‌های workflow یک پروژه
        public async Task<IActionResult> Index(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی کاربر به پروژه
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == projectId && 
                              (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Forbid();
            }

            var project = await _context.Projects.FindAsync(projectId);
            ViewBag.ProjectId = projectId;
            ViewBag.ProjectName = project?.Name;

            var statuses = await _context.WorkflowStatuses
                .Where(s => s.ProjectId == projectId)
                .OrderBy(s => s.Order)
                .ToListAsync();

            return View(statuses);
        }

        // GET: ایجاد وضعیت جدید
        public async Task<IActionResult> Create(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == projectId && 
                              (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Forbid();
            }

            var project = await _context.Projects.FindAsync(projectId);
            ViewBag.ProjectName = project?.Name;

            var model = new WorkflowStatusCreateVm
            {
                ProjectId = projectId,
                Order = await _context.WorkflowStatuses.CountAsync(s => s.ProjectId == projectId) + 1
            };

            return View(model);
        }

        // POST: ایجاد وضعیت جدید
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(WorkflowStatusCreateVm model)
        {
            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                // بررسی دسترسی
                var hasAccess = await _context.Projects
                    .AnyAsync(p => p.Id == model.ProjectId && 
                                  (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

                if (!hasAccess)
                {
                    return Forbid();
                }

                var status = new WorkflowStatus
                {
                    Name = model.Name,
                    Description = model.Description,
                    Color = model.Color,
                    Order = model.Order,
                    ProjectId = model.ProjectId,
                    Type = model.Type,
                    CreatedAt = DateTime.UtcNow
                };

                _context.WorkflowStatuses.Add(status);
                await _context.SaveChangesAsync();

                TempData["Success"] = "وضعیت جدید با موفقیت ایجاد شد";
                return RedirectToAction(nameof(Index), new { projectId = model.ProjectId });
            }

            return View(model);
        }

        // GET: ویرایش وضعیت
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var status = await _context.WorkflowStatuses
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (status == null)
            {
                return NotFound();
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == status.ProjectId && 
                              (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Forbid();
            }

            ViewBag.ProjectName = status.Project.Name;

            var model = new WorkflowStatusEditVm
            {
                Id = status.Id,
                Name = status.Name,
                Description = status.Description,
                Color = status.Color,
                Order = status.Order,
                Type = status.Type
            };

            return View(model);
        }

        // POST: ویرایش وضعیت
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, WorkflowStatusEditVm model)
        {
            if (id != model.Id)
            {
                return NotFound();
            }

            if (ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var status = await _context.WorkflowStatuses
                    .Include(s => s.Project)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (status == null)
                {
                    return NotFound();
                }

                // بررسی دسترسی
                var hasAccess = await _context.Projects
                    .AnyAsync(p => p.Id == status.ProjectId && 
                                  (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

                if (!hasAccess)
                {
                    return Forbid();
                }

                status.Name = model.Name;
                status.Description = model.Description;
                status.Color = model.Color;
                status.Order = model.Order;
                status.Type = model.Type;

                await _context.SaveChangesAsync();

                TempData["Success"] = "وضعیت با موفقیت ویرایش شد";
                return RedirectToAction(nameof(Index), new { projectId = status.ProjectId });
            }

            return View(model);
        }

        // GET: حذف وضعیت
        [HttpGet("{id}")] 
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var status = await _context.WorkflowStatuses
                .Include(s => s.Project)
                .Include(s => s.Tasks)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (status == null)
            {
                return NotFound();
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == status.ProjectId && 
                              (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Forbid();
            }

            return View(status);
        }

        // POST: حذف وضعیت
        [HttpPost("{id}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var status = await _context.WorkflowStatuses
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (status == null)
            {
                return NotFound();
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == status.ProjectId && 
                              (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Forbid();
            }

            var projectId = status.ProjectId;

            // حذف ارجاع تسک‌ها به این وضعیت
            var tasks = await _context.TaskItems
                .Where(t => t.WorkflowStatusId == id)
                .ToListAsync();

            foreach (var task in tasks)
            {
                task.WorkflowStatusId = null;
            }

            _context.WorkflowStatuses.Remove(status);
            await _context.SaveChangesAsync();

            TempData["Success"] = "وضعیت با موفقیت حذف شد";
            return RedirectToAction(nameof(Index), new { projectId });
        }

        // تغییر وضعیت یک تسک
        [HttpPost]
        public async Task<IActionResult> ChangeTaskStatus(int taskId, int? statusId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _context.TaskItems
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
            {
                return NotFound();
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == task.ProjectId && 
                              (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Forbid();
            }

            // اگر statusId داده شده، بررسی می‌کنیم که به همان پروژه تعلق داشته باشد
            if (statusId.HasValue)
            {
                var statusExists = await _context.WorkflowStatuses
                    .AnyAsync(s => s.Id == statusId.Value && s.ProjectId == task.ProjectId);

                if (!statusExists)
                {
                    return BadRequest();
                }
            }

            task.WorkflowStatusId = statusId;
            await _context.SaveChangesAsync();

            return Ok();
        }

        // ایجاد وضعیت‌های پیش‌فرض برای پروژه
        [HttpPost]
        public async Task<IActionResult> CreateDefaultStatuses(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == projectId && 
                              (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Forbid();
            }

            // بررسی اینکه آیا قبلاً وضعیتی برای این پروژه ایجاد شده یا نه
            var existingStatuses = await _context.WorkflowStatuses
                .AnyAsync(s => s.ProjectId == projectId);

            if (existingStatuses)
            {
                TempData["Warning"] = "وضعیت‌هایی برای این پروژه از قبل وجود دارد";
                return RedirectToAction(nameof(Index), new { projectId });
            }

            // ایجاد وضعیت‌های پیش‌فرض
            var defaultStatuses = new[]
            {
                new WorkflowStatus { Name = "باید انجام شود", Type = WorkflowType.Todo, Order = 1, Color = "#6c757d", ProjectId = projectId },
                new WorkflowStatus { Name = "در حال انجام", Type = WorkflowType.InProgress, Order = 2, Color = "#0dcaf0", ProjectId = projectId },
                new WorkflowStatus { Name = "در حال بررسی", Type = WorkflowType.InProgress, Order = 3, Color = "#ffc107", ProjectId = projectId },
                new WorkflowStatus { Name = "انجام شده", Type = WorkflowType.Done, Order = 4, Color = "#198754", ProjectId = projectId }
            };

            _context.WorkflowStatuses.AddRange(defaultStatuses);
            await _context.SaveChangesAsync();

            TempData["Success"] = "وضعیت‌های پیش‌فرض با موفقیت ایجاد شدند";
            return RedirectToAction(nameof(Index), new { projectId });
        }
    }
}




