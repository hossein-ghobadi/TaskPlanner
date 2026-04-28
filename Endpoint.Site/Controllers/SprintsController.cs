using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Data.SqlClient;
using System;
using System.Data.Common;
using Microsoft.EntityFrameworkCore.Infrastructure;
using System.Security.Claims;
using System.Text.Json;
using TaskPlanner.Domain.Entities.Users;
using TaskPlanner.Persistence.Contexts;
using Endpoint.Site.Models;
using DNTPersianUtils.Core;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Application.Services.ProjectService;
using DocumentFormat.OpenXml.InkML;
using Microsoft.CodeAnalysis;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class SprintsController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IProjectCommandService _projectCommandService;

        public SprintsController(
            MVPTestDatabaseContext context, 
            UserManager<User> userManager,
            IProjectCommandService projectCommandService)
        {
            _context = context;
            _userManager = userManager;
            _projectCommandService = projectCommandService;
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

        // 📌 جزئیات اسپرینت - Redirect به Board
        [HttpGet("{id}")]
        public IActionResult Details(int id)
        {
            // Redirect به Board (تلفیق Details و Board)
            return RedirectToAction(nameof(Board), new { id });
        }

        // 📌 برد (Kanban) اسپرینت مانند Jira
        [HttpGet("Board/{id}")]
        public async Task<IActionResult> Board(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var sprint = await _context.Sprints
                .Include(s => s.Project)
                .Include(s => s.SprintTasks)
                    .ThenInclude(st => st.Task)
                        .ThenInclude(t => t.Status)
                .Include(s => s.SprintTasks)
                    .ThenInclude(st => st.Task)
                        .ThenInclude(t => t.WorkflowStatus)
                .Include(s => s.SprintTasks)
                    .ThenInclude(st => st.Task)
                        .ThenInclude(t => t.ChildIssues)
                .Include(s => s.SprintTasks)
                    .ThenInclude(st => st.Task)
                        .ThenInclude(t => t.AssignedUser)
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

            var tasksWithoutSprint = _context.TaskItems
                        .Where(t => t.ProjectId == sprint.ProjectId && !t.IsCompleted && (t.SprintId != sprint.Id || t.SprintId == null) && (t.IssueType == IssueType.Story || t.IssueType == IssueType.Task))
                        .ToList();
            // وضعیت‌های اسپرینت (ستون‌ها)
            var statuses = await _context.WorkflowStatuses
                .Where(ws => ws.SprintId == sprint.Id)
                .OrderBy(ws => ws.Order)
                .ToListAsync();

            var sprintTasks = sprint.SprintTasks.Select(st => st.Task).ToList();
            // محاسبه آمار
            var completedTasks = sprintTasks.Count(t => t.StatusId.HasValue && statuses.Any(ws => ws.Id == t.StatusId && ws.IsFinal));
            var inProgressTasks = sprintTasks.Count(t => t.StatusId.HasValue && statuses.Any(ws => ws.Id == t.StatusId && ws.Type == WorkflowType.InProgress));
            var pendingTasks = sprintTasks.Count(t => t.StatusId == null || statuses.Any(ws => ws.Id == t.StatusId && ws.Type == WorkflowType.Todo));
            var blockedTasks = sprintTasks.Count(t => t.StatusId.HasValue && statuses.Any(ws => ws.Id == t.StatusId && ws.Type == WorkflowType.Blocked));

            var vm = new SprintBoardVm
            {
                SprintId = sprint.Id,
                SprintName = sprint.Name,
                Description = sprint.Description,
                Goal = sprint.Goal,
                StartDate = sprint.StartDate,
                EndDate = sprint.EndDate,
                Status = sprint.Status,
                ProjectId = sprint.ProjectId,
                ProjectName = sprint.Project.Name,
                Statuses = statuses,
                SprintIssues = sprintTasks,
                TotalTasks = sprintTasks.Count,
                CompletedTasks = completedTasks,
                InProgressTasks = inProgressTasks,
                PendingTasks = pendingTasks,
                BlockedTasks = blockedTasks
            };
            ViewBag.backLog = tasksWithoutSprint;
            ViewBag.WorkflowStatuses = statuses;
            ViewBag.ProjectId = sprint.ProjectId;
            ViewBag.SprintId = sprint.Id;

            // دریافت دسته‌بندی‌ها برای فرم ایجاد تسک (متعلق به همین پروژه)
            var categories = await _context.TaskCategories
                .Where(c => c.ProjectId == sprint.ProjectId)
                .OrderBy(c => c.Name)
                .ToListAsync();
            ViewBag.Categories = categories;

            // دریافت اعضای پروژه برای فرم ایجاد تسک
            var memberIds = await _context.ProjectMembers
                .Where(m => m.ProjectId == sprint.ProjectId)
                .Select(m => m.UserId)
                .ToListAsync();
            ViewBag.MemberIds = memberIds;

            // دریافت لیست کامل اعضای پروژه برای فیلتر
            var memberUserIds = await _context.ProjectMembers
                .Where(m => m.ProjectId == sprint.ProjectId)
                .Select(m => m.UserId)
                .ToListAsync();
            
            var projectMembers = await _context.Users
                .Where(u => memberUserIds.Contains(u.Id))
                .Select(u => new ProjectMemberVm
                {
                    UserId = u.Id,
                    FullName = u.FullName ?? u.UserName,
                    UserName = u.UserName
                })
                .ToListAsync();
            
            // اضافه کردن سازنده پروژه به لیست (اگر از قبل در لیست نیست)
            if (sprint.Project != null && !string.IsNullOrEmpty(sprint.Project.CreatorUserId))
            {
                var creatorExists = projectMembers.Any(pm => pm.UserId == sprint.Project.CreatorUserId);
                if (!creatorExists)
                {
                    var creatorInfo = await _userManager.FindByIdAsync(sprint.Project.CreatorUserId);
                    if (creatorInfo != null)
                    {
                        projectMembers.Add(new ProjectMemberVm
                        {
                            UserId = sprint.Project.CreatorUserId,
                            FullName = $"{creatorInfo.FullName ?? creatorInfo.UserName} - سازنده پروژه",
                            UserName = creatorInfo.UserName ?? ""
                        });
                    }
                }
            }
            
            ViewBag.ProjectMembers = projectMembers.OrderBy(u => u.FullName).ToList();

            return View(vm);
        }

        // 📅 Sprint Planning: صفحه برنامه‌ریزی اسپرینت (مثل Jira)
        [HttpGet("Planning/{id}")]
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
                .Include(t => t.ProjectIssueType)
                .Where(t => t.ProjectId == sprint.ProjectId
                            && !t.IsCompleted
                            && (t.ProjectIssueType != null ? t.ProjectIssueType.CanAddToSprint : 
                                (t.IssueType == IssueType.Story || t.IssueType == IssueType.Task || t.IssueType == IssueType.Bug)) // منطق CanAddToSprint
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
            
            // اضافه کردن ProjectIssueTypes برای استفاده در Quick Task form
            Console.WriteLine($"[Board] ===== START Loading ProjectIssueTypes =====");
            Console.WriteLine($"[Board] SprintId: {sprint.Id}, ProjectId: {sprint.ProjectId}");
            
            // بررسی اینکه آیا ProjectIssueTypes در دیتابیس وجود دارد
            var allProjectIssueTypes = await _context.ProjectIssueTypes.ToListAsync();
            Console.WriteLine($"[Board] Total ProjectIssueTypes in database: {allProjectIssueTypes.Count}");
            var projectIssueTypesForThisProject = allProjectIssueTypes.Where(pit => pit.ProjectId == sprint.ProjectId).ToList();
            Console.WriteLine($"[Board] ProjectIssueTypes for ProjectId {sprint.ProjectId}: {projectIssueTypesForThisProject.Count}");
            foreach (var pit in projectIssueTypesForThisProject)
            {
                Console.WriteLine($"[Board]   - {pit.Name} (Id: {pit.Id}, ProjectId: {pit.ProjectId})");
            }
            
            var projectIssueTypes = await _context.ProjectIssueTypes
                .Where(pit => pit.ProjectId == sprint.ProjectId)
                .OrderBy(pit => pit.Order)
                .ToListAsync();
            
            Console.WriteLine($"[Board] Query result: {projectIssueTypes.Count} ProjectIssueTypes");
            if (projectIssueTypes.Any())
            {
                foreach (var pit in projectIssueTypes)
                {
                    Console.WriteLine($"[Board] - {pit.Name} (Id: {pit.Id}, Level: {pit.Level}, CanAddToSprint: {pit.CanAddToSprint})");
                }
            }
            
            // اگر ProjectIssueTypes وجود ندارد، آن‌ها را ایجاد کن
            if (!projectIssueTypes.Any())
            {
                try
                {
                    // بررسی مجدد برای جلوگیری از race condition
                    var hasAnyIssueType = await _context.ProjectIssueTypes
                        .AnyAsync(pit => pit.ProjectId == sprint.ProjectId);
                    
                    if (!hasAnyIssueType)
                    {
                        // اطمینان از اینکه Project به درستی لود شده است
                        if (sprint.Project == null)
                        {
                            sprint.Project = await _context.Projects
                                .FirstOrDefaultAsync(p => p.Id == sprint.ProjectId);
                        }
                        
                        if (sprint.Project != null)
                        {
                            // ایجاد ProjectIssueTypes پیش‌فرض برای این پروژه
                            var now = DateTime.UtcNow;
                            var creatorUserId = sprint.Project.CreatorUserId ?? userId; // Fallback به userId فعلی
                            
                            var defaults = new List<ProjectIssueType>
                            {
                                new ProjectIssueType
                                {
                                    ProjectId = sprint.ProjectId,
                                    Name = "ویژگی",
                                    Description = "نوع پیش‌فرض ویژگی",
                                    Icon = "📦",
                                    Color = "#8B5CF6",
                                    Order = 0,
                                    BaseType = IssueType.Epic,
                                    IsCustom = false,
                                    CanAddToSprint = false,
                                    CanHaveChildren = true,
                                    IncludeInReports = true,
                                    Level = IssueTypeLevel.Epic,
                                    CreatedByUserId = creatorUserId,
                                    CreatedAt = now
                                },
                                new ProjectIssueType
                                {
                                    ProjectId = sprint.ProjectId,
                                    Name = "کار",
                                    Description = "نوع پیش‌فرض کار",
                                    Icon = "✅",
                                    Color = "#3B82F6",
                                    Order = 1,
                                    BaseType = IssueType.Task,
                                    IsCustom = false,
                                    CanAddToSprint = true,
                                    CanHaveChildren = true,
                                    IncludeInReports = true,
                                    Level = IssueTypeLevel.StoryLevel,
                                    CreatedByUserId = creatorUserId,
                                    CreatedAt = now
                                },
                                new ProjectIssueType
                                {
                                    ProjectId = sprint.ProjectId,
                                    Name = "کارک",
                                    Description = "نوع پیش‌فرض کارک",
                                    Icon = "🔹",
                                    Color = "#06B6D4",
                                    Order = 2,
                                    BaseType = IssueType.Subtask,
                                    IsCustom = false,
                                    CanAddToSprint = false,
                                    CanHaveChildren = false,
                                    IncludeInReports = true,
                                    Level = IssueTypeLevel.Subtask,
                                    CreatedByUserId = creatorUserId,
                                    CreatedAt = now
                                }
                            };
                            
                            _context.ProjectIssueTypes.AddRange(defaults);
                            await _context.SaveChangesAsync();
                            
                            // بارگذاری مجدد برای اطمینان
                            projectIssueTypes = await _context.ProjectIssueTypes
                                .Where(pit => pit.ProjectId == sprint.ProjectId)
                                .OrderBy(pit => pit.Order)
                                .ToListAsync();
                            
                            Console.WriteLine($"[Board] Created {projectIssueTypes.Count} ProjectIssueTypes for Project {sprint.ProjectId}");
                        }
                        else
                        {
                            Console.WriteLine($"[Board] ERROR: Project {sprint.ProjectId} not found!");
                        }
                    }
                    else
                    {
                        // اگر در بررسی مجدد پیدا شد، بارگذاری کن
                        projectIssueTypes = await _context.ProjectIssueTypes
                            .Where(pit => pit.ProjectId == sprint.ProjectId)
                            .OrderBy(pit => pit.Order)
                            .ToListAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[Board] ERROR creating ProjectIssueTypes: {ex.Message}");
                    Console.WriteLine($"[Board] StackTrace: {ex.StackTrace}");
                    // در صورت خطا، projectIssueTypes خالی باقی می‌ماند
                }
            }
            
            ViewBag.ProjectIssueTypes = projectIssueTypes;
            Console.WriteLine($"[Board] Final ProjectIssueTypes count: {projectIssueTypes.Count} for Project {sprint.ProjectId}");
            Console.WriteLine($"[Board] ViewBag.ProjectIssueTypes set: {ViewBag.ProjectIssueTypes != null}");
            
            // Debug: بررسی اینکه آیا ViewBag به درستی تنظیم شده است
            if (ViewBag.ProjectIssueTypes is List<ProjectIssueType> viewBagTypes)
            {
                Console.WriteLine($"[Board] ViewBag contains {viewBagTypes.Count} items");
            }
            else
            {
                Console.WriteLine($"[Board] WARNING: ViewBag.ProjectIssueTypes is not List<ProjectIssueType>! Type: {ViewBag.ProjectIssueTypes?.GetType().Name ?? "null"}");
            }

            return View(vm);
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
                    && (t.ProjectIssueType != null ? t.ProjectIssueType.CanAddToSprint : 
                        (t.IssueType == IssueType.Story || t.IssueType == IssueType.Task || t.IssueType == IssueType.Bug))) // منطق CanAddToSprint
                .Include(t => t.Category)
                .Include(t => t.AssignedUser)
                .Include(t => t.ProjectIssueType)
                .Select(t => new
                {
                    id = t.Id,
                    title = t.Title,
                    description = t.Description,
                    categoryName = t.Category != null ? t.Category.Name : "بدون دسته",
                    assignedUserId = t.AssignedUserId,
                    assignedUserName = t.AssignedUser != null ? (t.AssignedUser.FullName ?? t.AssignedUser.UserName) : "تخصیص نیافته",
                    dueDate = t.DueDate,
                    priority = t.Priority.ToString(),
                    isCompleted = t.IsCompleted
                })
                .ToListAsync();

            Console.WriteLine($"[GetProjectTasks] Found {availableTasks.Count} available tasks");

            return Json(new { success = true, tasks = availableTasks });
        }

        // 📋 دریافت ProjectIssueTypes به صورت داینامیک
        [HttpGet]
        public async Task<IActionResult> GetProjectIssueTypes(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == projectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Json(new { success = false, message = "شما به این پروژه دسترسی ندارید." });
            }

            // دریافت ProjectIssueTypes برای این پروژه
            var projectIssueTypes = await _context.ProjectIssueTypes
                .Where(pit => pit.ProjectId == projectId)
                .OrderBy(pit => pit.Order)
                .Select(pit => new
                {
                    id = pit.Id,
                    name = pit.Name,
                    icon = pit.Icon,
                    color = pit.Color,
                    baseType = pit.BaseType.ToString(),
                    level = pit.Level.ToString(),
                    canAddToSprint = pit.CanAddToSprint,
                    canHaveChildren = pit.CanHaveChildren,
                    isCustom = pit.IsCustom,
                    order = pit.Order
                })
                .ToListAsync();

            // اگر ProjectIssueTypes وجود ندارد، آن‌ها را ایجاد کن
            if (!projectIssueTypes.Any())
            {
                try
                {
                    var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);
                    if (project != null)
                    {
                        var now = DateTime.UtcNow;
                        var creatorUserId = project.CreatorUserId ?? userId;

                        // فقط سه نوع اولیه: Epic, Task, Subtask
                        var defaults = new List<ProjectIssueType>
                        {
                            new ProjectIssueType
                            {
                                ProjectId = projectId,
                                Name = "ویژگی",
                                Description = "نوع پیش‌فرض ویژگی",
                                Icon = "📦",
                                Color = "#8B5CF6",
                                Order = 0,
                                BaseType = IssueType.Epic,
                                IsCustom = false,
                                CanAddToSprint = false,
                                CanHaveChildren = true,
                                IncludeInReports = true,
                                Level = IssueTypeLevel.Epic,
                                CreatedByUserId = creatorUserId,
                                CreatedAt = now
                            },
                            new ProjectIssueType
                            {
                                ProjectId = projectId,
                                Name = "کار",
                                Description = "نوع پیش‌فرض کار",
                                Icon = "✅",
                                Color = "#3B82F6",
                                Order = 1,
                                BaseType = IssueType.Task,
                                IsCustom = false,
                                CanAddToSprint = true,
                                CanHaveChildren = true,
                                IncludeInReports = true,
                                Level = IssueTypeLevel.StoryLevel,
                                CreatedByUserId = creatorUserId,
                                CreatedAt = now
                            },
                            new ProjectIssueType
                            {
                                ProjectId = projectId,
                                Name = "کارک",
                                Description = "نوع پیش‌فرض کارک",
                                Icon = "🔹",
                                Color = "#06B6D4",
                                Order = 2,
                                BaseType = IssueType.Subtask,
                                IsCustom = false,
                                CanAddToSprint = false,
                                CanHaveChildren = false,
                                IncludeInReports = true,
                                Level = IssueTypeLevel.Subtask,
                                CreatedByUserId = creatorUserId,
                                CreatedAt = now
                            }
                        };

                        _context.ProjectIssueTypes.AddRange(defaults);
                        await _context.SaveChangesAsync();

                        // بارگذاری مجدد
                        projectIssueTypes = await _context.ProjectIssueTypes
                            .Where(pit => pit.ProjectId == projectId)
                            .OrderBy(pit => pit.Order)
                            .Select(pit => new
                            {
                                id = pit.Id,
                                name = pit.Name,
                                icon = pit.Icon,
                                color = pit.Color,
                                baseType = pit.BaseType.ToString(),
                                level = pit.Level.ToString(),
                                canAddToSprint = pit.CanAddToSprint,
                                canHaveChildren = pit.CanHaveChildren,
                                isCustom = pit.IsCustom,
                                order = pit.Order
                            })
                            .ToListAsync();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[GetProjectIssueTypes] ERROR: {ex.Message}");
                    return Json(new { success = false, message = "خطا در دریافت انواع تسک: " + ex.Message });
                }
            }

            return Json(new { success = true, issueTypes = projectIssueTypes });
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

            var task = await _context.TaskItems
                .Include(t => t.ProjectIssueType)
                .FirstOrDefaultAsync(t => t.Id == taskId);
            if (task == null)
            {
                return Json(new { success = false, message = "تسک یافت نشد." });
            }

            // بررسی اینکه آیا این Issue می‌تواند به Sprint اضافه شود
            if (!task.CanAddToSprint)
            {
                return Json(new { success = false, message = "این نوع Issue قابل اضافه شدن به اسپرینت نیست. فقط Story-level types قابل اضافه شدن هستند." });
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

            // به‌روزرسانی UpdatedAt کارت تا در ابتدای لیست قرار بگیرد
            task.UpdatedAt = DateTime.UtcNow;

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
                await _context.Database.ExecuteSqlRawAsync(
                 "UPDATE TaskItems SET SprintId = NULL WHERE Id = {0}",
                 taskId);

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
                return Json(new
                {
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

        // 📌 ایجاد تسک سریع و افزودن به اسپرینت
        [HttpPost]
        public async Task<IActionResult> CreateQuickTask([FromBody] JsonElement data)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!data.TryGetProperty("sprintId", out var sprintIdProp) || !data.TryGetProperty("title", out var titleProp))
            {
                return Json(new { success = false, message = "اسپرینت و عنوان الزامی است." });
            }

            int sprintId = sprintIdProp.GetInt32();
            string title = titleProp.GetString();

            if (string.IsNullOrWhiteSpace(title))
            {
                return Json(new { success = false, message = "عنوان الزامی است." });
            }

            var sprint = await _context.Sprints
                .Include(s => s.Project)
                .FirstOrDefaultAsync(s => s.Id == sprintId);

            if (sprint == null)
                return Json(new { success = false, message = "اسپرینت یافت نشد." });

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == sprint.ProjectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Json(new { success = false, message = "شما به این اسپرینت دسترسی ندارید." });
            }

            // فقط اسپرینت فعال می‌تواند تسک جدید داشته باشد
            if (sprint.Status != SprintStatus.Active)
            {
                return Json(new { success = false, message = "فقط اسپرینت فعال می‌تواند تسک جدید داشته باشد." });
            }

            var projectInfo = await _context.Projects
                .Where(p => p.Id == sprint.ProjectId)
                .Select(p => new { p.IssueKeyPrefix })
                .AsNoTracking()
                .FirstOrDefaultAsync();

            if (projectInfo == null)
                return Json(new { success = false, message = "پروژه یافت نشد." });

            // دریافت categoryId (اختیاری)
            int? categoryId = null;
            if (data.TryGetProperty("categoryId", out var catProp) && catProp.ValueKind == JsonValueKind.Number)
            {
                categoryId = catProp.GetInt32();
                if (categoryId <= 0) categoryId = null;
            }

            // دریافت IssueType (پیش‌فرض: Task)
            IssueType issueType = IssueType.Task;
            if (data.TryGetProperty("issueType", out var issueTypeProp) && issueTypeProp.ValueKind == JsonValueKind.Number)
                issueType = (IssueType)issueTypeProp.GetInt32();

            WorkflowStatus? targetStatus = null;
            if (data.TryGetProperty("statusId", out var statusProp) && statusProp.ValueKind == JsonValueKind.Number)
            {
                var requestedStatusId = statusProp.GetInt32();
                targetStatus = await _context.WorkflowStatuses
                    .FirstOrDefaultAsync(ws => ws.Id == requestedStatusId && ws.SprintId == sprintId);
            }

            if (targetStatus == null)
                targetStatus = await _context.WorkflowStatuses
                    .Where(ws => ws.SprintId == sprintId)
                    .OrderBy(ws => ws.IsDefault ? 0 : ws.Order)
                    .FirstOrDefaultAsync();

            var targetStatusId = targetStatus?.Id;
            var targetStatusName = targetStatus?.Name;
            var isFinalStatus = targetStatus?.IsFinal == true;

            // دریافت ProjectIssueTypeId اگر ارسال شده باشد
            int? projectIssueTypeId = null;
            if (data.TryGetProperty("projectIssueTypeId", out var pitProp) && pitProp.ValueKind == JsonValueKind.Number)
            {
                var pitId = pitProp.GetInt32();
                var projectIssueType = await _context.ProjectIssueTypes
                    .FirstOrDefaultAsync(pit => pit.Id == pitId && pit.ProjectId == sprint.ProjectId);
                if (projectIssueType != null)
                {
                    projectIssueTypeId = projectIssueType.Id;
                    issueType = projectIssueType.BaseType;
                }
            }

            // تسک را ابتدا بدون IssueKey ذخیره می‌کنیم تا Id تولید شود، بعد IssueKey = Prefix-Id (همیشه یکتا)
            var newTask = new TaskItem
            {
                Title = title,
                Description = data.TryGetProperty("description", out var descProp) ? descProp.GetString() : null,
                IssueType = issueType,
                ProjectIssueTypeId = projectIssueTypeId,
                IssueKey = null,
                StartDate = DateTime.Today,
                DueDate = null,
                ProjectId = sprint.ProjectId,
                CategoryId = categoryId,
                AssignedUserId = data.TryGetProperty("assignedUserId", out var assignedProp) && !string.IsNullOrWhiteSpace(assignedProp.GetString()) ? assignedProp.GetString() : null,
                StoryPoints = data.TryGetProperty("storyPoints", out var spProp) && spProp.ValueKind == JsonValueKind.Number ? spProp.GetInt32() : null,
                StatusId = targetStatusId,
                WorkflowStatusId = targetStatusId,
                IsCompleted = isFinalStatus,
                SprintId = sprintId,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.TaskItems.Add(newTask);
            await _context.SaveChangesAsync();

            newTask.IssueKey = $"{projectInfo.IssueKeyPrefix}-{newTask.Id}";
            await _context.SaveChangesAsync();

            var sprintTask = new SprintTask
            {
                SprintId = sprintId,
                TaskId = newTask.Id,
                AddedAt = DateTime.UtcNow,
                AddedByUserId = userId,
                Status = isFinalStatus ? SprintTaskStatus.Completed : SprintTaskStatus.Pending,
                SprintPriority = (int)TaskPriority.Medium
            };

            _context.SprintTasks.Add(sprintTask);
            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                message = "تسک با موفقیت ایجاد و به اسپرینت اضافه شد.",
                taskId = newTask.Id,
                issueKey = newTask.IssueKey,
                statusId = targetStatusId,
                statusName = targetStatusName
            });
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
                .AnyAsync(s => s.ProjectId == vm.ProjectId && (s.Status == SprintStatus.Active || s.Status == SprintStatus.Planning));

            if (hasActiveSprint)
            {
                Console.WriteLine("[Create POST] Active sprint exists");
                TempData["Error"] = "فقط یک اسپرینت می‌تواند در هر پروژه فعال باشد. لطفاً اسپرینت فعال یا در حال برنامه ریزی را مدیریت کنید.";
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