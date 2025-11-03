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
        [HttpGet]
        // GET: لیست وضعیت‌های workflow - می‌تواند sprintId یا projectId بگیرد
        public async Task<IActionResult> Index(int? sprintId, int? projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // اگر projectId داده شده، باید اسپرینت‌های آن را نشان دهیم
            if (projectId.HasValue && !sprintId.HasValue)
            {
                var project = await _context.Projects.FindAsync(projectId.Value);
                if (project == null)
                {
                    return NotFound();
                }

                // بررسی دسترسی
                var hasAccess = await _context.Projects
                    .AnyAsync(p => p.Id == projectId.Value && 
                                  (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

                if (!hasAccess)
                {
                    return Forbid();
                }

                // دریافت اسپرینت‌های این پروژه
                var sprints = await _context.Sprints
                    .Where(s => s.ProjectId == projectId.Value)
                    .OrderByDescending(s => s.CreatedAt)
                    .ToListAsync();

                ViewBag.ProjectId = projectId.Value;
                ViewBag.ProjectName = project.Name;

                return View("SelectSprint", sprints);
            }

            // اگر sprintId داده شده (یا هردو)
            if (!sprintId.HasValue)
            {
                return BadRequest("لطفاً sprintId یا projectId را مشخص کنید.");
            }

            var sprint = await _context.Sprints
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == sprintId.Value);

            if (sprint == null)
            {
                return NotFound();
            }

            // بررسی دسترسی کاربر به پروژه
            var hasAccess2 = await _context.Projects
                .AnyAsync(p => p.Id == sprint.ProjectId && 
                              (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess2)
            {
                return Forbid();
            }

            ViewBag.SprintId = sprintId.Value;
            ViewBag.SprintName = sprint.Name;
            ViewBag.SprintStatus = sprint.Status;
            ViewBag.ProjectId = sprint.ProjectId;
            ViewBag.ProjectName = sprint.Project.Name;
            ViewBag.CanEdit = sprint.Status == SprintStatus.Planning;

            var statuses = await _context.WorkflowStatuses
                .Where(s => s.SprintId == sprintId.Value)
                .OrderBy(s => s.Order)
                .ToListAsync();

            return View(statuses);
        }

        // GET: ایجاد وضعیت جدید
        public async Task<IActionResult> Create(int sprintId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var sprint = await _context.Sprints
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == sprintId);

            if (sprint == null)
            {
                return NotFound();
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == sprint.ProjectId && 
                              (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Forbid();
            }

            // فقط در حالت Planning می‌توان وضعیت ایجاد کرد
            if (sprint.Status != SprintStatus.Planning)
            {
                TempData["Error"] = "فقط در حالت برنامه‌ریزی می‌توان وضعیت جدید ایجاد کرد. لطفاً ابتدا اسپرینت را به حالت برنامه‌ریزی برگردانید.";
                return RedirectToAction(nameof(Index), new { sprintId });
            }

            ViewBag.SprintName = sprint.Name;
            ViewBag.SprintId = sprintId;
            ViewBag.ProjectName = sprint.Project.Name;
            ViewBag.ProjectId = sprint.ProjectId;

            // محاسبه Order برای نمایش (بدون ذخیره - فقط برای نمایش)
            var existingStatuses = await _context.WorkflowStatuses
                .Where(s => s.SprintId == sprintId)
                .OrderBy(s => s.Order)
                .ToListAsync();

            // اگر Done وجود دارد، Order باید قبل از Done باشد
            var doneStatus = existingStatuses.FirstOrDefault(s => s.Type == WorkflowType.Done && s.IsFinal);
            int newOrder;
            if (doneStatus != null)
            {
                // وضعیت جدید باید یکی قبل از Done باشد (فقط برای نمایش - در POST واقعی ذخیره می‌شود)
                newOrder = doneStatus.Order;
            }
            else
            {
                // اگر Done وجود ندارد، Order را بعد از آخرین وضعیت قرار می‌دهیم
                newOrder = existingStatuses.Any() ? existingStatuses.Max(s => s.Order) + 1 : 2; // 1 برای Todo است
            }

            var model = new WorkflowStatusCreateVm
            {
                SprintId = sprintId,
                ProjectId = sprint.ProjectId,
                Order = newOrder
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

                if (!model.SprintId.HasValue)
                {
                    ModelState.AddModelError("", "اسپرینت مشخص نشده است");
                    return View(model);
                }

                var sprint = await _context.Sprints
                    .Include(s => s.Project)
                    .FirstOrDefaultAsync(s => s.Id == model.SprintId.Value);

                if (sprint == null)
                {
                    return NotFound();
                }

                // بررسی دسترسی
                var hasAccess = await _context.Projects
                    .AnyAsync(p => p.Id == sprint.ProjectId && 
                                  (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

                if (!hasAccess)
                {
                    return Forbid();
                }

                // فقط در حالت Planning می‌توان وضعیت ایجاد کرد
                if (sprint.Status != SprintStatus.Planning)
                {
                    TempData["Error"] = "فقط در حالت برنامه‌ریزی می‌توان وضعیت جدید ایجاد کرد.";
                    return RedirectToAction(nameof(Index), new { sprintId = model.SprintId });
                }

                // محاسبه Order: باید بین Todo (1) و Done (آخرین) قرار بگیرد
                var existingStatuses = await _context.WorkflowStatuses
                    .Where(s => s.SprintId == model.SprintId)
                    .OrderBy(s => s.Order)
                    .ToListAsync();

                var doneStatus = existingStatuses.FirstOrDefault(s => s.Type == WorkflowType.Done && s.IsFinal);
                int newOrder;
                if (doneStatus != null)
                {
                    // وضعیت جدید باید یکی قبل از Done باشد
                    newOrder = doneStatus.Order;
                    // Done را یک واحد افزایش می‌دهیم تا همیشه آخر بماند
                    doneStatus.Order++;
                    // تمام وضعیت‌های دیگر که Order >= newOrder بودند (به جز Done که قبلاً افزایش دادیم) را یک واحد افزایش می‌دهیم
                    foreach (var s in existingStatuses.Where(st => st.Order >= newOrder && st.Id != doneStatus.Id))
                    {
                        s.Order++;
                    }
                    await _context.SaveChangesAsync();
                }
                else
                {
                    // اگر Done وجود ندارد، Order را بعد از آخرین وضعیت قرار می‌دهیم
                    newOrder = existingStatuses.Any() ? existingStatuses.Max(s => s.Order) + 1 : 2; // 1 برای Todo است
                }

                var status = new WorkflowStatus
                {
                    Name = model.Name,
                    Description = model.Description,
                    Color = model.Color,
                    Order = newOrder,
                    ProjectId = sprint.ProjectId,
                    SprintId = model.SprintId,
                    Type = WorkflowType.InProgress, // به صورت پیش‌فرض InProgress
                    CreatedAt = DateTime.UtcNow
                };

                _context.WorkflowStatuses.Add(status);
                await _context.SaveChangesAsync();

                TempData["Success"] = "وضعیت جدید با موفقیت ایجاد شد";
                return RedirectToAction(nameof(Index), new { sprintId = model.SprintId });
            }

            return View(model);
        }

        // GET: ویرایش وضعیت
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var status = await _context.WorkflowStatuses
                .Include(s => s.Sprint)
                .ThenInclude(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (status == null || !status.SprintId.HasValue)
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

            // فقط در حالت Planning می‌توان وضعیت ویرایش کرد
            if (status.Sprint.Status != SprintStatus.Planning)
            {
                TempData["Error"] = "فقط در حالت برنامه‌ریزی می‌توان وضعیت را ویرایش کرد.";
                return RedirectToAction(nameof(Index), new { sprintId = status.SprintId });
            }

            ViewBag.SprintName = status.Sprint.Name;
            ViewBag.ProjectName = status.Sprint.Project.Name;
            ViewBag.SprintId = status.SprintId;

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
                    .Include(s => s.Sprint)
                    .FirstOrDefaultAsync(s => s.Id == id);

                if (status == null || !status.SprintId.HasValue)
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

                // فقط در حالت Planning می‌توان وضعیت ویرایش کرد
                if (status.Sprint.Status != SprintStatus.Planning)
                {
                    TempData["Error"] = "فقط در حالت برنامه‌ریزی می‌توان وضعیت را ویرایش کرد.";
                    return RedirectToAction(nameof(Index), new { sprintId = status.SprintId });
                }

                // جلوگیری از ویرایش وضعیت‌های پیش‌فرض
                if (status.IsDefault || status.IsFinal || status.Type == WorkflowType.Todo || status.Type == WorkflowType.Done)
                {
                    TempData["Error"] = "نمی‌توان وضعیت‌های پیش‌فرض (باید انجام شود و انجام شده) را ویرایش کرد.";
                    return RedirectToAction(nameof(Index), new { sprintId = status.SprintId });
                }

                status.Name = model.Name;
                status.Description = model.Description;
                status.Color = model.Color;
                status.Order = model.Order;
                // Type را تغییر نمی‌دهیم (همیشه همان نوع قبلی می‌ماند)
                status.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                TempData["Success"] = "وضعیت با موفقیت ویرایش شد";
                return RedirectToAction(nameof(Index), new { sprintId = status.SprintId });
            }

            return View(model);
        }

        // GET: حذف وضعیت
        [HttpGet("{id}")] 
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var status = await _context.WorkflowStatuses
                .Include(s => s.Sprint)
                .ThenInclude(s => s.Project)
                .Include(s => s.Tasks)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (status == null || !status.SprintId.HasValue)
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

            ViewBag.SprintId = status.SprintId;
            ViewBag.SprintName = status.Sprint.Name;

            return View(status);
        }

        // POST: حذف وضعیت
        [HttpPost("{id}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var status = await _context.WorkflowStatuses
                .Include(s => s.Sprint)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (status == null || !status.SprintId.HasValue)
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

            // فقط در حالت Planning می‌توان وضعیت حذف کرد
            if (status.Sprint.Status != SprintStatus.Planning)
            {
                TempData["Error"] = "فقط در حالت برنامه‌ریزی می‌توان وضعیت را حذف کرد.";
                return RedirectToAction(nameof(Index), new { sprintId = status.SprintId });
            }

            var sprintId = status.SprintId.Value;

            // جلوگیری از حذف وضعیت‌های پیش‌فرض (Todo و Done)
            if (status.IsDefault || status.IsFinal || status.Type == WorkflowType.Todo || status.Type == WorkflowType.Done)
            {
                TempData["Error"] = "نمی‌توان وضعیت‌های پیش‌فرض (باید انجام شود و انجام شده) را حذف کرد.";
                return RedirectToAction(nameof(Index), new { sprintId });
            }

            // بررسی اینکه حداقل یک وضعیت IsDefault و یک وضعیت IsFinal باقی بماند
            var remainingStatuses = await _context.WorkflowStatuses
                .Where(s => s.SprintId == sprintId && s.Id != id)
                .ToListAsync();

            var hasDefault = remainingStatuses.Any(s => s.IsDefault);
            var hasFinal = remainingStatuses.Any(s => s.IsFinal);

            if (!hasDefault)
            {
                TempData["Error"] = "نمی‌توان این وضعیت را حذف کرد. باید حداقل یک وضعیت شروع (پیش‌فرض) در اسپرینت وجود داشته باشد.";
                return RedirectToAction(nameof(Index), new { sprintId });
            }

            if (!hasFinal)
            {
                TempData["Error"] = "نمی‌توان این وضعیت را حذف کرد. باید حداقل یک وضعیت پایان در اسپرینت وجود داشته باشد.";
                return RedirectToAction(nameof(Index), new { sprintId });
            }

            // حذف ارجاع تسک‌ها به این وضعیت (فقط تسک‌های همان اسپرینت)
            var tasks = await _context.TaskItems
                .Where(t => (t.WorkflowStatusId == id || t.StatusId == id) && t.SprintId == sprintId)
                .ToListAsync();

            var defaultStatus = remainingStatuses.FirstOrDefault(s => s.IsDefault);
            foreach (var task in tasks)
            {
                task.WorkflowStatusId = defaultStatus?.Id;
                task.StatusId = defaultStatus?.Id;
            }

            _context.WorkflowStatuses.Remove(status);
            await _context.SaveChangesAsync();

            TempData["Success"] = "وضعیت با موفقیت حذف شد";
            return RedirectToAction(nameof(Index), new { sprintId });
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

        // ایجاد وضعیت‌های پیش‌فرض برای اسپرینت
        [HttpPost]
        public async Task<IActionResult> CreateDefaultStatuses(int sprintId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var sprint = await _context.Sprints
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == sprintId);

            if (sprint == null)
            {
                return NotFound();
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == sprint.ProjectId && 
                              (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Forbid();
            }

            // فقط در حالت Planning می‌توان وضعیت ایجاد کرد
            if (sprint.Status != SprintStatus.Planning)
            {
                TempData["Error"] = "فقط در حالت برنامه‌ریزی می‌توان وضعیت‌های پیش‌فرض ایجاد کرد.";
                return RedirectToAction(nameof(Index), new { sprintId });
            }

            // بررسی اینکه آیا قبلاً وضعیتی برای این اسپرینت ایجاد شده یا نه
            var existingStatuses = await _context.WorkflowStatuses
                .AnyAsync(s => s.SprintId == sprintId);

            if (existingStatuses)
            {
                TempData["Warning"] = "وضعیت‌هایی برای این اسپرینت از قبل وجود دارد";
                return RedirectToAction(nameof(Index), new { sprintId });
            }

            // ایجاد وضعیت‌های پیش‌فرض: فقط Todo و Done
            var defaultStatuses = new[]
            {
                new WorkflowStatus { Name = "باید انجام شود", Type = WorkflowType.Todo, Order = 1, Color = "#6c757d", ProjectId = sprint.ProjectId, SprintId = sprintId, IsDefault = true },
                new WorkflowStatus { Name = "انجام شده", Type = WorkflowType.Done, Order = 2, Color = "#198754", ProjectId = sprint.ProjectId, SprintId = sprintId, IsFinal = true }
            };

            _context.WorkflowStatuses.AddRange(defaultStatuses);
            await _context.SaveChangesAsync();

            TempData["Success"] = "وضعیت‌های پیش‌فرض با موفقیت ایجاد شدند";
            return RedirectToAction(nameof(Index), new { sprintId });
        }

        // به‌روزرسانی ترتیب وضعیت‌ها
        [HttpPost]
        public async Task<IActionResult> UpdateOrder([FromBody] UpdateStatusOrderDto dto)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (dto.StatusOrders == null || !dto.StatusOrders.Any())
            {
                return BadRequest(new { success = false, message = "لیست وضعیت‌ها خالی است" });
            }

            if (!dto.SprintId.HasValue)
            {
                return BadRequest(new { success = false, message = "اسپرینت مشخص نشده است" });
            }

            var sprint = await _context.Sprints
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == dto.SprintId.Value);

            if (sprint == null)
            {
                return NotFound();
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == sprint.ProjectId && 
                              (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Forbid();
            }

            // فقط در حالت Planning می‌توان ترتیب را تغییر داد
            if (sprint.Status != SprintStatus.Planning)
            {
                return BadRequest(new { success = false, message = "فقط در حالت برنامه‌ریزی می‌توان ترتیب وضعیت‌ها را تغییر داد" });
            }

            // بررسی اینکه همه وضعیت‌ها به همان اسپرینت تعلق دارند
            var statusIds = dto.StatusOrders.Select(s => s.StatusId).ToList();
            var statuses = await _context.WorkflowStatuses
                .Where(s => statusIds.Contains(s.Id) && s.SprintId == dto.SprintId.Value)
                .ToListAsync();

            if (statuses.Count != statusIds.Count)
            {
                return BadRequest(new { success = false, message = "برخی وضعیت‌ها یافت نشدند" });
            }

            // یافتن وضعیت Todo (اول) و Done (آخر)
            var todoStatus = statuses.FirstOrDefault(s => s.Type == WorkflowType.Todo && s.IsDefault);
            var doneStatus = statuses.FirstOrDefault(s => s.Type == WorkflowType.Done && s.IsFinal);

            // بررسی اینکه Todo همیشه Order=1 و Done همیشه آخرین Order باشد
            var todoOrderDto = dto.StatusOrders.FirstOrDefault(o => o.StatusId == todoStatus?.Id);
            if (todoOrderDto != null && todoOrderDto.Order != 1)
            {
                return BadRequest(new { success = false, message = "وضعیت 'باید انجام شود' همیشه باید اولین گام باشد" });
            }

            if (doneStatus != null)
            {
                var doneOrderDto = dto.StatusOrders.FirstOrDefault(o => o.StatusId == doneStatus.Id);
                var maxOrder = dto.StatusOrders.Max(o => o.Order);
                if (doneOrderDto != null && doneOrderDto.Order != maxOrder)
                {
                    return BadRequest(new { success = false, message = "وضعیت 'انجام شده' همیشه باید آخرین گام باشد" });
                }
            }

            // به‌روزرسانی ترتیب
            foreach (var orderDto in dto.StatusOrders)
            {
                var status = statuses.FirstOrDefault(s => s.Id == orderDto.StatusId);
                if (status != null)
                {
                    // اطمینان از اینکه Todo همیشه 1 و Done همیشه آخرین است
                    if (status.Type == WorkflowType.Todo && status.IsDefault)
                    {
                        status.Order = 1;
                    }
                    else if (status.Type == WorkflowType.Done && status.IsFinal && doneStatus != null)
                    {
                        status.Order = dto.StatusOrders.Max(o => o.Order);
                    }
                    else
                    {
                        status.Order = orderDto.Order;
                    }
                    status.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "ترتیب وضعیت‌ها با موفقیت به‌روزرسانی شد" });
        }
    }

    public class UpdateStatusOrderDto
    {
        public int? SprintId { get; set; }
        public List<StatusOrderItem> StatusOrders { get; set; } = new();
    }

    public class StatusOrderItem
    {
        public int StatusId { get; set; }
        public int Order { get; set; }
    }
}




