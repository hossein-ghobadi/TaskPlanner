using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Security.Claims;
using TaskPlanner.Domain.Entities.Users;
using TaskPlanner.Persistence.Contexts;
using Endpoint.Site.Models;
using DNTPersianUtils.Core;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class SprintsController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;

        public SprintsController(MVPTestDatabaseContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // 📌 لیست اسپرینت‌های پروژه
        [HttpGet]
        public async Task<IActionResult> Index(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // Debug: چک کردن userId
            if (string.IsNullOrEmpty(userId))
            {
                TempData["Error"] = "کاربر وارد نشده است.";
                return RedirectToAction("Login", "Account", new { area = "" });
            }

            // بررسی دسترسی به پروژه
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == projectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            var project = await _context.Projects.FindAsync(projectId);
            ViewBag.ProjectName = project?.Name;
            ViewBag.ProjectId = projectId;

            var sprints = await _context.Sprints
                .Where(s => s.ProjectId == projectId)
                .Include(s => s.SprintTasks)
                    .ThenInclude(st => st.Task)
                .OrderByDescending(s => s.CreatedAt)
                .ToListAsync();

            var sprintVms = sprints.Select(s => new SprintVm
            {
                Id = s.Id,
                Name = s.Name,
                Description = s.Description,
                Goal = s.Goal,
                StartDate = s.StartDate,
                EndDate = s.EndDate,
                IsActive = s.IsActive,
                IsCompleted = s.IsCompleted,
                Status = s.Status,
                CreatedAt = s.CreatedAt,
                UpdatedAt = s.UpdatedAt,
                ProjectId = s.ProjectId,
                ProjectName = s.Project.Name,
                Project = s.Project,
                Tasks = s.SprintTasks.Select(st => st.Task).ToList(),
                TodoTasks = s.SprintTasks.Where(st => st.Status != SprintTaskStatus.Completed).Select(st => st.Task).ToList(),
                CompletedAt = s.IsCompleted ? s.UpdatedAt : null,
                TotalTasks = s.SprintTasks.Count,
                CompletedTasks = s.SprintTasks.Count(st => st.Status == SprintTaskStatus.Completed),
                InProgressTasks = s.SprintTasks.Count(st => st.Status == SprintTaskStatus.InProgress),
                PendingTasks = s.SprintTasks.Count(st => st.Status == SprintTaskStatus.Pending),
                BlockedTasks = s.SprintTasks.Count(st => st.Status == SprintTaskStatus.Blocked)
            }).ToList();

            return View(sprintVms);
        }

        // 📌 جزئیات اسپرینت
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var sprint = await _context.Sprints
                .Include(s => s.Project)
                .Include(s => s.SprintTasks)
                    .ThenInclude(st => st.Task)
                        .ThenInclude(t => t.Category)
                .Include(s => s.SprintTasks)
                    .ThenInclude(st => st.Task)
                        .ThenInclude(t => t.AssignedUser)
                .Include(s => s.SprintTasks)
                    .ThenInclude(st => st.Task)
                        .ThenInclude(t => t.WorkflowStatus)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sprint == null)
            {
                TempData["Error"] = "اسپرینت یافت نشد.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == sprint.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این اسپرینت دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            // دریافت وضعیت‌های workflow اسپرینت
            var workflowStatuses = await _context.WorkflowStatuses
                .Where(ws => ws.SprintId == sprint.Id)
                .OrderBy(ws => ws.Order)
                .ToListAsync();

            var sprintDetailsVm = new SprintDetailsVm
            {
                Id = sprint.Id,
                Name = sprint.Name,
                Description = sprint.Description,
                Goal = sprint.Goal,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                IsActive = sprint.IsActive,
                IsCompleted = sprint.IsCompleted,
                Status = sprint.Status,
                CreatedAt = sprint.CreatedAt,
                UpdatedAt = sprint.UpdatedAt,
                ProjectId = sprint.ProjectId,
                ProjectName = sprint.Project.Name,
                Project = sprint.Project,
                Tasks = sprint.SprintTasks.Select(st => st.Task).ToList(),
                SprintTasks = sprint.SprintTasks.ToList(),
                TodoTasks = sprint.SprintTasks.Select(st => st.Task).Where(t => t.StatusId == null || !workflowStatuses.Any(ws => ws.Id == t.StatusId && ws.IsFinal)).ToList(),
                CompletedAt = sprint.IsCompleted ? sprint.UpdatedAt : null,
                TotalTasks = sprint.SprintTasks.Count,
                CompletedTasks = sprint.SprintTasks.Select(st => st.Task).Count(t => t.StatusId.HasValue && workflowStatuses.Any(ws => ws.Id == t.StatusId && ws.IsFinal)),
                InProgressTasks = sprint.SprintTasks.Select(st => st.Task).Count(t => t.StatusId.HasValue && workflowStatuses.Any(ws => ws.Id == t.StatusId && ws.Type == WorkflowType.InProgress)),
                PendingTasks = sprint.SprintTasks.Select(st => st.Task).Count(t => t.StatusId == null || workflowStatuses.Any(ws => ws.Id == t.StatusId && ws.Type == WorkflowType.Todo)),
                BlockedTasks = sprint.SprintTasks.Select(st => st.Task).Count(t => t.StatusId.HasValue && workflowStatuses.Any(ws => ws.Id == t.StatusId && ws.Type == WorkflowType.Blocked))
            };

            ViewBag.WorkflowStatuses = workflowStatuses;

            return View(sprintDetailsVm);
        }

        // 📌 برد (Kanban) اسپرینت مانند Jira
        [HttpGet("{id}")]
        public async Task<IActionResult> Board(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var sprint = await _context.Sprints
                .Include(s => s.Project)
                .Include(s => s.SprintTasks)
                    .ThenInclude(st => st.Task)
                        .ThenInclude(t => t.Status)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sprint == null)
            {
                TempData["Error"] = "اسپرینت یافت نشد.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == sprint.ProjectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));
            if (!hasAccess)
            {
                TempData["Error"] = "شما به این اسپرینت دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            // وضعیت‌های اسپرینت (ستون‌ها)
            var statuses = await _context.WorkflowStatuses
                .Where(ws => ws.SprintId == sprint.Id)
                .OrderBy(ws => ws.Order)
                .ToListAsync();

            var vm = new SprintBoardVm
            {
                SprintId = sprint.Id,
                SprintName = sprint.Name,
                ProjectId = sprint.ProjectId,
                ProjectName = sprint.Project.Name,
                Statuses = statuses,
                SprintIssues = sprint.SprintTasks.Select(st => st.Task).ToList()
            };

            return View(vm);
        }

        // 📅 Sprint Planning: صفحه برنامه‌ریزی اسپرینت (مثل Jira)
        [HttpGet("{id}")]
        public async Task<IActionResult> Planning(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var sprint = await _context.Sprints
                .Include(s => s.Project)
                .Include(s => s.SprintTasks)
                    .ThenInclude(st => st.Task)
                        .ThenInclude(t => t.AssignedUser)
                .Include(s => s.SprintTasks)
                    .ThenInclude(st => st.Task)
                        .ThenInclude(t => t.Status)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sprint == null)
            {
                TempData["Error"] = "اسپرینت یافت نشد.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == sprint.ProjectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));
            if (!hasAccess)
            {
                TempData["Error"] = "شما به این اسپرینت دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            // Issues موجود در این Sprint (مرتب شده بر اساس SprintPriority)
            var sprintIssues = sprint.SprintTasks
                .OrderBy(st => st.SprintPriority)
                .ThenBy(st => st.Task.Priority)
                .Select(st => st.Task)
                .ToList();

            // Issues موجود در Backlog (خارج از تمام Sprint های باز)
            var tasksInOpenSprints = await _context.SprintTasks
                .Where(st => _context.Sprints.Any(s => s.Id == st.SprintId && s.ProjectId == sprint.ProjectId && s.Status != SprintStatus.Completed))
                .Select(st => st.TaskId)
                .Distinct()
                .ToListAsync();

            var backlogIssues = await _context.TaskItems
                .Include(t => t.AssignedUser)
                .Include(t => t.Category)
                .Where(t => t.ProjectId == sprint.ProjectId
                            && !t.IsCompleted
                            && (t.IssueType == IssueType.Story || t.IssueType == IssueType.Task)
                            && !tasksInOpenSprints.Contains(t.Id))
                .OrderByDescending(t => t.Priority)
                .ThenBy(t => t.DueDate)
                .ToListAsync();

            var vm = new SprintBoardVm
            {
                SprintId = sprint.Id,
                SprintName = sprint.Name,
                ProjectId = sprint.ProjectId,
                ProjectName = sprint.Project.Name,
                Statuses = await _context.WorkflowStatuses.Where(ws => ws.SprintId == sprint.Id).OrderBy(ws => ws.Order).ToListAsync(),
                SprintIssues = sprintIssues
            };

            ViewBag.BacklogIssues = backlogIssues;
            ViewBag.SprintStatus = sprint.Status;
            ViewBag.HasActiveSprint = sprint.Status == SprintStatus.Active;

            return View(vm);
        }

        // 🔄 به‌روزرسانی ترتیب اولویت Issues در اسپرینت
        [HttpPost]
        public async Task<IActionResult> UpdateSprintOrder(int sprintId, [FromBody] List<int> taskIds)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var sprint = await _context.Sprints
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == sprintId);

            if (sprint == null)
            {
                return Json(new { success = false, message = "اسپرینت یافت نشد." });
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == sprint.ProjectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));
            
            if (!hasAccess)
            {
                return Json(new { success = false, message = "شما به این اسپرینت دسترسی ندارید." });
            }

            // فقط برای Sprint های غیر Completed
            if (sprint.Status == SprintStatus.Completed)
            {
                return Json(new { success = false, message = "اسپرینت تکمیل شده قابل ویرایش نیست." });
            }

            try
            {
                // به‌روزرسانی SprintPriority برای هر Issue
                for (int i = 0; i < taskIds.Count; i++)
                {
                    var sprintTask = await _context.SprintTasks
                        .FirstOrDefaultAsync(st => st.SprintId == sprintId && st.TaskId == taskIds[i]);
                    
                    if (sprintTask != null)
                    {
                        sprintTask.SprintPriority = i + 1; // اولویت از 1 شروع می‌شود
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "ترتیب اولویت با موفقیت به‌روزرسانی شد." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در به‌روزرسانی: " + ex.Message });
            }
        }

        
        // 📌 دریافت تسک‌های پروژه برای اضافه کردن به اسپرینت
        [HttpGet]
        public async Task<IActionResult> GetProjectTasks(int projectId, int sprintId)
        {
            Console.WriteLine($"[GetProjectTasks] projectId: {projectId}, sprintId: {sprintId}");
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"[GetProjectTasks] userId: {userId}");

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == projectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            Console.WriteLine($"[GetProjectTasks] hasAccess: {hasAccess}");

            if (!hasAccess)
            {
                Console.WriteLine("[GetProjectTasks] Access denied");
                return Json(new { success = false, message = "شما به این پروژه دسترسی ندارید." });
            }

            // دریافت تسک‌های پروژه که در این اسپرینت نیستند
            var sprintTaskIds = await _context.SprintTasks
                .Where(st => st.SprintId == sprintId)
                .Select(st => st.TaskId)
                .ToListAsync();

            Console.WriteLine($"[GetProjectTasks] sprintTaskIds: {string.Join(",", sprintTaskIds)}");

            var availableTasks = await _context.TaskItems
                .Where(t => t.ProjectId == projectId 
                    && !sprintTaskIds.Contains(t.Id)
                    && !t.IsCompleted // فقط تسک‌های انجام نشده
                    && t.IssueType != IssueType.Epic
                    && t.IssueType != IssueType.Subtask) // Epic/Subtask قابل افزودن به اسپرینت نیستند
                .Include(t => t.Category)
                .Include(t => t.AssignedUser)
                .Select(t => new
                {
                    id = t.Id,
                    title = t.Title,
                    description = t.Description,
                    categoryName = t.Category.Name,
                    assignedUserName = t.AssignedUser != null ? (t.AssignedUser.FullName ?? t.AssignedUser.UserName) : "تخصیص نیافته",
                    dueDate = t.DueDate,
                    priority = t.Priority.ToString(),
                    isCompleted = t.IsCompleted
                })
                .ToListAsync();

            Console.WriteLine($"[GetProjectTasks] Found {availableTasks.Count} available tasks");

            return Json(new { success = true, tasks = availableTasks });
        }

        // 📌 اضافه کردن تسک به اسپرینت
        [HttpPost]
        public async Task<IActionResult> AddTaskToSprint(int sprintId, int taskId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var sprint = await _context.Sprints
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == sprintId);

            if (sprint == null)
            {
                return Json(new { success = false, message = "اسپرینت یافت نشد." });
            }

            // فقط اسپرینت فعال می‌تواند تسک داشته باشد
            if (sprint.Status != SprintStatus.Active)
            {
                return Json(new { success = false, message = "فقط اسپرینت فعال می‌تواند تسک داشته باشد." });
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == sprint.ProjectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Json(new { success = false, message = "شما به این اسپرینت دسترسی ندارید." });
            }

            var task = await _context.TaskItems.FindAsync(taskId);
            if (task == null)
            {
                return Json(new { success = false, message = "تسک یافت نشد." });
            }

            // Epic/Subtask قابل افزودن به اسپرینت نیستند
            if (task.IssueType == IssueType.Epic || task.IssueType == IssueType.Subtask)
            {
                return Json(new { success = false, message = "فقط Story/Task/Bug قابل اضافه شدن به اسپرینت هستند." });
            }

            // تسک‌های انجام شده یا لغو شده قابل اضافه شدن نیستند
            if (task.IsCompleted)
            {
                return Json(new { success = false, message = "تسک‌های انجام شده قابل اضافه شدن به اسپرینت نیستند." });
            }

            // بررسی اینکه تسک قبلاً در اسپرینت نباشد
            var existingSprintTask = await _context.SprintTasks
                .FirstOrDefaultAsync(st => st.SprintId == sprintId && st.TaskId == taskId);

            if (existingSprintTask != null)
            {
                return Json(new { success = false, message = "این تسک قبلاً در اسپرینت اضافه شده است." });
            }

            var sprintTask = new SprintTask
            {
                SprintId = sprintId,
                TaskId = taskId,
                AddedAt = DateTime.UtcNow,
                AddedByUserId = userId,
                Status = SprintTaskStatus.Pending,
                SprintPriority = (int)TaskPriority.Medium
            };

            _context.SprintTasks.Add(sprintTask);
            await _context.SaveChangesAsync();

            // بازگشت اطلاعات به‌روز شده
            var allTasks = await _context.SprintTasks
                .Where(st => st.SprintId == sprintId)
                .Include(st => st.Task)
                .Select(st => new
                {
                    id = st.TaskId,
                    title = st.Task.Title,
                    status = st.Status.ToString()
                })
                .ToListAsync();

            return Json(new
            {
                success = true,
                message = "تسک با موفقیت به اسپرینت اضافه شد.",
                tasks = allTasks
            });
        }

        // 📌 حذف تسک از اسپرینت
        [HttpPost]
        public async Task<IActionResult> RemoveTaskFromSprint(int sprintId, int taskId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var sprint = await _context.Sprints
                
                .FirstOrDefaultAsync(s => s.Id == sprintId);

            if (sprint == null)
            {
                return Json(new { success = false, message = "اسپرینت یافت نشد." });
            }

            // فقط اسپرینت فعال می‌تواند تسک داشته باشد
            if (sprint.Status != SprintStatus.Active)
            {
                return Json(new { success = false, message = "فقط اسپرینت فعال می‌تواند تسک داشته باشد." });
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == sprint.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Json(new { success = false, message = "شما به این اسپرینت دسترسی ندارید." });
            }

            // Find the SprintTask by composite key
            var sprintTask = await _context.SprintTasks
                .FirstOrDefaultAsync(st => st.SprintId == sprintId && st.TaskId == taskId);

            if (sprintTask == null)
            {
                return Json(new { success = false, message = "تسک در اسپرینت یافت نشد." });
            }
            
            try
            {
                // Delete using SQL to avoid Entity Framework tracking issues
                await _context.Database.ExecuteSqlRawAsync(
                    "DELETE FROM SprintTasks WHERE SprintId = {0} AND TaskId = {1}",
                    sprintId, taskId);
                
                // بازگشت اطلاعات به‌روز شده
                var remainingTasks = await _context.SprintTasks
                    .Where(st => st.SprintId == sprintId)
                    .Include(st => st.Task)
                    .Select(st => new
                    {
                        id = st.TaskId,
                        title = st.Task.Title,
                        status = st.Status.ToString()
                    })
                    .ToListAsync();
                
                return Json(new { 
                    success = true, 
                    message = "تسک با موفقیت از اسپرینت حذف شد.",
                    remainingTasks = remainingTasks
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در حذف تسک: " + ex.Message });
            }
        }

        // 📌 تغییر وضعیت تسک در اسپرینت
        [HttpPost]
        public async Task<IActionResult> UpdateTaskStatus(int sprintId, int taskId, SprintTaskStatus status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var sprint = await _context.Sprints
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == sprintId);

            if (sprint == null)
            {
                return Json(new { success = false, message = "اسپرینت یافت نشد." });
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == sprint.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Json(new { success = false, message = "شما به این اسپرینت دسترسی ندارید." });
            }

            // Check if task exists
            var exists = await _context.SprintTasks
                .AnyAsync(st => st.SprintId == sprintId && st.TaskId == taskId);

            if (!exists)
            {
                return Json(new { success = false, message = "تسک در اسپرینت یافت نشد." });
            }

            try
            {
                // Update using SQL to avoid Entity Framework tracking issues
                var completedAt = status == SprintTaskStatus.Completed ? DateTime.UtcNow : (DateTime?)null;
                
                if (completedAt.HasValue)
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE SprintTasks SET Status = {0}, CompletedAt = {1} WHERE SprintId = {2} AND TaskId = {3}",
                        (int)status, completedAt, sprintId, taskId);
                }
                else
                {
                    await _context.Database.ExecuteSqlRawAsync(
                        "UPDATE SprintTasks SET Status = {0} WHERE SprintId = {1} AND TaskId = {2}",
                        (int)status, sprintId, taskId);
                }

                return Json(new { success = true, message = "وضعیت تسک با موفقیت تغییر کرد." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در تغییر وضعیت تسک: " + ex.Message });
            }
        }

        // 📌 ایجاد اسپرینت جدید
        [HttpGet]
        public async Task<IActionResult> Create(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == projectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            var project = await _context.Projects.FindAsync(projectId);
            ViewBag.ProjectName = project?.Name;

            var vm = new SprintCreateVm
            {
                ProjectId = projectId,
                StartDate = DateTime.Today,
                EndDate = DateTime.Today.AddDays(14)
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SprintCreateVm vm)
        {
            Console.WriteLine($"[Create POST] Received - Name: {vm.Name}, ProjectId: {vm.ProjectId}");
            Console.WriteLine($"[Create POST] StartDate (initial): {vm.StartDate}, EndDate (initial): {vm.EndDate}");
            
            // بررسی اینکه آیا اسپرینت فعال وجود دارد
            var hasActiveSprint = await _context.Sprints
                .AnyAsync(s => s.ProjectId == vm.ProjectId && s.Status == SprintStatus.Active);

            if (hasActiveSprint)
            {
                Console.WriteLine("[Create POST] Active sprint exists");
                TempData["Error"] = "فقط یک اسپرینت می‌تواند در هر پروژه فعال باشد. لطفاً اسپرینت فعال را خاتمه دهید.";
                return RedirectToAction(nameof(Index), new { projectId = vm.ProjectId });
            }

            // Parse dates - check if they come as Persian dates
            bool startDateParsed = false;
            string? startDateFormValue = Request.Form["StartDate"].ToString();
            Console.WriteLine($"[Create POST] StartDate from form: '{startDateFormValue}'");
            
            if (Request.Form.ContainsKey("StartDate") && !string.IsNullOrWhiteSpace(startDateFormValue))
            {
                // Try to parse as Gregorian date first (format: YYYY-MM-DD from Persian datepicker altFormat)
                if (DateTime.TryParse(startDateFormValue, out DateTime startDate))
                {
                    vm.StartDate = startDate;
                    startDateParsed = true;
                    Console.WriteLine($"[Create POST] StartDate parsed as Gregorian: {vm.StartDate}");
                }
                else
                {
                    // Try to parse as Persian date
                    if (startDateFormValue.IsValidPersianDateTime())
                    {
                        var persianDate = startDateFormValue.ToGregorianDateTime();
                        if (persianDate.HasValue)
                        {
                            vm.StartDate = persianDate.Value;
                            startDateParsed = true;
                            Console.WriteLine($"[Create POST] StartDate parsed as Persian: {vm.StartDate}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[Create POST] StartDate could not be parsed");
                    }
                }
            }
            else
            {
                Console.WriteLine("[Create POST] StartDate field not found or empty in form");
            }
            
            if (startDateParsed)
            {
                ModelState.Remove("StartDate");
            }
            else
            {
                Console.WriteLine("[Create POST] Warning: StartDate not parsed, using default value");
            }

            bool endDateParsed = false;
            string? endDateFormValue = Request.Form["EndDate"].ToString();
            Console.WriteLine($"[Create POST] EndDate from form: '{endDateFormValue}'");
            
            if (Request.Form.ContainsKey("EndDate") && !string.IsNullOrWhiteSpace(endDateFormValue))
            {
                // Try to parse as Gregorian date first
                if (DateTime.TryParse(endDateFormValue, out DateTime endDate))
                {
                    vm.EndDate = endDate;
                    endDateParsed = true;
                    Console.WriteLine($"[Create POST] EndDate parsed as Gregorian: {vm.EndDate}");
                }
                else
                {
                    // Try to parse as Persian date
                    if (endDateFormValue.IsValidPersianDateTime())
                    {
                        var persianDate = endDateFormValue.ToGregorianDateTime();
                        if (persianDate.HasValue)
                        {
                            vm.EndDate = persianDate.Value;
                            endDateParsed = true;
                            Console.WriteLine($"[Create POST] EndDate parsed as Persian: {vm.EndDate}");
                        }
                    }
                    else
                    {
                        Console.WriteLine("[Create POST] EndDate could not be parsed");
                    }
                }
            }
            else
            {
                Console.WriteLine("[Create POST] EndDate field not found or empty in form");
            }
            
            if (endDateParsed)
            {
                ModelState.Remove("EndDate");
            }
            else
            {
                Console.WriteLine("[Create POST] Warning: EndDate not parsed, using default value");
            }

            // بررسی اعتبار تاریخ‌ها
            if (vm.StartDate >= vm.EndDate)
            {
                Console.WriteLine($"[Create POST] Date validation failed: StartDate >= EndDate");
                ModelState.AddModelError("EndDate", "تاریخ پایان باید پس از تاریخ شروع باشد.");
            }

            if (!ModelState.IsValid)
            {
                Console.WriteLine("[Create POST] ModelState is invalid:");
                foreach (var error in ModelState)
                {
                    foreach (var err in error.Value.Errors)
                    {
                        Console.WriteLine($"  - {error.Key}: {err.ErrorMessage}");
                    }
                }
                var project = await _context.Projects.FindAsync(vm.ProjectId);
                ViewBag.ProjectName = project?.Name;
                return View(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            Console.WriteLine($"[Create POST] userId: {userId}");

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == vm.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                Console.WriteLine("[Create POST] Access denied");
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            Console.WriteLine($"[Create POST] All validations passed. Creating sprint...");
            Console.WriteLine($"[Create POST] Final values - Name: {vm.Name}, StartDate: {vm.StartDate}, EndDate: {vm.EndDate}, ProjectId: {vm.ProjectId}");

            try
            {
                var sprint = new Sprint
                {
                    Name = vm.Name,
                    Description = vm.Description,
                    Goal = vm.Goal,
                    StartDate = vm.StartDate,
                    EndDate = vm.EndDate,
                    ProjectId = vm.ProjectId,
                    CreatorUserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                Console.WriteLine($"[Create POST] Sprint object created. Adding to context...");
                _context.Sprints.Add(sprint);
                
                Console.WriteLine($"[Create POST] Saving changes...");
                await _context.SaveChangesAsync();
                
                Console.WriteLine($"[Create POST] Sprint created successfully with Id: {sprint.Id}");

                // ایجاد وضعیت‌های پیش‌فرض برای اسپرینت
                var defaultStatuses = new[]
                {
                    new WorkflowStatus 
                    { 
                        Name = "باید انجام شود", 
                        Type = WorkflowType.Todo, 
                        Order = 1, 
                        Color = "#6c757d", 
                        ProjectId = sprint.ProjectId, 
                        SprintId = sprint.Id, 
                        IsDefault = true,
                        CreatedAt = DateTime.UtcNow
                    },
                    new WorkflowStatus 
                    { 
                        Name = "انجام شده", 
                        Type = WorkflowType.Done, 
                        Order = 2, 
                        Color = "#198754", 
                        ProjectId = sprint.ProjectId, 
                        SprintId = sprint.Id, 
                        IsFinal = true,
                        CreatedAt = DateTime.UtcNow
                    }
                };

                _context.WorkflowStatuses.AddRange(defaultStatuses);
                await _context.SaveChangesAsync();

                Console.WriteLine($"[Create POST] Default workflow statuses created for sprint {sprint.Id}");

                TempData["Success"] = "اسپرینت با موفقیت ایجاد شد و وضعیت‌های پیش‌فرض تنظیم شدند.";
                return RedirectToAction(nameof(Index), new { projectId = vm.ProjectId });
            }
            catch (Exception ex)
            {
                // Log error for debugging
                Console.WriteLine($"[Create POST] ERROR creating sprint: {ex.Message}");
                Console.WriteLine($"[Create POST] ERROR Type: {ex.GetType().Name}");
                Console.WriteLine($"[Create POST] ERROR Stack trace: {ex.StackTrace}");
                
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"[Create POST] ERROR InnerException: {ex.InnerException.Message}");
                }
                
                TempData["Error"] = $"خطا در ایجاد اسپرینت: {ex.Message}";
                var project = await _context.Projects.FindAsync(vm.ProjectId);
                ViewBag.ProjectName = project?.Name;
                return View(vm);
            }
        }

        // 📌 ویرایش اسپرینت
        [HttpGet("{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var sprint = await _context.Sprints
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sprint == null)
            {
                TempData["Error"] = "اسپرینت یافت نشد.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == sprint.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این اسپرینت دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            ViewBag.ProjectName = sprint.Project.Name;

            var vm = new SprintEditVm
            {
                Id = sprint.Id,
                Name = sprint.Name,
                Description = sprint.Description,
                Goal = sprint.Goal,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                IsActive = sprint.IsActive,
                IsCompleted = sprint.IsCompleted,
                Status = sprint.Status,
                ProjectId = sprint.ProjectId
            };

            return View(vm);
        }

        [HttpPost("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(SprintEditVm vm)
        {
            // Parse dates - check if they come as Persian dates
            if (Request.Form.ContainsKey("StartDate"))
            {
                var startDateStr = Request.Form["StartDate"].ToString();
                if (!string.IsNullOrEmpty(startDateStr))
                {
                    // Try to parse as Gregorian date first
                    if (DateTime.TryParse(startDateStr, out DateTime startDate))
                    {
                        vm.StartDate = startDate;
                    }
                    else
                    {
                        // Try to parse as Persian date
                        if (startDateStr.IsValidPersianDateTime())
                        {
                            var persianDate = startDateStr.ToGregorianDateTime();
                            if (persianDate.HasValue)
                            {
                                vm.StartDate = persianDate.Value;
                            }
                        }
                    }
                    ModelState.Remove("StartDate");
                }
            }

            if (Request.Form.ContainsKey("EndDate"))
            {
                var endDateStr = Request.Form["EndDate"].ToString();
                if (!string.IsNullOrEmpty(endDateStr))
                {
                    // Try to parse as Gregorian date first
                    if (DateTime.TryParse(endDateStr, out DateTime endDate))
                    {
                        vm.EndDate = endDate;
                    }
                    else
                    {
                        // Try to parse as Persian date
                        if (endDateStr.IsValidPersianDateTime())
                        {
                            var persianDate = endDateStr.ToGregorianDateTime();
                            if (persianDate.HasValue)
                            {
                                vm.EndDate = persianDate.Value;
                            }
                        }
                    }
                    ModelState.Remove("EndDate");
                }
            }

            if (!ModelState.IsValid)
            {
                var project = await _context.Projects.FindAsync(vm.ProjectId);
                ViewBag.ProjectName = project?.Name;
                return View(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var sprint = await _context.Sprints.FindAsync(vm.Id);
            if (sprint == null)
            {
                TempData["Error"] = "اسپرینت یافت نشد.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == sprint.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این اسپرینت دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            sprint.Name = vm.Name;
            sprint.Description = vm.Description;
            sprint.Goal = vm.Goal;
            sprint.StartDate = vm.StartDate;
            sprint.EndDate = vm.EndDate;
            sprint.IsActive = vm.IsActive;
            sprint.IsCompleted = vm.IsCompleted;
            sprint.Status = vm.Status;
            sprint.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "اسپرینت با موفقیت ویرایش شد.";
            return RedirectToAction(nameof(Index), new { projectId = sprint.ProjectId });
        }

        // 📌 حذف اسپرینت
        [HttpGet("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var sprint = await _context.Sprints
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sprint == null)
            {
                TempData["Error"] = "اسپرینت یافت نشد.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == sprint.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این اسپرینت دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            return View(sprint);
        }

        [HttpPost("{id}"), ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var sprint = await _context.Sprints
                .Include(s => s.SprintTasks)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sprint == null)
            {
                TempData["Error"] = "اسپرینت یافت نشد.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == sprint.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این اسپرینت دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            // فقط اسپرینت‌های در حال برنامه‌ریزی قابل حذف هستند
            if (sprint.Status == SprintStatus.Active)
            {
                TempData["Error"] = "امکان حذف اسپرینت فعال وجود ندارد. لطفاً ابتدا اسپرینت را خاتمه دهید.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (sprint.Status == SprintStatus.Completed)
            {
                TempData["Error"] = "امکان حذف اسپرینت خاتمه یافته وجود ندارد.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var projectId = sprint.ProjectId;

            // حذف تسک‌های اسپرینت
            _context.SprintTasks.RemoveRange(sprint.SprintTasks);
            
            // حذف اسپرینت
            _context.Sprints.Remove(sprint);
            await _context.SaveChangesAsync();

            TempData["Success"] = "اسپرینت با موفقیت حذف شد.";
            return RedirectToAction(nameof(Index), new { projectId });
        }

        // 📌 تغییر وضعیت اسپرینت
        [HttpPost]
        //[Route("/TaskPlanner/Sprints/ChangeStatus")]
        public async Task<IActionResult> ChangeStatus(int id, SprintStatus status)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var sprint = await _context.Sprints
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == id);

            if (sprint == null)
            {
                TempData["Error"] = "اسپرینت یافت نشد.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == sprint.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این اسپرینت دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            try
            {
                // بررسی اینکه آیا می‌خواهیم یک اسپرینت فعال کنیم
                if (status == SprintStatus.Active)
                {
                    // بررسی اینکه آیا قبلاً اسپرینت فعالی در این پروژه وجود دارد
                    var hasActiveSprint = await _context.Sprints
                        .AnyAsync(s => s.ProjectId == sprint.ProjectId 
                            && s.Id != id 
                            && s.Status == SprintStatus.Active);

                    if (hasActiveSprint)
                    {
                        TempData["Error"] = "فقط یک اسپرینت می‌تواند در هر پروژه فعال باشد.";
                        return RedirectToAction(nameof(Details), new { id });
                    }
                }

                var updatedAt = DateTime.UtcNow;
                var statusValue = (int)status;
                var isCompleted = status == SprintStatus.Completed ? 1 : 0;
                var isActive = status == SprintStatus.Active ? 1 : 0;

                // Update sprint state
                await _context.Database.ExecuteSqlRawAsync(
                    "UPDATE Sprints SET Status = {0}, UpdatedAt = {1}, IsCompleted = {2}, IsActive = {3} WHERE Id = {4}",
                    statusValue, updatedAt, isCompleted, isActive, id);

                // وقتی اسپرینت خاتمه می‌یابد، تمام تسک‌های با وضعیت Done را تکمیل کن
                if (status == SprintStatus.Completed)
                {
                    var doneTasks = await _context.SprintTasks
                        .Where(st => st.SprintId == id)
                        .Include(st => st.Task)
                        .ThenInclude(t => t.Status)
                        .Select(st => st.Task)
                        .Where(t => t.Status != null && t.Status.Type == WorkflowType.Done && t.Status.IsFinal)
                        .ToListAsync();

                    foreach (var t in doneTasks)
                    {
                        t.IsCompleted = true;
                        t.UpdatedAt = DateTime.UtcNow;
                    }

                    await _context.SaveChangesAsync();
                }

                TempData["Success"] = "وضعیت اسپرینت با موفقیت تغییر کرد.";
            }
            catch (Exception ex)
            {
                TempData["Error"] = "خطا در تغییر وضعیت اسپرینت: " + ex.Message;
            }

            // اگر اسپرینت تکمیل شد، به صفحه Details پروژه redirect می‌کنیم
            if (status == SprintStatus.Completed)
            {
                return RedirectToAction("Details", "Projects", new { id = sprint.ProjectId });
            }

            return RedirectToAction(nameof(Details), new { id });
        }
    }
}