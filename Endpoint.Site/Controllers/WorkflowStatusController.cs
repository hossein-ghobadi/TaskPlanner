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

            // در اسپرینت تکمیل‌شده امکان ایجاد وضعیت نیست
            if (sprint.Status == SprintStatus.Completed)
            {
                TempData["Error"] = "در اسپرینت تکمیل‌شده نمی‌توان وضعیت جدید ایجاد کرد.";
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

                // فقط زمانی که اسپرینت تکمیل نشده باشد می‌توان وضعیت ایجاد کرد
                if (sprint.Status == SprintStatus.Completed)
                {
                    TempData["Error"] = "در اسپرینت تکمیل‌شده امکان ایجاد وضعیت جدید وجود ندارد.";
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
        [HttpGet("{id}")]
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

            // فقط اسپرینت‌های تکمیل‌نشده قابل ویرایش هستند
            if (status.Sprint.Status == SprintStatus.Completed)
            {
                TempData["Error"] = "اسپرینت تکمیل‌شده قابل ویرایش نیست.";
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
        [HttpPost("{id}")]
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

                // فقط اسپرینت‌های تکمیل‌نشده قابل ویرایش هستند
                if (status.Sprint.Status == SprintStatus.Completed)
                {
                    TempData["Error"] = "اسپرینت تکمیل‌شده قابل ویرایش نیست.";
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

            // در حالت تکمیل‌شده امکان حذف نیست
            if (status.Sprint.Status == SprintStatus.Completed)
            {
                TempData["Error"] = "در اسپرینت تکمیل‌شده امکان حذف وضعیت وجود ندارد.";
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
                task.SprintId = null;
            }

            _context.WorkflowStatuses.Remove(status);
            await _context.SaveChangesAsync();

            TempData["Success"] = "وضعیت با موفقیت حذف شد";
            return RedirectToAction(nameof(Index), new { sprintId });
        }

        // ایجاد وضعیت جدید به‌صورت AJAX در صفحه برد (اسپرینت یا تخته پروژه بدون اسپرینت)
        [HttpPost]
        public async Task<IActionResult> CreateInline([FromBody] InlineStatusCreateDto dto)
        {
            if (dto == null || string.IsNullOrWhiteSpace(dto.Name))
            {
                return BadRequest(new { success = false, message = "اطلاعات وضعیت نامعتبر است" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (dto.SprintId > 0)
            {
                var sprint = await _context.Sprints
                    .Include(s => s.Project)
                    .FirstOrDefaultAsync(s => s.Id == dto.SprintId);

                if (sprint == null)
                {
                    return NotFound(new { success = false, message = "اسپرینت یافت نشد" });
                }

                var hasAccess = await _context.Projects
                    .AnyAsync(p => p.Id == sprint.ProjectId &&
                                   (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

                if (!hasAccess)
                {
                    return Forbid();
                }

                if (sprint.Status == SprintStatus.Completed)
                {
                    return BadRequest(new { success = false, message = "اسپرینت تکمیل‌شده قابل تغییر نیست" });
                }

                var sprintStatuses = await _context.WorkflowStatuses
                    .Where(s => s.SprintId == sprint.Id)
                    .OrderBy(s => s.Order)
                    .ToListAsync();

                var sprintStatus = new WorkflowStatus
                {
                    Name = dto.Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                    Color = string.IsNullOrWhiteSpace(dto.Color) ? "#0d6efd" : dto.Color.Trim(),
                    Order = sprintStatuses.Any() ? sprintStatuses.Max(s => s.Order) + 1 : 1,
                    ProjectId = sprint.ProjectId,
                    SprintId = sprint.Id,
                    Type = WorkflowType.InProgress,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.WorkflowStatuses.Add(sprintStatus);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "وضعیت جدید با موفقیت ایجاد شد",
                    status = new
                    {
                        sprintStatus.Id,
                        sprintStatus.Name,
                        sprintStatus.Color,
                        sprintStatus.Order
                    }
                });
            }

            if (dto.ProjectId > 0)
            {
                var projectExists = await _context.Projects.AnyAsync(p => p.Id == dto.ProjectId);
                if (!projectExists)
                {
                    return NotFound(new { success = false, message = "پروژه یافت نشد" });
                }

                var hasProjectAccess = await _context.Projects
                    .AnyAsync(p => p.Id == dto.ProjectId &&
                                   (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

                if (!hasProjectAccess)
                {
                    return Forbid();
                }

                var projectStatuses = await _context.WorkflowStatuses
                    .Where(s => s.ProjectId == dto.ProjectId && s.SprintId == null)
                    .OrderBy(s => s.Order)
                    .ToListAsync();

                var projectStatus = new WorkflowStatus
                {
                    Name = dto.Name.Trim(),
                    Description = string.IsNullOrWhiteSpace(dto.Description) ? null : dto.Description.Trim(),
                    Color = string.IsNullOrWhiteSpace(dto.Color) ? "#0d6efd" : dto.Color.Trim(),
                    Order = projectStatuses.Any() ? projectStatuses.Max(s => s.Order) + 1 : 1,
                    ProjectId = dto.ProjectId,
                    SprintId = null,
                    Type = WorkflowType.InProgress,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                _context.WorkflowStatuses.Add(projectStatus);
                await _context.SaveChangesAsync();

                return Ok(new
                {
                    success = true,
                    message = "وضعیت جدید با موفقیت ایجاد شد",
                    status = new
                    {
                        projectStatus.Id,
                        projectStatus.Name,
                        projectStatus.Color,
                        projectStatus.Order
                    }
                });
            }

            return BadRequest(new { success = false, message = "اطلاعات وضعیت نامعتبر است" });
        }

        // حذف وضعیت به‌صورت AJAX (به‌جز شروع و پایان)
        [HttpPost]
        public async Task<IActionResult> DeleteInline([FromBody] InlineStatusDeleteDto dto)
        {
            if (dto == null || dto.StatusId <= 0)
            {
                return BadRequest(new { success = false, message = "شناسه وضعیت نامعتبر است" });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var status = await _context.WorkflowStatuses
                .Include(s => s.Sprint)
                .FirstOrDefaultAsync(s => s.Id == dto.StatusId);

            if (status == null)
            {
                return NotFound(new { success = false, message = "وضعیت یافت نشد" });
            }

            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == status.ProjectId &&
                               (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Forbid();
            }

            if (status.IsDefault || status.IsFinal || status.Type == WorkflowType.Todo || status.Type == WorkflowType.Done)
            {
                return BadRequest(new { success = false, message = "وضعیت‌های شروع و پایان قابل حذف نیستند" });
            }

            // تخته پروژه: وضعیت‌های سطح پروژه (بدون اسپرینت)
            if (!status.SprintId.HasValue)
            {
                var remainingProjectStatuses = await _context.WorkflowStatuses
                    .Where(s => s.ProjectId == status.ProjectId && s.SprintId == null && s.Id != status.Id)
                    .OrderBy(s => s.Order)
                    .ToListAsync();

                if (!remainingProjectStatuses.Any())
                {
                    return BadRequest(new { success = false, message = "حداقل باید یک وضعیت در تخته باقی بماند" });
                }

                var defaultProjectStatus = remainingProjectStatuses.FirstOrDefault(s => s.IsDefault) ?? remainingProjectStatuses.First();

                await using var transactionProject = await _context.Database.BeginTransactionAsync();
                try
                {
                    var projectTasks = await _context.TaskItems
                        .Where(t =>
                            t.ProjectId == status.ProjectId &&
                            t.SprintId == null &&
                            (t.WorkflowStatusId == status.Id || t.StatusId == status.Id))
                        .ToListAsync();

                    foreach (var task in projectTasks)
                    {
                        task.WorkflowStatusId = defaultProjectStatus.Id;
                        task.StatusId = defaultProjectStatus.Id;
                    }

                    if (projectTasks.Any())
                    {
                        _context.TaskItems.UpdateRange(projectTasks);
                        await _context.SaveChangesAsync();
                    }

                    var historiesProject = await _context.IssueStatusHistories
                        .Where(h => h.FromStatusId == status.Id || h.ToStatusId == status.Id)
                        .ToListAsync();

                    if (historiesProject.Any())
                    {
                        _context.IssueStatusHistories.RemoveRange(historiesProject);
                    }

                    _context.WorkflowStatuses.Remove(status);
                    await _context.SaveChangesAsync();
                    await transactionProject.CommitAsync();

                    var msgProject = projectTasks.Any()
                        ? "وضعیت حذف شد و کارها به وضعیت شروع منتقل شدند."
                        : "وضعیت با موفقیت حذف شد.";

                    return Ok(new { success = true, message = msgProject });
                }
                catch (Exception ex)
                {
                    await transactionProject.RollbackAsync();
                    return StatusCode(500, new { success = false, message = "خطا در حذف وضعیت: " + ex.Message });
                }
            }

            if (status.Sprint == null || status.Sprint.Status == SprintStatus.Completed)
            {
                return BadRequest(new { success = false, message = "اسپرینت تکمیل‌شده قابل تغییر نیست" });
            }

            var sprintId = status.SprintId!.Value;

            var remainingStatuses = await _context.WorkflowStatuses
                .Where(s => s.SprintId == sprintId && s.Id != status.Id)
                .OrderBy(s => s.Order)
                .ToListAsync();

            if (!remainingStatuses.Any())
            {
                return BadRequest(new { success = false, message = "حداقل باید یک وضعیت در اسپرینت باقی بماند" });
            }

            var defaultStatus = remainingStatuses.FirstOrDefault(s => s.IsDefault) ?? remainingStatuses.First();

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var affectedTasks = await _context.TaskItems
                    .Include(t => t.SprintTasks)
                    .Where(t =>
                        t.SprintId == sprintId &&
                        (t.WorkflowStatusId == status.Id || t.StatusId == status.Id))
                    .ToListAsync();

                if (affectedTasks.Any())
                {
                    // خارج کردن تسک‌ها از اسپرینت و حذف ارتباط آن‌ها با SprintTasks
                    var taskIds = affectedTasks.Select(t => t.Id).ToList();

                    var sprintTasks = await _context.SprintTasks
                        .Where(st => st.SprintId == sprintId && taskIds.Contains(st.TaskId))
                        .ToListAsync();

                    _context.SprintTasks.RemoveRange(sprintTasks);

                    foreach (var task in affectedTasks)
                    {
                        task.SprintId = null;
                        task.WorkflowStatusId = null;
                    }
                    _context.TaskItems.UpdateRange(affectedTasks);
                    await _context.SaveChangesAsync();

                }

                // حذف تاریخچه‌هایی که به این وضعیت اشاره دارند تا محدودیت FK نقض نشود
                var histories = await _context.IssueStatusHistories
                    .Where(h => h.FromStatusId == status.Id || h.ToStatusId == status.Id)
                    .ToListAsync();

                if (histories.Any())
                {
                    _context.IssueStatusHistories.RemoveRange(histories);
                }

                _context.WorkflowStatuses.Remove(status);
                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var responseMessage = affectedTasks.Any()
                    ? "وضعیت حذف شد و تسک‌های مرتبط از اسپرینت خارج شدند."
                    : "وضعیت با موفقیت حذف شد.";

                return Ok(new { success = true, message = responseMessage });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new { success = false, message = "خطا در حذف وضعیت: " + ex.Message });
            }
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

            var hasProjectScope = dto.ProjectId.HasValue && dto.ProjectId.Value > 0;
            var hasSprintScope = dto.SprintId.HasValue && dto.SprintId.Value > 0;

            if (!hasProjectScope && !hasSprintScope)
            {
                return BadRequest(new { success = false, message = "اسپرینت یا پروژه مشخص نشده است" });
            }

            if (hasProjectScope)
            {
                var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == dto.ProjectId!.Value);
                if (project == null)
                {
                    return NotFound(new { success = false, message = "پروژه یافت نشد" });
                }

                var hasAccessProject = await _context.Projects
                    .AnyAsync(p => p.Id == dto.ProjectId!.Value &&
                                  (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

                if (!hasAccessProject)
                {
                    return Forbid();
                }

                var statusIdsProject = dto.StatusOrders.Select(s => s.StatusId).ToList();
                var statusesProject = await _context.WorkflowStatuses
                    .Where(s => s.ProjectId == dto.ProjectId!.Value && s.SprintId == null)
                    .ToListAsync();

                if (statusesProject.Count != statusIdsProject.Count || dto.StatusOrders.Count != statusesProject.Count)
                {
                    return BadRequest(new { success = false, message = "لیست وضعیت‌ها کامل نیست" });
                }

                var ordersProject = dto.StatusOrders.ToDictionary(s => s.StatusId, s => s.Order);

                foreach (var ws in statusesProject)
                {
                    if (!ordersProject.TryGetValue(ws.Id, out var newOrder))
                    {
                        return BadRequest(new { success = false, message = "وضعیت نامعتبر در لیست مشاهده شد" });
                    }

                    ws.Order = newOrder;
                    ws.UpdatedAt = DateTime.UtcNow;
                }

                await _context.SaveChangesAsync();

                return Ok(new { success = true, message = "ترتیب وضعیت‌ها با موفقیت به‌روزرسانی شد" });
            }

            var sprint = await _context.Sprints
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == dto.SprintId!.Value);

            if (sprint == null)
            {
                return NotFound(new { success = false, message = "اسپرینت یافت نشد" });
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == sprint.ProjectId && 
                              (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Forbid();
            }

            // فقط اسپرینت‌های تکمیل‌نشده امکان تغییر ترتیب دارند
            if (sprint.Status == SprintStatus.Completed)
            {
                return BadRequest(new { success = false, message = "اسپرینت تکمیل‌شده قابل ویرایش نیست" });
            }

            // بررسی اینکه همه وضعیت‌ها به همان اسپرینت تعلق دارند
            var statusIds = dto.StatusOrders.Select(s => s.StatusId).ToList();
            var statuses = await _context.WorkflowStatuses
                .Where(s => s.SprintId == dto.SprintId!.Value)
                .ToListAsync();

            var statusesCount = statuses.Count;
            var statusIdsCount = statusIds.Count;
            var statusOrdersCount = dto.StatusOrders.Count;

            if (statusesCount != statusIdsCount || statusOrdersCount != statusesCount)
            {
                return BadRequest(new { success = false, message = "لیست وضعیت‌ها کامل نیست" });
            }

            var ordersDictionary = dto.StatusOrders.ToDictionary(s => s.StatusId, s => s.Order);

            foreach (var ws in statuses)
            {
                if (!ordersDictionary.TryGetValue(ws.Id, out var newOrder))
                {
                    return BadRequest(new { success = false, message = "وضعیت نامعتبر در لیست مشاهده شد" });
                }

                ws.Order = newOrder;
                ws.UpdatedAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "ترتیب وضعیت‌ها با موفقیت به‌روزرسانی شد" });
        }
    }

    public class UpdateStatusOrderDto
    {
        public int? SprintId { get; set; }
        /// <summary>تخته پروژه (وضعیت‌های بدون اسپرینت)</summary>
        public int? ProjectId { get; set; }
        public List<WorkflowStatusOrderItem> StatusOrders { get; set; } = new();
    }

    public class WorkflowStatusOrderItem
    {
        public int StatusId { get; set; }
        public int Order { get; set; }
    }

    public class InlineStatusCreateDto
    {
        public int SprintId { get; set; }
        /// <summary>برای تخته پروژه وقتی SprintId صفر است</summary>
        public int ProjectId { get; set; }
        public string? Name { get; set; }
        public string? Description { get; set; }
        public string? Color { get; set; }
    }

    public class InlineStatusDeleteDto
    {
        public int StatusId { get; set; }
    }
}




