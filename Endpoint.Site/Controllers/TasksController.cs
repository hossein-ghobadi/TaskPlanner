using System;
using DNTPersianUtils.Core;
using Endpoint.Site.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskPlanner.Persistence.Contexts;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TaskPlanner.Domain.Entities.Users;
using System.Security.Claims;
using System.Text.Json;
using System.Linq;
using TaskPlanner.Application.Services.FileUpload;
using TaskPlanner.Application.Services.NotificationService;
using TaskPlanner.Domain.Entities.TaskPlanner;
using Microsoft.AspNetCore.SignalR;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class TasksController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IFileUploadService _fileUploadService;
        private readonly INotificationService _notificationService;

        public TasksController(MVPTestDatabaseContext context, UserManager<User> userManager, IFileUploadService fileUploadService, INotificationService notificationService)
        {
            _context = context;
            _userManager = userManager;
            _fileUploadService = fileUploadService;
            _notificationService = notificationService;

        }


        // GET: لیست تسک‌ها
        [Authorize]
        public async Task<IActionResult> Index(int? projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userProjectIds = await _context.Projects
                .Where(p => p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId))
                .Select(p => p.Id)
                .ToListAsync();

            // اگر projectId داده شده، دسترسی را بررسی و فیلتر کن
            Project? selectedProject = null;
            if (projectId.HasValue)
            {
                if (!userProjectIds.Contains(projectId.Value))
                {
                    return Forbid();
                }
                userProjectIds = new List<int> { projectId.Value };
                
                // دریافت اطلاعات پروژه برای نمایش نام
                selectedProject = await _context.Projects
                    .FirstOrDefaultAsync(p => p.Id == projectId.Value);
            }

            // ساخت شرط فیلتر: اگر projectId مشخص است، فقط کارهای همان پروژه را بگیر
            var tasksQuery = _context.TaskItems
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.Sprint)
                .Include(t => t.WorkflowStatus)
                .Include(t => t.ProjectIssueType)
                .Include(t => t.ChildIssues)
                    .ThenInclude(st => st.ProjectIssueType)
                .Include(t => t.ChildIssues)
                    .ThenInclude(st => st.ChildIssues)
                .AsQueryable();

            if (projectId.HasValue)
            {
                // اگر projectId مشخص است، فقط کارهای همان پروژه را بگیر
                tasksQuery = tasksQuery.Where(t => t.ProjectId == projectId.Value);
            }
            else
            {
                // در غیر این صورت، کارهای همه پروژه‌های کاربر یا کارهای اختصاص داده شده به کاربر
                tasksQuery = tasksQuery.Where(t => userProjectIds.Contains(t.ProjectId) || t.AssignedUserId == userId);
            }

            var tasks = await tasksQuery
                .OrderBy(t => t.StartDate)
                .ToListAsync();

            var assignedUserIds = tasks
                .Where(t => t.AssignedUserId != null)
                .Select(t => t.AssignedUserId)
                .Distinct()
                .ToList();

            var userLookup = await _userManager.Users
                .Where(u => assignedUserIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => $"{u.FullName ?? u.UserName} ({u.Phone})"
                );

            ViewBag.UserLookup = userLookup;
            ViewBag.Categories = await _context.TaskCategories
                .Where(c => userProjectIds.Contains(c.ProjectId))
                .OrderBy(c => c.Name)
                .ToListAsync();

            // اضافه کردن ProjectIssueTypes برای استفاده در مودال
            if (projectId.HasValue)
            {
                ViewBag.ProjectIssueTypes = await _context.ProjectIssueTypes
                    .Where(pit => pit.ProjectId == projectId.Value)
                    .OrderBy(pit => pit.Order)
                    .ToListAsync();
            }
            else
            {
                // اگر projectId مشخص نیست، همه ProjectIssueTypes را بگیر
                ViewBag.ProjectIssueTypes = await _context.ProjectIssueTypes
                    .Where(pit => userProjectIds.Contains(pit.ProjectId))
                    .OrderBy(pit => pit.Order)
                    .ToListAsync();
            }

            // ارسال اطلاعات پروژه به ویو
            ViewBag.SelectedProjectId = projectId;
            ViewBag.SelectedProjectName = selectedProject?.Name;

            // پیدا کردن اسپرینت فعال برای پروژه (اگر projectId مشخص باشد)
            if (projectId.HasValue)
            {
                var activeSprint = await _context.Sprints
                    .FirstOrDefaultAsync(s => s.ProjectId == projectId.Value && s.Status == SprintStatus.Active);
                ViewBag.ActiveSprintId = activeSprint?.Id;
                ViewBag.ActiveSprintName = activeSprint?.Name;
            }
            else
            {
                ViewBag.ActiveSprintId = null;
                ViewBag.ActiveSprintName = null;
            }

            return View(tasks);
        }

        [Authorize]
        public async Task<IActionResult> Completed(int? projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userProjectIds = await _context.Projects
                .Where(p => p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId))
                .Select(p => p.Id)
                .ToListAsync();

            if (projectId.HasValue)
            {
                if (!userProjectIds.Contains(projectId.Value))
                {
                    return Forbid();
                }
                userProjectIds = new List<int> { projectId.Value };
            }

            var tasks = await _context.TaskItems
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.Sprint)
                .Include(t => t.WorkflowStatus)
                .Include(t => t.ProjectIssueType)
                .Include(t => t.ChildIssues)
                    .ThenInclude(st => st.ProjectIssueType)
                .Include(t => t.ChildIssues)
                    .ThenInclude(st => st.ChildIssues)
                .Where(t => (userProjectIds.Contains(t.ProjectId) || t.AssignedUserId == userId) && t.IsCompleted)
                .OrderBy(t => t.StartDate)
                .ToListAsync();

            var assignedUserIds = tasks
                .Where(t => t.AssignedUserId != null)
                .Select(t => t.AssignedUserId)
                .Distinct()
                .ToList();

            var userLookup = await _userManager.Users
                .Where(u => assignedUserIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => $"{u.FullName ?? u.UserName} ({u.Phone})"
                );

            ViewBag.UserLookup = userLookup;
            ViewBag.Categories = await _context.TaskCategories
                .Where(c => userProjectIds.Contains(c.ProjectId))
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.SelectedProjectId = projectId;

            return View(tasks);
        }

        /// <summary>
        /// افزودن Subtask از طریق دکمه "زیرتسک"
        /// با رعایت محدودیت‌های سلسله مراتبی Jira
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddSubTask([FromBody] JsonElement data)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            string title = data.GetProperty("title").GetString();
            int parentId = data.GetProperty("parentId").GetInt32();
            int categoryId = data.GetProperty("categoryId").GetInt32();
            Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>> 1");
            if (string.IsNullOrWhiteSpace(title))
                return BadRequest("عنوان الزامی است.");

            // دریافت Parent Issue
            var parent = await _context.TaskItems
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == parentId);

            if (parent == null)
                return NotFound("Parent Issue یافت نشد.");

            // 🔒 Validation: بررسی سلسله مراتبی Jira
            // فقط Story, Task, Bug می‌تونن Subtask داشته باشن
            // Epic و Subtask نمی‌تونن Subtask داشته باشن
            if (!parent.CanHaveChildren)
            {
                return BadRequest($"❌ {parent.IssueType.GetDisplayName()} نمی‌تواند Subtask داشته باشد. " +
                    "فقط Story، Task و Bug می‌توانند Subtask داشته باشند.");
            }
            Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>> 2");

            // بررسی دقیق‌تر با Validator
            var validationResult = IssueHierarchyValidator.ValidateParentChild(
                IssueType.Subtask,
                parent.IssueType
            );

            if (!validationResult.IsValid)
            {
                return BadRequest($"❌ {string.Join(" ", validationResult.Errors)}");
            }

            // 🔒 بررسی دسترسی کاربر به پروژه
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == parent.ProjectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));
            Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>> 3");

            if (!hasAccess)
            {
                return Forbid();
            }

            TaskCategory selectedCategory = null;
            if (categoryId > 0)
            {
                selectedCategory = await _context.TaskCategories
                    .FirstOrDefaultAsync(c => c.Id == categoryId && c.ProjectId == parent.ProjectId);

                if (selectedCategory == null)
                {
                    return BadRequest("دسته‌بندی انتخاب‌شده متعلق به این پروژه نیست.");
                }
            }

            // 📝 Generate کردن IssueKey
            var project = parent.Project;
            if (project == null)
            {
                project = await _context.Projects.FindAsync(parent.ProjectId);
            }

            // 🔄 Retry mechanism برای جلوگیری از race condition در IssueKey
            int maxRetries = 5;
            TaskItem subTask = null;
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    // حذف project از change tracker و دریافت مجدد از دیتابیس
                    _context.Entry(project).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    project = await _context.Projects.FindAsync(parent.ProjectId);
                    
                    if (project == null)
                    {
                        return BadRequest("❌ پروژه یافت نشد.");
                    }
                    
                    // بررسی اینکه آیا IssueKey تولید شده تکراری است یا نه
                    string generatedIssueKey = project.GenerateNextIssueKey();
                    bool keyExists = await _context.TaskItems
                        .AnyAsync(t => t.IssueKey == generatedIssueKey);
                    
                    if (keyExists)
                    {
                        // اگر key تکراری بود، LastIssueNumber را افزایش می‌دهیم و دوباره تلاش می‌کنیم
                        project.LastIssueNumber++;
                        generatedIssueKey = project.GenerateNextIssueKey();
                    }

                    subTask = new TaskItem
                    {
                        Title = title,
                        Description = null,
                        IssueType = IssueType.Subtask, // ✅ به صورت خودکار Subtask
                        IssueKey = generatedIssueKey, // ✅ IssueKey اتوماتیک
                        StartDate = DateTime.Today,
                        DueDate = parent.DueDate, // وراثت DueDate از parent
                        ProjectId = parent.ProjectId,
                        CategoryId = selectedCategory?.Id ?? parent.CategoryId, // اگه category نداد، از parent بگیر
                        ParentTaskId = parent.Id,
                        AssignedUserId = parent.AssignedUserId, // وراثت مسئول از parent
                        IsCompleted = false,
                        CreatedByUserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };
                    Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>> 4");

                    if (parent.IsCompleted)
                    {
                        parent.IsCompleted = false;
                        parent.UpdatedAt = DateTime.UtcNow;
                    }
                    await MarkAncestorsIncompleteAsync(parent.ParentTaskId);
                    Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>> 5");

                    _context.TaskItems.Add(subTask);
                    _context.Projects.Update(project); // به‌روزرسانی LastIssueNumber
                    Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>> 6");

                    await _context.SaveChangesAsync();
                    Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>> 7");

                    break; // موفقیت‌آمیز بود، خارج شو
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx &&
                                                                               sqlEx.Number == 2601 && // Duplicate key error
                                                                               retry < maxRetries - 1)
                {
                    // اگر IssueKey تکراری بود، دوباره تلاش کن
                    if (subTask != null)
                    {
                        _context.Entry(subTask).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                    _context.Entry(project).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    await Task.Delay(50 * (retry + 1)); // delay افزایشی برای retry بعدی
                    continue; // دوباره تلاش کن
                }
            }

            if (subTask == null)
            {
                return BadRequest("❌ خطا در ایجاد Subtask. لطفاً دوباره تلاش کنید.");
            }

            return Ok(new
            {
                success = true,
                message = $"✅ Subtask '{subTask.Title}' با IssueKey '{subTask.IssueKey}' ایجاد شد.",
                issueKey = subTask.IssueKey,
                issueId = subTask.Id
            });
        }

        /// <summary>
        /// 🎯 متد هوشمند برای افزودن Child Issue (Smart based on parent type)
        /// - اگه Parent = Epic → باید IssueType مشخص بشه (Story/Task/Bug)
        /// - اگه Parent = Story/Task/Bug → خودکار IssueType = Subtask
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> AddChildIssue([FromBody] JsonElement data)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 📝 دریافت داده‌ها
            string title = data.GetProperty("title").GetString();
            int parentId = data.GetProperty("parentId").GetInt32();
            int categoryId = data.TryGetProperty("categoryId", out var catProp) ? catProp.GetInt32() : 0;

            // IssueType اختیاری: اگه ارسال نشد → خودکار Subtask
            IssueType? issueType = null;
            if (data.TryGetProperty("issueType", out var issueTypeProp))
            {
                issueType = (IssueType)issueTypeProp.GetInt32();
            }

            // StoryPoints اختیاری
            int? storyPoints = null;
            if (data.TryGetProperty("storyPoints", out var spProp) && spProp.ValueKind == JsonValueKind.Number)
            {
                storyPoints = spProp.GetInt32();
            }

            // 🔍 Validation: عنوان الزامی
            if (string.IsNullOrWhiteSpace(title))
                return BadRequest("عنوان الزامی است.");

            // 🔍 دریافت Parent Issue با ProjectIssueType
            var parent = await _context.TaskItems
                .Include(t => t.Project)
                .Include(t => t.ProjectIssueType)
                .FirstOrDefaultAsync(t => t.Id == parentId);

            if (parent == null)
                return NotFound("Parent Issue یافت نشد.");

            TaskCategory selectedCategory = null;
            if (categoryId > 0)
            {
                selectedCategory = await _context.TaskCategories
                    .FirstOrDefaultAsync(c => c.Id == categoryId && c.ProjectId == parent.ProjectId);

                if (selectedCategory == null)
                {
                    return BadRequest("دسته‌بندی انتخاب‌شده متعلق به این پروژه نیست.");
                }
            }

            // 🎯 Smart Logic: تعیین IssueType بر اساس Parent Level
            IssueType finalIssueType;
            int? projectIssueTypeId = null;
            var parentLevel = parent.ProjectIssueType?.Level ??
                (parent.IssueType == IssueType.Epic ? IssueTypeLevel.Epic :
                 parent.IssueType == IssueType.Subtask ? IssueTypeLevel.Subtask :
                 IssueTypeLevel.StoryLevel);

            if (parentLevel == IssueTypeLevel.Epic)
            {
                // اگه Parent = Epic → باید Story-level type مشخص شده باشد
                if (!issueType.HasValue)
                {
                    return BadRequest("برای افزودن Issue به Epic، باید نوع Issue (Story-level) مشخص شود.");
                }

                // باید Story-level type باشد (Task یا انواع سفارشی)
                // پیدا کردن ProjectIssueType مربوطه
                if (data.TryGetProperty("projectIssueTypeId", out var pitProp) && pitProp.ValueKind == JsonValueKind.Number)
                {
                    var pitId = pitProp.GetInt32();
                    var projectIssueType = await _context.ProjectIssueTypes
                        .FirstOrDefaultAsync(pit => pit.Id == pitId &&
                            pit.ProjectId == parent.ProjectId &&
                            pit.Level == IssueTypeLevel.StoryLevel);

                    if (projectIssueType != null)
                    {
                        projectIssueTypeId = projectIssueType.Id;
                        finalIssueType = projectIssueType.BaseType;
                    }
                    else
                    {
                        // اگر ProjectIssueType پیدا نشد، از IssueType استفاده کن
                        finalIssueType = issueType.Value;
                    }
                }
                else
                {
                    // اگر ProjectIssueTypeId ارسال نشد، از IssueType استفاده کن
                    finalIssueType = issueType.Value;

                    // پیدا کردن ProjectIssueType مربوطه بر اساس BaseType
                    var projectIssueType = await _context.ProjectIssueTypes
                        .FirstOrDefaultAsync(pit => pit.ProjectId == parent.ProjectId &&
                            pit.BaseType == finalIssueType &&
                            pit.Level == IssueTypeLevel.StoryLevel);

                    if (projectIssueType != null)
                    {
                        projectIssueTypeId = projectIssueType.Id;
                    }
                }

                // بررسی اینکه نوع انتخاب شده Story-level است
                if (finalIssueType == IssueType.Epic || finalIssueType == IssueType.Subtask)
                {
                    return BadRequest("Epic فقط می‌تواند Story-level types (Task یا انواع سفارشی) داشته باشد.");
                }
            }
            else if (parentLevel == IssueTypeLevel.StoryLevel)
            {
                // اگه Parent = Story-level → خودکار Subtask
                finalIssueType = IssueType.Subtask;

                // پیدا کردن ProjectIssueType برای Subtask
                var subtaskType = await _context.ProjectIssueTypes
                    .FirstOrDefaultAsync(pit => pit.ProjectId == parent.ProjectId &&
                        pit.Level == IssueTypeLevel.Subtask);

                if (subtaskType != null)
                {
                    projectIssueTypeId = subtaskType.Id;
                }
            }
            else
            {
                return BadRequest("نمی‌توان به این نوع Issue، child اضافه کرد.");
            }

            // 🔒 Validation: بررسی سلسله مراتبی بر اساس Level
            var childLevel = finalIssueType == IssueType.Subtask ? IssueTypeLevel.Subtask :
                            projectIssueTypeId.HasValue ? IssueTypeLevel.StoryLevel :
                            IssueTypeLevel.StoryLevel;

            var validationResult = IssueHierarchyValidator.ValidateParentChildByLevel(
                childLevel,
                parentLevel,
                finalIssueType.GetDisplayName(),
                parent.IssueTypeName
            );

            if (!validationResult.IsValid)
            {
                return BadRequest($"❌ {string.Join(" ", validationResult.Errors)}");
            }

            // 🔒 بررسی دسترسی کاربر به پروژه
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == parent.ProjectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Forbid();
            }

            // 📝 Generate کردن IssueKey
            var project = parent.Project;
            if (project == null)
            {
                project = await _context.Projects.FindAsync(parent.ProjectId);
            }

            // 🔄 Retry mechanism برای جلوگیری از race condition در IssueKey
            TaskItem newIssue = null;
            
                // حذف project از change tracker و دریافت مجدد از دیتابیس
                _context.Entry(project).State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                project = await _context.Projects.FindAsync(parent.ProjectId);

                if (project == null)
                {
                    return BadRequest("❌ پروژه یافت نشد.");
                }
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////                

                var projectForIssueKey = await _context.Projects.FindAsync(parent.ProjectId);
       
                //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////                

                // 🔄 Retry mechanism برای جلوگیری از race condition در IssueKey

                string generatedIssueKey = null;

               
                    // پاک کردن ChangeTracker برای project و newTask
                    if (projectForIssueKey != null)
                    {
                        var entry = _context.Entry(projectForIssueKey);
                        if (entry.State != Microsoft.EntityFrameworkCore.EntityState.Detached)
                        {
                            entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                    }

                 
                    // تولید IssueKey و بررسی تکراری بودن - تا زمانی که IssueKey منحصر به فرد باشد
                    int keyGenerationAttempts = 0;
                    do
                    {
                        generatedIssueKey = projectForIssueKey.GenerateNextIssueKey();
                        bool keyExists = await _context.TaskItems
                            .AnyAsync(t => t.IssueKey == generatedIssueKey);

                        if (keyExists)
                        {
                            // اگر key تکراری بود، LastIssueNumber را افزایش می‌دهیم
                            projectForIssueKey.LastIssueNumber++;
                            keyGenerationAttempts++;

                        }
                        else
                        {
                            break; // IssueKey منحصر به فرد است
                        }
                    } while (true);

                    // 🏗️ ساخت Issue جدید
                    newIssue = new TaskItem
                    {
                        Title = title,
                        Description = null,
                        IssueType = finalIssueType,
                        ProjectIssueTypeId = projectIssueTypeId,
                        IssueKey = generatedIssueKey,
                        StartDate = DateTime.Today,
                        DueDate = parent.DueDate, // وراثت DueDate از parent
                        ProjectId = parent.ProjectId,
                        CategoryId = selectedCategory?.Id ?? parent.CategoryId,
                        ParentTaskId = parent.Id,
                        AssignedUserId = parent.AssignedUserId, // وراثت مسئول از parent
                        StoryPoints = storyPoints, // فقط برای Story-level types
                        IsCompleted = false,
                        CreatedByUserId = userId,
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow
                    };

                    if (parent.IsCompleted)
                    {
                        parent.IsCompleted = false;
                        parent.UpdatedAt = DateTime.UtcNow;
                    }
                    await MarkAncestorsIncompleteAsync(parent.ParentTaskId);
                    Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>> 11");

                    _context.TaskItems.Add(newIssue);
                    _context.Projects.Update(project); // به‌روزرسانی LastIssueNumber

                    await _context.SaveChangesAsync();
                    Console.WriteLine(">>>>>>>>>>>>>>>>>>>>>>>>>>>>>> 12");
                    
                  
                
            return Ok(new
            {
                success = true,
                message = $"✅ {newIssue.IssueTypeName} '{newIssue.Title}' با IssueKey '{newIssue.IssueKey}' ایجاد شد.",
                issueKey = newIssue.IssueKey,
                issueId = newIssue.Id,
                issueType = newIssue.IssueTypeName
            });
        }

                
        


        [HttpPost]
        public async Task<IActionResult> DeleteTask([FromBody] JsonElement data)
        {
            int taskId = data.GetProperty("taskId").GetInt32();

            var task = await _context.TaskItems
                .Include(t => t.ChildIssues)
                .ThenInclude(st => st.ChildIssues)
                .Include(t => t.SprintTasks)
                .Include(t => t.StatusHistory)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return Json(new { success = false, message = "تسک یافت نشد." });

            try
            {
                // 🔹 تابع بازگشتی برای حذف Child Issues
                async Task DeleteRecursiveAsync(TaskItem item)
                {
                    // حذف زیرتسک‌ها به صورت بازگشتی
                    var childIssues = await _context.TaskItems
                        .Include(c => c.SprintTasks)
                        .Include(c => c.StatusHistory)
                        .Where(c => c.ParentTaskId == item.Id)
                        .ToListAsync();

                    foreach (var child in childIssues)
                    {
                        await DeleteRecursiveAsync(child);
                    }

                    // حذف رکوردهای SprintTasks مربوط به این تسک
                    var sprintTasks = await _context.SprintTasks
                        .Where(st => st.TaskId == item.Id)
                        .ToListAsync();
                    if (sprintTasks.Any())
                    {
                        _context.SprintTasks.RemoveRange(sprintTasks);
                    }

                    // حذف تاریخچه وضعیت‌ها
                    var statusHistories = await _context.IssueStatusHistories
                        .Where(h => h.TaskId == item.Id)
                        .ToListAsync();
                    if (statusHistories.Any())
                    {
                        _context.IssueStatusHistories.RemoveRange(statusHistories);
                    }

                    // حذف کامنت‌ها
                    var comments = await _context.TaskComments
                        .Where(c => c.TaskId == item.Id)
                        .ToListAsync();
                    if (comments.Any())
                    {
                        _context.TaskComments.RemoveRange(comments);
                    }

                    // حذف خود تسک
                    _context.TaskItems.Remove(item);
                }

                await DeleteRecursiveAsync(task);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "تسک با موفقیت حذف شد." });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "خطا در حذف تسک: " + ex.Message });
            }
        }

        /// <summary>
        /// خاتمه دادن به تسک و بروزرسانی وضعیت والدین آن
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> CompleteTask([FromBody] JsonElement data)
        {
            if (!data.TryGetProperty("taskId", out var taskIdProp) || taskIdProp.ValueKind != JsonValueKind.Number)
            {
                return BadRequest("taskId الزامی است.");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int taskId = taskIdProp.GetInt32();

            var task = await _context.TaskItems
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
            {
                return NotFound("تسک یافت نشد.");
            }

            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == task.ProjectId &&
                               (p.CreatorUserId == userId ||
                                p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Forbid();
            }

            await MarkTaskAndDescendantsCompletedAsync(task);
            await _context.SaveChangesAsync();

            await UpdateParentCompletionStatusAsync(task.ParentTaskId);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = "تسک با موفقیت خاتمه یافت."
            });
        }




        [HttpPost]
        public async Task<IActionResult> ToggleCompleteSubTask(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _context.TaskItems
                
                .FirstOrDefaultAsync(t => t.Id == id);
            var taskparent= await _context.TaskItems
                .Include(t => t.ChildIssues)

                .FirstOrDefaultAsync(t => t.Id == task.ParentTaskId);
            var subtask = taskparent.ChildIssues.FirstOrDefault(t=>t.Id==id);
            var sprintTask = await _context.SprintTasks

                .FirstOrDefaultAsync(t => t.SprintId== taskparent.SprintId && t.TaskId== task.ParentTaskId);
            if (subtask == null)
                return Json(new { success = false, message = "کار یافت نشد" });

            // بررسی دسترسی
            if (subtask.Project.CreatorUserId != userId && !subtask.Project.Members.Any(m => m.UserId == userId))
                return Json(new { success = false, message = "دسترسی ندارید" });

            subtask.IsCompleted = !task.IsCompleted;
            subtask.UpdatedAt = DateTime.UtcNow;

            // اگر کار تکمیل شد، همه کارک‌ها رو هم تکمیل کن
            if (subtask.IsCompleted)
            {
                bool allCompleted = true;

                foreach (var sub in taskparent.ChildIssues)
                {
                    if (!sub.IsCompleted)
                    {
                        allCompleted = false;
                        break;
                    }
                }

                if (!allCompleted)
                {
                    // عملیات مورد نظر در صورت وجود زیرتکمیل نشده
                    
                    taskparent.IsCompleted = true;
                    taskparent.UpdatedAt = DateTime.UtcNow;
                    sprintTask.CompletedAt = DateTime.UtcNow;
                    subtask.UpdatedAt = DateTime.UtcNow;
                }
            }

            // اگر کار uncomplete شد، همه کارک‌ها رو هم uncomplete کن

            //_context.TaskItems.update(taskparent.);
            //await _context.TaskItems.update(subtask);
            //await _context.SprintTasks.update(sprintTask);

            await _context.SaveChangesAsync();

            return Json(new
            {
                success = true,
                isCompleted = task.IsCompleted,
                
            });
        }



        /// <summary>
        /// انتقال Issue بین وضعیت‌ها بر اساس WorkflowTransitions
        /// </summary>
        [HttpPost]
        public async Task<IActionResult> MoveIssue([FromBody] JsonElement data)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!data.TryGetProperty("taskId", out var taskIdProp) || taskIdProp.ValueKind != JsonValueKind.Number)
                return BadRequest("taskId الزامی است.");
            
            int taskId = taskIdProp.GetInt32();
            int? toStatusId = null;
            
            // toStatusId ممکن است null باشد (برای حذف وضعیت)
            if (data.TryGetProperty("toStatusId", out var toStatusProp))
            {
                if (toStatusProp.ValueKind == JsonValueKind.Number)
                {
                    toStatusId = toStatusProp.GetInt32();
                }
                else if (toStatusProp.ValueKind == JsonValueKind.Null)
                {
                    toStatusId = null;
                }
            }

            string? reason = data.TryGetProperty("reason", out var reasonProp) && reasonProp.ValueKind == JsonValueKind.String
                ? reasonProp.GetString()
                : null;

            var task = await _context.TaskItems
                .Include(t => t.Project)
                .Include(t => t.Status)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return NotFound("Issue یافت نشد.");

            // دسترسی به پروژه
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == task.ProjectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));
            if (!hasAccess)
                return Forbid();

            // 🔒 بررسی دسترسی: فقط مسئول کار و سازنده پروژه می‌توانند کارت را انتقال دهند
            var isAssignee = !string.IsNullOrEmpty(task.AssignedUserId) && 
                            string.Equals(task.AssignedUserId, userId, StringComparison.OrdinalIgnoreCase);
            var isProjectCreator = task.Project != null && 
                                  !string.IsNullOrEmpty(task.Project.CreatorUserId) &&
                                  string.Equals(task.Project.CreatorUserId, userId, StringComparison.OrdinalIgnoreCase);
            
            if (!isAssignee && !isProjectCreator)
            {
                return BadRequest("فقط مسئول کار و سازنده پروژه می‌توانند کارت را بین ستون‌ها انتقال دهند.");
            }

            int? fromStatusId = task.StatusId; // ممکن است null باشد

            // اگر toStatusId null است، وضعیت را حذف می‌کنیم
            WorkflowStatus? toStatus = null;
            if (toStatusId.HasValue)
            {
                // اگر تسک در یک اسپرینت است، باید از وضعیت‌های همان اسپرینت استفاده کند
                if (task.SprintId.HasValue)
                {
                    toStatus = await _context.WorkflowStatuses
                        .FirstOrDefaultAsync(ws => ws.Id == toStatusId.Value && ws.SprintId == task.SprintId.Value);
                }
                else
                {
                    // اگر تسک در اسپرینت نیست، از وضعیت‌های پروژه استفاده می‌کند (سازگاری با داده‌های قدیمی)
                    toStatus = await _context.WorkflowStatuses
                        .FirstOrDefaultAsync(ws => ws.Id == toStatusId.Value && ws.ProjectId == task.ProjectId && ws.SprintId == null);
                }
                
                if (toStatus == null)
                    return BadRequest("وضعیت مقصد معتبر نیست.");

                // اعتبارسنجی Transition فقط وقتی fromStatus داریم و toStatus داریم
                WorkflowTransition? usedTransition = null;
                if (fromStatusId.HasValue)
                {
                    // جلوگیری از بازگشت از Done به وضعیت‌های قبلی
                    var fromStatus = await _context.WorkflowStatuses.FirstOrDefaultAsync(ws => ws.Id == fromStatusId.Value);
                    if (fromStatus != null && fromStatus.Type == WorkflowType.Done && fromStatus.IsFinal && toStatus.Type != WorkflowType.Done)
                    {
                        return BadRequest("انتقال از وضعیت انجام شده به وضعیت‌های قبلی مجاز نیست.");
                    }

                    // اگر تسک در یک اسپرینت است، از وضعیت‌های sprint استفاده می‌کنیم و نیازی به بررسی transition نیست
                    // چون در Board همه انتقال‌ها مجاز هستند (drag & drop آزاد)
                    if (!task.SprintId.HasValue)
                    {
                        // فقط برای project-level statuses، transition را بررسی می‌کنیم
                        usedTransition = await _context.WorkflowTransitions
                            .FirstOrDefaultAsync(tr => tr.ProjectId == task.ProjectId
                                                    && tr.FromStatusId == fromStatusId.Value
                                                    && tr.ToStatusId == toStatusId.Value);
                        if (usedTransition == null)
                            return BadRequest("این انتقال در Workflow پروژه مجاز نیست.");

                        if (usedTransition.OnlyAssigneeCanTransition && task.AssignedUserId != userId)
                            return BadRequest("فقط مسئول Issue می‌تواند این انتقال را انجام دهد.");
                    }
                    // برای sprint-level statuses، انتقال آزاد است (drag & drop)
                }
            }

            // بروزرسانی وضعیت Issue
            task.StatusId = toStatus?.Id;
            task.UpdatedAt = DateTime.UtcNow;
            if (toStatus != null && toStatus.IsFinal)
            {
                task.IsCompleted = true;
            }
            else if (toStatus == null || !toStatus.IsFinal)
            {
                // اگر وضعیت نهایی نیست، IsCompleted را false کن (مگر اینکه از قبل true باشد و بخواهیم حفظ کنیم)
                // برای ساده‌تر کردن، فقط وقتی به وضعیت نهایی می‌رود IsCompleted را true می‌کنیم
            }

            // ثبت تاریخچه (فقط اگر toStatus null نباشد، چون ToStatusId نمی‌تواند null باشد)
            if (toStatus != null)
            {
                var history = new IssueStatusHistory
                {
                    TaskId = task.Id,
                    FromStatusId = fromStatusId,
                    ToStatusId = toStatus.Id,
                    TransitionId = null, // وقتی null است یا transition نداریم
                    ChangedByUserId = userId,
                    ChangeReason = reason,
                    ChangedAt = DateTime.UtcNow
                };
                _context.IssueStatusHistories.Add(history);
            }
            
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = toStatus != null 
                    ? $"وضعیت Issue به '{toStatus.Name}' تغییر کرد."
                    : "وضعیت Issue حذف شد.",
                toStatusId = toStatus?.Id,
                toStatusName = toStatus?.Name,
                isCompleted = task.IsCompleted
            });
        }

        // 📅 برنامه هفتگی کاربر
        public async Task<IActionResult> Weekly(int? offset, int? projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 📅 محاسبه دقیق هفته جاری با شروع شنبه
            var weekOffset = offset ?? 0;
            var today = DateTime.Today;
            int daysSinceSaturday = ((int)today.DayOfWeek + 1) % 7;
            var startOfWeek = today.AddDays(-daysSinceSaturday).AddDays(7 * weekOffset);
            var endOfWeek = startOfWeek.AddDays(6);

            // 🎯 فقط پروژه‌های کاربر فعلی
            var userProjectIds = await _context.Projects
                .Where(p => p.CreatorUserId == userId ||
                            p.Members.Any(m => m.UserId == userId))
                .Select(p => p.Id)
                .ToListAsync();

            // اگر projectId داده شده، دسترسی را بررسی و فیلتر کن
            if (projectId.HasValue)
            {
                if (!userProjectIds.Contains(projectId.Value))
                {
                    TempData["Error"] = "شما دسترسی به این پروژه ندارید.";
                    return RedirectToAction(nameof(Weekly), new { offset = weekOffset });
                }
                userProjectIds = new List<int> { projectId.Value };
            }

            // 📋 تسک‌های همان پروژه‌ها (بدون Epic)
            var weeklyTasks = await _context.TaskItems
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.ChildIssues)
                .Where(t =>
                    userProjectIds.Contains(t.ProjectId) &&
                    t.IssueType != IssueType.Epic &&
                    t.StartDate.Date >= startOfWeek &&
                    t.StartDate.Date <= endOfWeek)
                .OrderBy(t => t.StartDate)
                .ToListAsync();

            // 📌 لیست آیدی‌های کاربرانی که به‌عنوان مسئول ثبت شده‌اند
            var assignedUserIds = weeklyTasks
                .Where(t => t.AssignedUserId != null)
                .Select(t => t.AssignedUserId)
                .Distinct()
                .ToList();

            // 📌 ساخت Lookup از نام کاربران
            var userLookup = await _userManager.Users
                .Where(u => assignedUserIds.Contains(u.Id))
                .ToDictionaryAsync(
                    u => u.Id,
                    u => $"{u.FullName ?? u.UserName} ({u.Phone})"
                );

            // 📌 دریافت لیست پروژه‌های کاربر برای فیلتر
            var userProjects = await _context.Projects
                .Where(p => p.CreatorUserId == userId ||
                            p.Members.Any(m => m.UserId == userId))
                .OrderBy(p => p.Name)
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();

            ViewBag.UserLookup = userLookup;
            ViewBag.StartOfWeek = startOfWeek;
            ViewBag.EndOfWeek = endOfWeek;
            ViewBag.Offset = weekOffset;
            ViewBag.UserProjects = userProjects;
            ViewBag.SelectedProjectId = projectId;

            return View(weeklyTasks);
        }
        // 🔄 دریافت لیست اعضای پروژه برای AJAX
        [HttpGet]
        public async Task<IActionResult> GetProjectMembers(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // بررسی دسترسی کاربر به پروژه
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == projectId &&
                    (p.CreatorUserId == userId || 
                     p.Members.Any(m => m.UserId == userId) ||
                     _context.ProjectInvitations.Any(i => i.ProjectId == projectId && i.InviteeId == userId && i.Status == InvitationStatus.Accepted)));
            
            if (!hasAccess)
            {
                return Forbid();
            }

            var assignedUsers = new List<dynamic>();
            var project = await _context.Projects.FindAsync(projectId);
            
            // اضافه کردن سازنده پروژه
            if (project != null && !string.IsNullOrEmpty(project.CreatorUserId))
            {
                var creatorInfo = await _userManager.FindByIdAsync(project.CreatorUserId);
                if (creatorInfo != null)
                {
                    assignedUsers.Add(new
                    {
                        Id = project.CreatorUserId,
                        Name = $"{creatorInfo.FullName ?? creatorInfo.UserName} ({creatorInfo.Phone}) - سازنده پروژه"
                    });
                }
            }

            // دریافت اعضای فعال پروژه از جدول ProjectMembers
            var activeProjectMembers = await _context.ProjectMembers
                .Where(m => m.ProjectId == projectId)
                .Select(m => m.UserId)
                .ToListAsync();

            foreach (var memberId in activeProjectMembers)
            {
                // جلوگیری از اضافه کردن مجدد سازنده پروژه
                if (project != null && memberId == project.CreatorUserId) continue;

                var userInfo = await _userManager.FindByIdAsync(memberId);
                if (userInfo != null)
                {
                    assignedUsers.Add(new
                    {
                        Id = memberId,
                        Name = $"{userInfo.FullName ?? userInfo.UserName} ({userInfo.Phone})"
                    });
                }
            }

            // خود کاربر لاگین‌شده نیز باید در لیست باشد
            var currentUser = await _userManager.FindByIdAsync(userId);
            if (currentUser != null && !assignedUsers.Any(u => u.Id == userId))
            {
                assignedUsers.Add(new
                {
                    Id = userId,
                    Name = $"{currentUser.FullName ?? currentUser.UserName} ({currentUser.Phone})"
                });
            }

            return Json(assignedUsers.OrderBy(u => u.Name).ToList());
        }

        [HttpGet]
        public async Task<IActionResult> Create(int? projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // ✅ projectId اجباری است - کار فقط از داخل پروژه ایجاد می‌شود
            if (!projectId.HasValue)
            {
                TempData["Error"] = "برای ایجاد کار باید از داخل یک پروژه اقدام کنید.";
                return RedirectToAction("Index", "Projects");
            }

            // 🔒 بررسی دسترسی کاربر به پروژه
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId.Value &&
                    (p.CreatorUserId == userId ||
                     p.Members.Any(m => m.UserId == userId) ||
                     _context.ProjectInvitations.Any(i => i.ProjectId == p.Id && i.InviteeId == userId && i.Status == InvitationStatus.Accepted))
                );

            if (project == null)
            {
                TempData["Error"] = "پروژه یافت نشد یا شما به آن دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            // نام پروژه برای نمایش
            ViewBag.ProjectName = project.Name;

            // 📂 دسته‌بندی‌ها
            ViewBag.Categories = await _context.TaskCategories
                .Where(c => c.ProjectId == project.Id)
                .OrderBy(c => c.Name)
                .ToListAsync();

            // 🧩 تسک‌های این پروژه برای انتخاب Parent Task
            ViewBag.Tasks = await _context.TaskItems
                .Where(t => t.ProjectId == projectId.Value)
                .ToListAsync();

            var projectIssueTypes = await _context.ProjectIssueTypes
                .Where(pit => pit.ProjectId == project.Id)
                .OrderBy(pit => pit.Order)
                .ToListAsync();
            ViewBag.ProjectIssueTypes = projectIssueTypes;

            // 👤 کاربران قابل انتخاب برای AssignedUserId (فقط اعضای این پروژه)
            var assignedUsers = new List<dynamic>();
            
            // اضافه کردن سازنده پروژه
            if (!string.IsNullOrEmpty(project.CreatorUserId))
            {
                var creatorInfo = await _userManager.FindByIdAsync(project.CreatorUserId);
                if (creatorInfo != null)
                {
                    assignedUsers.Add(new
                    {
                        Id = project.CreatorUserId,
                        Name = $"{creatorInfo.FullName ?? creatorInfo.UserName} ({creatorInfo.Phone}) - سازنده پروژه"
                    });
                }
            }

            // دریافت اعضای فعال پروژه از جدول ProjectMembers
            var activeProjectMembers = await _context.ProjectMembers
                .Where(m => m.ProjectId == projectId.Value)
                .Select(m => m.UserId)
                .ToListAsync();

            foreach (var memberId in activeProjectMembers)
            {
                // جلوگیری از اضافه کردن مجدد سازنده پروژه
                if (memberId == project.CreatorUserId) continue;

                var userInfo = await _userManager.FindByIdAsync(memberId);
                if (userInfo != null)
                {
                    assignedUsers.Add(new
                    {
                        Id = memberId,
                        Name = $"{userInfo.FullName ?? userInfo.UserName} ({userInfo.Phone})"
                    });
                }
            }

            // خود کاربر لاگین‌شده نیز باید در لیست باشد (اگر از قبل نیست)
            var currentUser = await _userManager.FindByIdAsync(userId);
            if (currentUser != null && !assignedUsers.Any(u => u.Id == userId))
            {
                assignedUsers.Add(new
                {
                    Id = userId,
                    Name = $"{currentUser.FullName ?? currentUser.UserName} ({currentUser.Phone})"
                });
            }

            ViewBag.AssignedUsers = assignedUsers.OrderBy(u => u.Name).ToList();

            // 📦 مدل اولیه
            var model = new TaskCreateVm
            {
                ProjectId = projectId.Value,
                IssueType = IssueType.Task
            };

            var defaultProjectIssueType = projectIssueTypes.FirstOrDefault();
            if (defaultProjectIssueType != null)
            {
                model.ProjectIssueTypeId = defaultProjectIssueType.Id;
                model.IssueType = defaultProjectIssueType.BaseType;
            }

            return View(model);
        }


        // POST
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(TaskCreateVm vm)
        {
            if (!ModelState.IsValid)
            {
                await FillListsForCreate(vm.ProjectId);
                return View(vm);
            }

            var start = vm.StartDateSh.ToGregorianDateTime();
            var due = string.IsNullOrWhiteSpace(vm.DueDateSh) ? (DateTime?)null : vm.DueDateSh.ToGregorianDateTime();

            if (start is null)
            {
                ModelState.AddModelError(nameof(vm.StartDateSh), "تاریخ شروع معتبر نیست.");
                await FillListsForCreate(vm.ProjectId);
                return View(vm);
            }

            if (vm.CategoryId.HasValue)
            {
                var categoryIsValid = await _context.TaskCategories
                    .AnyAsync(c => c.Id == vm.CategoryId.Value && c.ProjectId == vm.ProjectId);

                if (!categoryIsValid)
                {
                    ModelState.AddModelError(nameof(vm.CategoryId), "دسته‌بندی انتخاب‌شده متعلق به این پروژه نیست.");
                    await FillListsForCreate(vm.ProjectId);
                    return View(vm);
                }
            }

            ProjectIssueType? selectedProjectIssueType = null;
            if (vm.ProjectIssueTypeId.HasValue)
            {
                selectedProjectIssueType = await _context.ProjectIssueTypes
                    .FirstOrDefaultAsync(pit => pit.Id == vm.ProjectIssueTypeId.Value && pit.ProjectId == vm.ProjectId);

                if (selectedProjectIssueType == null)
                {
                    ModelState.AddModelError(nameof(vm.ProjectIssueTypeId), "نوع کار انتخاب‌شده معتبر نیست.");
                    await FillListsForCreate(vm.ProjectId);
                    return View(vm);
                }

                vm.IssueType = selectedProjectIssueType.BaseType;
            }

            // ✅ اگر AssignedUserId ست شده، حتماً عضو پروژه یا سازنده پروژه باشد
            if (!string.IsNullOrWhiteSpace(vm.AssignedUserId))
            {
                var project = await _context.Projects.FindAsync(vm.ProjectId);
                if (project == null)
                {
                    ModelState.AddModelError(nameof(vm.ProjectId), "پروژه یافت نشد.");
                    await FillListsForCreate(vm.ProjectId);
                    return View(vm);
                }

                // بررسی اینکه آیا کاربر سازنده پروژه است یا عضو پروژه
                var isCreator = project.CreatorUserId == vm.AssignedUserId;
                var isMember = await _context.ProjectMembers
                    .AnyAsync(m => m.ProjectId == vm.ProjectId && m.UserId == vm.AssignedUserId);
                
                // بررسی دعوت‌های پذیرفته‌شده
                var isAcceptedInvitee = await _context.ProjectInvitations
                    .AnyAsync(i => i.ProjectId == vm.ProjectId && 
                                 i.InviteeId == vm.AssignedUserId && 
                                 i.Status == InvitationStatus.Accepted);

                if (!isCreator && !isMember && !isAcceptedInvitee)
                {
                    ModelState.AddModelError(nameof(vm.AssignedUserId), "کاربر انتخاب‌شده عضو این پروژه نیست.");
                    await FillListsForCreate(vm.ProjectId);
                    return View(vm);
                }
            }

            // 📝 دریافت پروژه برای تولید IssueKey
            var projectForIssueKey = await _context.Projects.FindAsync(vm.ProjectId);
            if (projectForIssueKey == null)
            {
                ModelState.AddModelError(nameof(vm.ProjectId), "پروژه یافت نشد.");
                await FillListsForCreate(vm.ProjectId);
                return View(vm);
            }
            //////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////////                

            // 🔄 Retry mechanism برای جلوگیری از race condition در IssueKey
            int maxRetries = 10;
            TaskItem newTask = null;
            string generatedIssueKey = null;
            
            for (int retry = 0; retry < maxRetries; retry++)
            {
                try
                {
                    // پاک کردن ChangeTracker برای project و newTask
                    if (projectForIssueKey != null)
                    {
                        var entry = _context.Entry(projectForIssueKey);
                        if (entry.State != Microsoft.EntityFrameworkCore.EntityState.Detached)
                        {
                            entry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                    }
                    
                    if (newTask != null)
                    {
                        var taskEntry = _context.Entry(newTask);
                        if (taskEntry.State != Microsoft.EntityFrameworkCore.EntityState.Detached)
                        {
                            taskEntry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                    }
                    
                    // دریافت مجدد project از دیتابیس برای اطمینان از آخرین LastIssueNumber
                    projectForIssueKey = await _context.Projects
                        .AsNoTracking()
                        .FirstOrDefaultAsync(p => p.Id == vm.ProjectId);
                    
                    if (projectForIssueKey == null)
                    {
                        ModelState.AddModelError(nameof(vm.ProjectId), "پروژه یافت نشد.");
                        await FillListsForCreate(vm.ProjectId);
                        return View(vm);
                    }
                    
                    // تولید IssueKey و بررسی تکراری بودن - تا زمانی که IssueKey منحصر به فرد باشد
                    int keyGenerationAttempts = 0;
                    do
                    {
                        generatedIssueKey = projectForIssueKey.GenerateNextIssueKey();
                        bool keyExists = await _context.TaskItems
                            .AnyAsync(t => t.IssueKey == generatedIssueKey);
                        
                        if (keyExists)
                        {
                            // اگر key تکراری بود، LastIssueNumber را افزایش می‌دهیم
                            projectForIssueKey.LastIssueNumber++;
                            keyGenerationAttempts++;
                            
                            // جلوگیری از حلقه بی‌نهایت
                            if (keyGenerationAttempts > 100)
                            {
                                ModelState.AddModelError(string.Empty, "خطا در تولید IssueKey منحصر به فرد. لطفاً دوباره تلاش کنید.");
                                await FillListsForCreate(vm.ProjectId);
                                return View(vm);
                            }
                        }
                        else
                        {
                            break; // IssueKey منحصر به فرد است
                        }
                    } while (true);
                    Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>>> generatedIssueKey={generatedIssueKey} ");
                    // ساخت Task جدید با IssueKey منحصر به فرد
                    newTask = new TaskItem
                    {
                        Title = vm.Title,
                        Description = vm.Description,
                        IssueType = vm.IssueType,
                        IssueKey = generatedIssueKey,
                        CategoryId = vm.CategoryId,
                        ParentTaskId = vm.ParentId,
                        ProjectId = vm.ProjectId,
                        StartDate = start.Value,
                        DueDate = due,
                        AssignedUserId = vm.AssignedUserId,
                        StoryPoints = vm.StoryPoints,
                        IsCompleted = false,
                        CreatedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                        CreatedAt = DateTime.UtcNow,
                        UpdatedAt = DateTime.UtcNow,
                        ProjectIssueTypeId = selectedProjectIssueType?.Id
                    };

                    // اضافه کردن به context
                    _context.TaskItems.Add(newTask);
                    
                    // به‌روزرسانی LastIssueNumber در project
                    var projectToUpdate = await _context.Projects.FindAsync(vm.ProjectId);
                    if (projectToUpdate != null)
                    {
                        projectToUpdate.LastIssueNumber = projectForIssueKey.LastIssueNumber;
                        _context.Projects.Update(projectToUpdate);
                    }
                    
                    await _context.SaveChangesAsync();
                    break; // موفقیت‌آمیز بود، خارج شو
                }
                catch (Microsoft.EntityFrameworkCore.DbUpdateException ex) when (ex.InnerException is Microsoft.Data.SqlClient.SqlException sqlEx && 
                                                   sqlEx.Number == 2601 && // Duplicate key error
                                                   retry < maxRetries - 1)
                {
                    // اگر IssueKey تکراری بود، دوباره تلاش کن
                    if (newTask != null)
                    {
                        var taskEntry = _context.Entry(newTask);
                        if (taskEntry.State != Microsoft.EntityFrameworkCore.EntityState.Detached)
                        {
                            taskEntry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                        newTask = null; // reset برای retry
                    }
                    
                    if (projectForIssueKey != null)
                    {
                        var projectEntry = _context.Entry(projectForIssueKey);
                        if (projectEntry.State != Microsoft.EntityFrameworkCore.EntityState.Detached)
                        {
                            projectEntry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                        }
                    }
                    
                    // پاک کردن ChangeTracker برای اطمینان از clean state
                    var trackedEntries = _context.ChangeTracker.Entries()
                        .Where(e => (newTask != null && e.Entity == newTask) || (e.Entity is Project p && p.Id == vm.ProjectId))
                        .ToList();
                    foreach (var trackedEntry in trackedEntries)
                    {
                        trackedEntry.State = Microsoft.EntityFrameworkCore.EntityState.Detached;
                    }
                    
                    // delay افزایشی برای retry بعدی
                    await Task.Delay(100 * (retry + 1));
                    continue; // دوباره تلاش کن
                }
            }

            if (newTask == null)
            {
                ModelState.AddModelError(string.Empty, "خطا در ایجاد تسک. لطفاً دوباره تلاش کنید.");
                await FillListsForCreate(vm.ProjectId);
                return View(vm);
            }

            if (!string.IsNullOrWhiteSpace(newTask.AssignedUserId))
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.Equals(newTask.AssignedUserId, currentUserId, StringComparison.OrdinalIgnoreCase))
                {
                    var projectName = projectForIssueKey?.Name ?? "پروژه";
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        UserId = newTask.AssignedUserId,
                        Title = $"تسک جدید به شما واگذار شد",
                        Message = $"تسک «{newTask.Title}» در پروژه «{projectName}» به شما واگذار شد.",
                        RelatedEntityId = newTask.Id.ToString(),
                        RelatedEntityType = nameof(TaskItem),
                        Type = NotificationCreateType.TaskAssigned,
                        PayloadJson = JsonSerializer.Serialize(new
                        {
                            taskId = newTask.Id,
                            issueKey = newTask.IssueKey,
                            projectId = newTask.ProjectId
                        })
                    });
                }
            }

            // ✅ افزودن خودکار کار به اسپرینت فعال (اگر وجود داشته باشد)
            var activeSprint = await _context.Sprints
                .FirstOrDefaultAsync(s => s.ProjectId == vm.ProjectId && s.Status == SprintStatus.Active);

            if (activeSprint != null)
            {
                // بررسی اینکه آیا کار می‌تواند به اسپرینت اضافه شود
                bool canAddToSprint = false;
                
                if (selectedProjectIssueType != null)
                {
                    canAddToSprint = selectedProjectIssueType.CanAddToSprint;
                }
                else
                {
                    // اگر ProjectIssueType مشخص نشده، بر اساس IssueType بررسی کن
                    canAddToSprint = newTask.IssueType == IssueType.Task || 
                                    newTask.IssueType == IssueType.Story || 
                                    newTask.IssueType == IssueType.Bug;
                }

                // فقط کارهای Story-level (نه Epic و Subtask) می‌توانند به اسپرینت اضافه شوند
                if (canAddToSprint && newTask.ParentTaskId == null)
                {
                    // بررسی اینکه آیا کار قبلاً در اسپرینت است
                    var existingSprintTask = await _context.SprintTasks
                        .FirstOrDefaultAsync(st => st.SprintId == activeSprint.Id && st.TaskId == newTask.Id);

                    if (existingSprintTask == null)
                    {
                        // دریافت وضعیت پیش‌فرض (Todo) برای اسپرینت
                        var defaultStatus = await _context.WorkflowStatuses
                            .Where(ws => ws.SprintId == activeSprint.Id)
                            .OrderBy(ws => ws.IsDefault ? 0 : ws.Order)
                            .FirstOrDefaultAsync();

                        // افزودن کار به اسپرینت
                        var sprintTask = new SprintTask
                        {
                            SprintId = activeSprint.Id,
                            TaskId = newTask.Id,
                            AddedByUserId = User.FindFirstValue(ClaimTypes.NameIdentifier),
                            AddedAt = DateTime.UtcNow,
                            Status = SprintTaskStatus.Pending,
                            SprintPriority = 1
                        };
                        _context.SprintTasks.Add(sprintTask);

                        // تنظیم وضعیت کار به Todo (وضعیت پیش‌فرض اسپرینت)
                        if (defaultStatus != null)
                        {
                            newTask.StatusId = defaultStatus.Id;
                            newTask.SprintId = activeSprint.Id;
                        }

                        await _context.SaveChangesAsync();
                    }
                }
            }

            await _notificationService.TrySendDueSoonNotificationAsync(newTask, projectForIssueKey?.Name);
            return RedirectToAction(nameof(Index), new { projectId = vm.ProjectId });
        }

        private async Task FillListsForCreate(int projectId)
        {
            var project = await _context.Projects.FindAsync(projectId);
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            ViewBag.ProjectName = project?.Name ?? "پروژه";

            ViewBag.Categories = await _context.TaskCategories
                .Where(c => c.ProjectId == projectId)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.Tasks = await _context.TaskItems
                .Where(t => t.ProjectId == projectId)
                .ToListAsync();

            ViewBag.ProjectIssueTypes = await _context.ProjectIssueTypes
                .Where(pit => pit.ProjectId == projectId)
                .OrderBy(pit => pit.Order)
                .ToListAsync();

            ViewBag.Projects = project != null
                ? new List<Project> { project }
                : new List<Project>();

            var assignedUsers = new List<dynamic>();

            if (project != null && !string.IsNullOrEmpty(project.CreatorUserId))
            {
                var creatorInfo = await _userManager.FindByIdAsync(project.CreatorUserId);
                if (creatorInfo != null)
                {
                    assignedUsers.Add(new
                    {
                        Id = project.CreatorUserId,
                        Name = $"{creatorInfo.FullName ?? creatorInfo.UserName} ({creatorInfo.Phone}) - سازنده پروژه"
                    });
                }
            }

            if (project != null)
            {
                // دریافت اعضای فعال پروژه از جدول ProjectMembers
                var activeProjectMembers = await _context.ProjectMembers
                    .Where(m => m.ProjectId == projectId)
                    .Select(m => m.UserId)
                    .ToListAsync();

                foreach (var memberId in activeProjectMembers)
                {
                    if (memberId == project.CreatorUserId) continue;

                    var userInfo = await _userManager.FindByIdAsync(memberId);
                    if (userInfo != null)
                    {
                        assignedUsers.Add(new
                        {
                            Id = memberId,
                            Name = $"{userInfo.FullName ?? userInfo.UserName} ({userInfo.Phone})"
                        });
                    }
                }
            }

            if (!string.IsNullOrEmpty(currentUserId) && !assignedUsers.Any(u => u.Id == currentUserId))
            {
                var currentUser = await _userManager.FindByIdAsync(currentUserId);
                if (currentUser != null)
                {
                    assignedUsers.Add(new
                    {
                        Id = currentUserId,
                        Name = $"{currentUser.FullName ?? currentUser.UserName} ({currentUser.Phone})"
                    });
                }
            }

            ViewBag.AssignedUsers = assignedUsers.OrderBy(u => u.Name).ToList();
        }

        private async Task<string?> GetParentTaskTitleAsync(int? parentTaskId)
        {
            if (!parentTaskId.HasValue)
            {
                return null;
            }

            return await _context.TaskItems
                .Where(t => t.Id == parentTaskId.Value)
                .Select(t => t.Title)
                .FirstOrDefaultAsync();
        }


        [HttpGet]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> GetTaskDetails(int? id)
        {
            if (!id.HasValue)
            {
                if (Request.Query.ContainsKey("id") && int.TryParse(Request.Query["id"], out int queryId))
                {
                    id = queryId;
                }
                else
                {
                    return Json(new { success = false, message = "شناسه کار نامعتبر است" });
                }
            }
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var task = await _context.TaskItems
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.ProjectIssueType)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
            {
                return Json(new { success = false, message = "کار یافت نشد" });
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == task.ProjectId &&
                    (p.CreatorUserId == userId || 
                     p.Members.Any(m => m.UserId == userId) ||
                     _context.ProjectInvitations.Any(i => i.ProjectId == task.ProjectId && i.InviteeId == userId && i.Status == InvitationStatus.Accepted)));

            if (!hasAccess)
            {
                return Json(new { success = false, message = "شما به این کار دسترسی ندارید" });
            }


            // دریافت IssueTypes
            var projectIssueTypes = await _context.ProjectIssueTypes
                .Where(pit => pit.ProjectId == task.ProjectId)
                .OrderBy(pit => pit.Order)
                .ToListAsync();

            var availableIssueTypes = projectIssueTypes
                .Where(pit => pit.Level != IssueTypeLevel.Subtask)
                .Select(pit => new
                {
                    id = pit.Id,
                    name = pit.Name,
                    icon = pit.Icon,
                    baseType = (int)pit.BaseType
                })
                .ToList();

            // دریافت کاربران
            var assignedUsers = new List<dynamic>();
            
            if (task.Project != null && !string.IsNullOrEmpty(task.Project.CreatorUserId))
            {
                var creatorInfo = await _userManager.FindByIdAsync(task.Project.CreatorUserId);
                if (creatorInfo != null)
                {
                    assignedUsers.Add(new
                    {
                        id = task.Project.CreatorUserId,
                        name = $"{creatorInfo.FullName ?? creatorInfo.UserName} ({creatorInfo.Phone}) - سازنده پروژه"
                    });
                }
            }

            // دریافت اعضای فعال پروژه از جدول ProjectMembers
            var activeMembers = await _context.ProjectMembers
                .Where(m => m.ProjectId == task.ProjectId)
                .Select(m => m.UserId)
                .ToListAsync();

            foreach (var memberId in activeMembers)
            {
                if (task.Project != null && memberId == task.Project.CreatorUserId) continue;

                var userInfo = await _userManager.FindByIdAsync(memberId);
                if (userInfo != null)
                {
                    assignedUsers.Add(new
                    {
                        id = memberId,
                        name = $"{userInfo.FullName ?? userInfo.UserName} ({userInfo.Phone})"
                    });
                }
            }

            var currentUser = await _userManager.FindByIdAsync(userId);
            if (currentUser != null && !assignedUsers.Any(u => u.id == userId))
            {
                assignedUsers.Add(new
                {
                    id = userId,
                    name = $"{currentUser.FullName ?? currentUser.UserName} ({currentUser.Phone})"
                });
            }
            return Json(new
            {
                success = true,
                id = task.Id,
                title = task.Title,
                description = task.Description,
                issueType = (int)task.IssueType,
                projectIssueTypeId = task.ProjectIssueTypeId,
                projectId = task.ProjectId,
                startDateSh = task.StartDate.ToShortPersianDateString(),
                dueDateSh = task.DueDate != null ? task.DueDate.Value.ToShortPersianDateString() : string.Empty,
                startDate = task.StartDate,
                dueDate = task.DueDate,
                categoryId = task.CategoryId,
                assignedUserId = task.AssignedUserId,
                storyPoints = task.StoryPoints,
                availableIssueTypes = availableIssueTypes,
                assignedUsers = assignedUsers.OrderBy(u => u.name).ToList()
            });
        }

        [HttpGet]
        public async Task<IActionResult> Edit(int? id)
        {
            // پشتیبانی از query parameter
            if (!id.HasValue)
            {
                if (Request.Query.ContainsKey("id") && int.TryParse(Request.Query["id"], out int queryId))
                {
                    id = queryId;
                }
                else
                {
                    return NotFound();
                }
            }
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var task = await _context.TaskItems
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.ProjectIssueType)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            // 🔒 بررسی دسترسی کاربر به تسک
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == task.ProjectId &&
                    (p.CreatorUserId == userId || 
                     p.Members.Any(m => m.UserId == userId) ||
                     _context.ProjectInvitations.Any(i => i.ProjectId == task.ProjectId && i.InviteeId == userId && i.Status == InvitationStatus.Accepted)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این تسک دسترسی ندارید.";
                return RedirectToAction(nameof(Index), new { projectId = task.ProjectId });
            }


            var vm = new TaskEditVm
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                CategoryId = task.CategoryId,
                ParentId = task.ParentTaskId,
                ProjectId = task.ProjectId,
                StartDateSh = task.StartDate.ToShortPersianDateString(),
                DueDateSh = task.DueDate.HasValue ? task.DueDate.Value.ToShortPersianDateString() : null,
                AssignedUserId = task.AssignedUserId,
                IssueType = task.IssueType,
                ProjectIssueTypeId = task.ProjectIssueTypeId
            };

            ViewBag.Categories = await _context.TaskCategories
                .Where(c => c.ProjectId == task.ProjectId)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.ProjectIssueTypes = await _context.ProjectIssueTypes
                .Where(pit => pit.ProjectId == task.ProjectId)
                .OrderBy(pit => pit.Order)
                .ToListAsync();

            ViewBag.ParentTaskTitle = await GetParentTaskTitleAsync(task.ParentTaskId);
            
            // 🔒 پروژه فقط برای نمایش (غیرفعال خواهد شد) - فقط پروژه فعلی
            ViewBag.Projects = new List<Project> { task.Project };

            // 🔹 گرفتن کاربران فعال پروژه برای انتخاب "مسئول تسک"
            var users = new List<dynamic>();
            
            // اضافه کردن سازنده پروژه
            if (task.Project != null && !string.IsNullOrEmpty(task.Project.CreatorUserId))
            {
                var creatorInfo = await _userManager.FindByIdAsync(task.Project.CreatorUserId);
                if (creatorInfo != null)
                {
                    users.Add(new 
                    { 
                        Id = task.Project.CreatorUserId, 
                        Name = $"{creatorInfo.FullName ?? creatorInfo.UserName} ({creatorInfo.Phone}) - سازنده پروژه" 
                    });
                }
            }

            // دریافت اعضای فعال پروژه از جدول ProjectMembers
            var activeMembers = await _context.ProjectMembers
                .Where(m => m.ProjectId == task.ProjectId)
                .Select(m => m.UserId)
                .ToListAsync();

            foreach (var memberId in activeMembers)
            {
                // جلوگیری از اضافه کردن مجدد سازنده پروژه
                if (task.Project != null && memberId == task.Project.CreatorUserId) continue;

                var userInfo = await _userManager.FindByIdAsync(memberId);
                if (userInfo != null)
                    users.Add(new { Id = memberId, Name = $"{userInfo.FullName ?? userInfo.UserName} ({userInfo.Phone})" });
            }
            
            ViewBag.AssignedUsers = users;

            return View(vm);
        }

        // ویرایش کار از تخته کانبان (فقط JSON)
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> EditFromBoard([FromBody] TaskEditVm vm)
        {
            if (vm == null || vm.Id == 0)
            {
                return Json(new { success = false, message = "اطلاعات کار نامعتبر است." });
            }

            return await SaveTaskEditAsync(vm, isJsonRequest: true);
        }

        // ویرایش کار از فرم معمولی (فقط Form Data)
        [HttpPost]
        [IgnoreAntiforgeryToken]
        public async Task<IActionResult> Edit(TaskEditVm vm = null)
        {
            // برای فرم‌های معمولی، از model binding استفاده می‌کنیم
            // اگر model binding کار نکرد، دستی parse کن
            if (vm == null || vm.Id == 0)
            {
                vm = new TaskEditVm
                {
                    Id = int.TryParse(Request.Form["Id"].FirstOrDefault(), out var id) ? id : 0,
                    Title = Request.Form["Title"].FirstOrDefault(),
                    Description = Request.Form["Description"].FirstOrDefault(),
                    IssueType = Enum.TryParse<IssueType>(Request.Form["IssueType"].FirstOrDefault(), out var issueType) ? issueType : IssueType.Task,
                    ProjectIssueTypeId = int.TryParse(Request.Form["ProjectIssueTypeId"].FirstOrDefault(), out var pitId) ? pitId : (int?)null,
                    ProjectId = int.TryParse(Request.Form["ProjectId"].FirstOrDefault(), out var projectId) ? projectId : 0,
                    StartDateSh = Request.Form["StartDateSh"].FirstOrDefault(),
                    DueDateSh = Request.Form["DueDateSh"].FirstOrDefault(),
                    CategoryId = int.TryParse(Request.Form["CategoryId"].FirstOrDefault(), out var catId) ? catId : (int?)null,
                    AssignedUserId = Request.Form["AssignedUserId"].FirstOrDefault(),
                    StoryPoints = int.TryParse(Request.Form["StoryPoints"].FirstOrDefault(), out var sp) ? sp : (int?)null
                };
            }
            
            if (vm == null || vm.Id == 0)
            {
                TempData["Error"] = "اطلاعات کار نامعتبر است.";
                return RedirectToAction(nameof(Index));
            }
            
            return await SaveTaskEditAsync(vm, isJsonRequest: false);
        }

        // متد کمکی برای ذخیره ویرایش کار
        private async Task<IActionResult> SaveTaskEditAsync(TaskEditVm vm, bool isJsonRequest)
        {
            if (!ModelState.IsValid)
            {
                if (isJsonRequest)
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage);
                    return Json(new { success = false, message = string.Join("; ", errors) });
                }

                // بارگذاری مجدد ViewBag برای نمایش خطا
                var taskForError = await _context.TaskItems
                    .Include(t => t.Project)
                    .FirstOrDefaultAsync(t => t.Id == vm.Id);
                
                ViewBag.Categories = taskForError != null
                    ? await _context.TaskCategories
                        .Where(c => c.ProjectId == taskForError.ProjectId)
                        .OrderBy(c => c.Name)
                        .ToListAsync()
                    : new List<TaskCategory>();
                ViewBag.ProjectIssueTypes = taskForError != null
                    ? await _context.ProjectIssueTypes
                        .Where(pit => pit.ProjectId == taskForError.ProjectId)
                        .OrderBy(pit => pit.Order)
                        .ToListAsync()
                    : new List<ProjectIssueType>();
                ViewBag.ParentTaskTitle = taskForError != null
                    ? await GetParentTaskTitleAsync(taskForError.ParentTaskId)
                    : null;
                ViewBag.Projects = taskForError?.Project != null 
                    ? new List<Project> { taskForError.Project } 
                    : new List<Project>();
                
                // بارگذاری مجدد لیست کاربران
                if (taskForError?.Project != null)
                {
                    var users = new List<dynamic>();
                    if (!string.IsNullOrEmpty(taskForError.Project.CreatorUserId))
                    {
                        var creatorInfo = await _userManager.FindByIdAsync(taskForError.Project.CreatorUserId);
                        if (creatorInfo != null)
                        {
                            users.Add(new 
                            { 
                                Id = taskForError.Project.CreatorUserId, 
                                Name = $"{creatorInfo.FullName ?? creatorInfo.UserName} ({creatorInfo.Phone}) - سازنده پروژه" 
                            });
                        }
                    }
                    // دریافت اعضای فعال پروژه از جدول ProjectMembers
                    var activeMembers = await _context.ProjectMembers
                        .Where(m => m.ProjectId == taskForError.ProjectId)
                        .Select(m => m.UserId)
                        .ToListAsync();
                    foreach (var memberId in activeMembers)
                    {
                        if (taskForError.Project != null && memberId == taskForError.Project.CreatorUserId) continue;
                        var userInfo = await _userManager.FindByIdAsync(memberId);
                        if (userInfo != null)
                            users.Add(new { Id = memberId, Name = $"{userInfo.FullName ?? userInfo.UserName} ({userInfo.Phone})" });
                    }
                    ViewBag.AssignedUsers = users;
                }
                else
                {
                    ViewBag.AssignedUsers = new List<dynamic>();
                }
                
                return View(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            var task = await _context.TaskItems
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == vm.Id);
            if (task == null) return NotFound();

            // 🔒 بررسی دسترسی کاربر به تسک
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == task.ProjectId &&
                    (p.CreatorUserId == userId || 
                     p.Members.Any(m => m.UserId == userId) ||
                     _context.ProjectInvitations.Any(i => i.ProjectId == task.ProjectId && i.InviteeId == userId && i.Status == InvitationStatus.Accepted)));

            if (!hasAccess)
            {
                if (isJsonRequest)
                {
                    return Json(new { success = false, message = "شما به این تسک دسترسی ندارید." });
                }
                TempData["Error"] = "شما به این تسک دسترسی ندارید.";
                return RedirectToAction(nameof(Index), new { projectId = task.ProjectId });
            }

            // 🔒 جلوگیری از تغییر پروژه - ProjectId نباید تغییر کند
            if (vm.ProjectId != task.ProjectId)
            {
                if (isJsonRequest)
                {
                    return Json(new { success = false, message = "تغییر پروژه تسک مجاز نیست." });
                }
                ModelState.AddModelError(nameof(vm.ProjectId), "تغییر پروژه تسک مجاز نیست.");
                ViewBag.Categories = await _context.TaskCategories
                    .Where(c => c.ProjectId == task.ProjectId)
                    .OrderBy(c => c.Name)
                    .ToListAsync();
                ViewBag.ProjectIssueTypes = await _context.ProjectIssueTypes
                    .Where(pit => pit.ProjectId == task.ProjectId)
                    .OrderBy(pit => pit.Order)
                    .ToListAsync();
                ViewBag.ParentTaskTitle = await GetParentTaskTitleAsync(task.ParentTaskId);
                ViewBag.Projects = new List<Project> { task.Project };
                
                // بارگذاری مجدد لیست کاربران
                var users = new List<dynamic>();
                if (task.Project != null && !string.IsNullOrEmpty(task.Project.CreatorUserId))
                {
                    var creatorInfo = await _userManager.FindByIdAsync(task.Project.CreatorUserId);
                    if (creatorInfo != null)
                    {
                        users.Add(new 
                        { 
                            Id = task.Project.CreatorUserId, 
                            Name = $"{creatorInfo.FullName ?? creatorInfo.UserName} ({creatorInfo.Phone}) - سازنده پروژه" 
                        });
                    }
                }
                    // دریافت اعضای فعال پروژه از جدول ProjectMembers
                    var activeMembers = await _context.ProjectMembers
                        .Where(m => m.ProjectId == task.ProjectId)
                        .Select(m => m.UserId)
                        .ToListAsync();
                    foreach (var memberId in activeMembers)
                    {
                        if (task.Project != null && memberId == task.Project.CreatorUserId) continue;
                        var userInfo = await _userManager.FindByIdAsync(memberId);
                        if (userInfo != null)
                            users.Add(new { Id = memberId, Name = $"{userInfo.FullName ?? userInfo.UserName} ({userInfo.Phone})" });
                    }
                    ViewBag.AssignedUsers = users;
                
                return View(vm);
            }

            if (vm.CategoryId.HasValue)
            {
                var categoryIsValid = await _context.TaskCategories
                    .AnyAsync(c => c.Id == vm.CategoryId.Value && c.ProjectId == task.ProjectId);

                if (!categoryIsValid)
                {
                    if (isJsonRequest)
                    {
                        return Json(new { success = false, message = "دسته‌بندی انتخاب‌شده متعلق به این پروژه نیست." });
                    }
                    ModelState.AddModelError(nameof(vm.CategoryId), "دسته‌بندی انتخاب‌شده متعلق به این پروژه نیست.");
                    ViewBag.Categories = await _context.TaskCategories
                        .Where(c => c.ProjectId == task.ProjectId)
                        .OrderBy(c => c.Name)
                        .ToListAsync();
                    ViewBag.ParentTaskTitle = await GetParentTaskTitleAsync(task.ParentTaskId);
                    ViewBag.Projects = task.Project != null ? new List<Project> { task.Project } : new List<Project>();

                    var users = new List<dynamic>();
                    if (task.Project != null && !string.IsNullOrEmpty(task.Project.CreatorUserId))
                    {
                        var creatorInfo = await _userManager.FindByIdAsync(task.Project.CreatorUserId);
                        if (creatorInfo != null)
                        {
                            users.Add(new
                            {
                                Id = task.Project.CreatorUserId,
                                Name = $"{creatorInfo.FullName ?? creatorInfo.UserName} ({creatorInfo.Phone}) - سازنده پروژه"
                            });
                        }
                    }
                    // دریافت اعضای فعال پروژه از جدول ProjectMembers
                    var activeMembers = await _context.ProjectMembers
                        .Where(m => m.ProjectId == task.ProjectId)
                        .Select(m => m.UserId)
                        .ToListAsync();
                    foreach (var memberId in activeMembers)
                    {
                        if (task.Project != null && memberId == task.Project.CreatorUserId) continue;
                        var userInfo = await _userManager.FindByIdAsync(memberId);
                        if (userInfo != null)
                            users.Add(new { Id = memberId, Name = $"{userInfo.FullName ?? userInfo.UserName} ({userInfo.Phone})" });
                    }
                    ViewBag.AssignedUsers = users;
                
                return View(vm);
            }
            }

            // ✅ اگر AssignedUserId ست شده، حتماً عضو پروژه یا سازنده پروژه باشد
            if (!string.IsNullOrWhiteSpace(vm.AssignedUserId))
            {
                // 🔒 استفاده از task.ProjectId به جای vm.ProjectId چون پروژه قابل تغییر نیست
                var project = task.Project ?? await _context.Projects.FindAsync(task.ProjectId);
                if (project == null)
                {
                    if (isJsonRequest)
                    {
                        return Json(new { success = false, message = "پروژه یافت نشد." });
                    }
                    ModelState.AddModelError(nameof(vm.ProjectId), "پروژه یافت نشد.");
                    ViewBag.Categories = await _context.TaskCategories
                        .Where(c => c.ProjectId == task.ProjectId)
                        .OrderBy(c => c.Name)
                        .ToListAsync();
                    ViewBag.ParentTaskTitle = await GetParentTaskTitleAsync(task.ParentTaskId);
                    ViewBag.Projects = task.Project != null ? new List<Project> { task.Project } : new List<Project>();
                    
                    // بارگذاری مجدد لیست کاربران
                    var users = new List<dynamic>();
                    if (task.Project != null && !string.IsNullOrEmpty(task.Project.CreatorUserId))
                    {
                        var creatorInfo = await _userManager.FindByIdAsync(task.Project.CreatorUserId);
                        if (creatorInfo != null)
                        {
                            users.Add(new 
                            { 
                                Id = task.Project.CreatorUserId, 
                                Name = $"{creatorInfo.FullName ?? creatorInfo.UserName} ({creatorInfo.Phone}) - سازنده پروژه" 
                            });
                        }
                    }
                    // دریافت اعضای فعال پروژه از جدول ProjectMembers
                    var activeMembers = await _context.ProjectMembers
                        .Where(m => m.ProjectId == task.ProjectId)
                        .Select(m => m.UserId)
                        .ToListAsync();
                    foreach (var memberId in activeMembers)
                    {
                        if (task.Project != null && memberId == task.Project.CreatorUserId) continue;
                        var userInfo = await _userManager.FindByIdAsync(memberId);
                        if (userInfo != null)
                            users.Add(new { Id = memberId, Name = $"{userInfo.FullName ?? userInfo.UserName} ({userInfo.Phone})" });
                    }
                    ViewBag.AssignedUsers = users;
                
                return View(vm);
            }

                // بررسی اینکه آیا کاربر سازنده پروژه است یا عضو پروژه
                // 🔒 استفاده از task.ProjectId به جای vm.ProjectId چون پروژه قابل تغییر نیست
                var isCreator = project.CreatorUserId == vm.AssignedUserId;
                var isMember = await _context.ProjectMembers
                    .AnyAsync(m => m.ProjectId == task.ProjectId && m.UserId == vm.AssignedUserId);
                
                // بررسی دعوت‌های پذیرفته‌شده
                var isAcceptedInvitee = await _context.ProjectInvitations
                    .AnyAsync(i => i.ProjectId == task.ProjectId && 
                                 i.InviteeId == vm.AssignedUserId && 
                                 i.Status == InvitationStatus.Accepted);

                if (!isCreator && !isMember && !isAcceptedInvitee)
                {
                    if (isJsonRequest)
                    {
                        return Json(new { success = false, message = "کاربر انتخاب‌شده عضو این پروژه نیست." });
                    }
                    ModelState.AddModelError(nameof(vm.AssignedUserId), "کاربر انتخاب‌شده عضو این پروژه نیست.");
                    ViewBag.Categories = await _context.TaskCategories
                        .Where(c => c.ProjectId == task.ProjectId)
                        .OrderBy(c => c.Name)
                        .ToListAsync();
                    ViewBag.ParentTaskTitle = await GetParentTaskTitleAsync(task.ParentTaskId);
                    ViewBag.Projects = task.Project != null ? new List<Project> { task.Project } : new List<Project>();
                    
                    // بارگذاری مجدد لیست کاربران
                    var users = new List<dynamic>();
                    if (task.Project != null && !string.IsNullOrEmpty(task.Project.CreatorUserId))
                    {
                        var creatorInfo = await _userManager.FindByIdAsync(task.Project.CreatorUserId);
                        if (creatorInfo != null)
                        {
                            users.Add(new 
                            { 
                                Id = task.Project.CreatorUserId, 
                                Name = $"{creatorInfo.FullName ?? creatorInfo.UserName} ({creatorInfo.Phone}) - سازنده پروژه" 
                            });
                        }
                    }
                    // دریافت اعضای فعال پروژه از جدول ProjectMembers
                    var activeMembers = await _context.ProjectMembers
                        .Where(m => m.ProjectId == task.ProjectId)
                        .Select(m => m.UserId)
                        .ToListAsync();
                    foreach (var memberId in activeMembers)
                    {
                        if (task.Project != null && memberId == task.Project.CreatorUserId) continue;
                        var userInfo = await _userManager.FindByIdAsync(memberId);
                        if (userInfo != null)
                            users.Add(new { Id = memberId, Name = $"{userInfo.FullName ?? userInfo.UserName} ({userInfo.Phone})" });
                    }
                    ViewBag.AssignedUsers = users;
                
                return View(vm);
            }
            }

            // بررسی و به‌روزرسانی ProjectIssueType
            ProjectIssueType? selectedProjectIssueType = null;
            if (vm.ProjectIssueTypeId.HasValue)
            {
                selectedProjectIssueType = await _context.ProjectIssueTypes
                    .FirstOrDefaultAsync(pit => pit.Id == vm.ProjectIssueTypeId.Value && pit.ProjectId == task.ProjectId);

                if (selectedProjectIssueType == null)
                {
                    if (isJsonRequest)
                    {
                        return Json(new { success = false, message = "نوع کار انتخاب‌شده معتبر نیست." });
                    }
                    ModelState.AddModelError(nameof(vm.ProjectIssueTypeId), "نوع کار انتخاب‌شده معتبر نیست.");
                    ViewBag.Categories = await _context.TaskCategories
                        .Where(c => c.ProjectId == task.ProjectId)
                        .OrderBy(c => c.Name)
                        .ToListAsync();
                    ViewBag.ProjectIssueTypes = await _context.ProjectIssueTypes
                        .Where(pit => pit.ProjectId == task.ProjectId)
                        .OrderBy(pit => pit.Order)
                        .ToListAsync();
                    ViewBag.ParentTaskTitle = await GetParentTaskTitleAsync(task.ParentTaskId);
                    ViewBag.Projects = task.Project != null ? new List<Project> { task.Project } : new List<Project>();
                    // ... (بارگذاری مجدد کاربران)
                    return View(vm);
                }

                vm.IssueType = selectedProjectIssueType.BaseType;
            }

            var previousAssignee = task.AssignedUserId;
            task.Title = vm.Title;
            task.Description = vm.Description;
            task.CategoryId = vm.CategoryId;
            // 🔒 IssueType تغییر نمی‌کند - نوع کار قابل ویرایش نیست
            // task.IssueType = vm.IssueType; // ❌ حذف شد - نوع کار قابل تغییر نیست
            // task.ProjectIssueTypeId = selectedProjectIssueType?.Id; // ❌ حذف شد - نوع کار قابل تغییر نیست
            // 🔒 ProjectId تغییر نمی‌کند - همیشه همان پروژه اصلی تسک باقی می‌ماند
            // task.ProjectId = vm.ProjectId; // ❌ حذف شد - پروژه قابل تغییر نیست
            var parsedStartDate = vm.StartDateSh.ToGregorianDateTime();
            if (!parsedStartDate.HasValue)
            {
                if (isJsonRequest)
                {
                    return Json(new { success = false, message = "تاریخ شروع نامعتبر است." });
                }
                ModelState.AddModelError(nameof(vm.StartDateSh), "تاریخ شروع نامعتبر است.");
                return View(vm);
            }

            var parsedDueDate = string.IsNullOrWhiteSpace(vm.DueDateSh)
                ? (DateTime?)null
                : vm.DueDateSh.ToGregorianDateTime();
            if (!string.IsNullOrWhiteSpace(vm.DueDateSh) && !parsedDueDate.HasValue)
            {
                if (isJsonRequest)
                {
                    return Json(new { success = false, message = "تاریخ پایان نامعتبر است." });
                }
                ModelState.AddModelError(nameof(vm.DueDateSh), "تاریخ پایان نامعتبر است.");
                return View(vm);
            }

            task.StartDate = parsedStartDate.Value;
            task.DueDate = parsedDueDate;

            // 👇 مسئول تسک
            // برای Subtask، AssignedUserId را از parent task بگیر (نه از vm)
            if (vm.IssueType == IssueType.Subtask && task.ParentTaskId.HasValue)
            {
                var parentTask = await _context.TaskItems
                    .FirstOrDefaultAsync(t => t.Id == task.ParentTaskId.Value);
                if (parentTask != null)
                {
                    task.AssignedUserId = parentTask.AssignedUserId;
                }
                else
                {
                    task.AssignedUserId = vm.AssignedUserId; // fallback
                }
            }
            else
            {
                task.AssignedUserId = vm.AssignedUserId;
                
                // 🔄 اگر مسئول کار تغییر کرد، مسئول تمام کارک‌های آن را هم به‌روزرسانی کن
                var assignedUserIdChanged = !string.Equals(previousAssignee, vm.AssignedUserId, StringComparison.OrdinalIgnoreCase);
                if (assignedUserIdChanged && vm.IssueType != IssueType.Subtask)
                {
                    // پیدا کردن تمام کارک‌های این کار
                    var childTasks = await _context.TaskItems
                        .Where(t => t.ParentTaskId == task.Id && t.IssueType == IssueType.Subtask)
                        .ToListAsync();
                    
                    // به‌روزرسانی مسئول تمام کارک‌ها
                    foreach (var childTask in childTasks)
                    {
                        childTask.AssignedUserId = vm.AssignedUserId;
                        _context.Update(childTask);
                    }
                }
            }

            _context.Update(task);
            await _context.SaveChangesAsync();

            var assignmentChanged = !string.Equals(previousAssignee, vm.AssignedUserId, StringComparison.OrdinalIgnoreCase);
            if (assignmentChanged && !string.IsNullOrWhiteSpace(vm.AssignedUserId))
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                if (!string.Equals(vm.AssignedUserId, currentUserId, StringComparison.OrdinalIgnoreCase))
                {
                    var projectName = task.Project?.Name ?? "پروژه";
                    await _notificationService.CreateNotificationAsync(new NotificationCreateRequest
                    {
                        UserId = vm.AssignedUserId,
                        Title = $"تسک به شما واگذار شد",
                        Message = $"تسک «{task.Title}» در پروژه «{projectName}» به شما واگذار شد.",
                        RelatedEntityId = task.Id.ToString(),
                        RelatedEntityType = nameof(TaskItem),
                        Type = NotificationCreateType.TaskAssigned,
                        PayloadJson = JsonSerializer.Serialize(new
                        {
                            taskId = task.Id,
                            issueKey = task.IssueKey,
                            projectId = task.ProjectId
                        })
                    });
                }
            }
            await _notificationService.TrySendDueSoonNotificationAsync(task, task.Project?.Name);
            
            if (isJsonRequest)
            {
                return Json(new { success = true, message = "کار با موفقیت ویرایش شد." });
            }
            
            TempData["Success"] = "کار با موفقیت ویرایش شد.";
            
            // بررسی اینکه آیا از modal آمده (از form data یا query parameter)
            var fromModal = Request.Form.ContainsKey("fromModal") && Request.Form["fromModal"] == "true" ||
                           Request.Query.ContainsKey("fromModal") && Request.Query["fromModal"] == "true";
            
            // اگر از query parameter ?fromModal=true استفاده شده، به Edit برگرد (برای iframe)
            if (fromModal)
            {
                return RedirectToAction(nameof(Edit), new { id = task.Id, fromModal = true, saved = true });
            }
            
            return RedirectToAction(nameof(Index), new { projectId = task.ProjectId });
        }


        // GET: Details
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _context.TaskItems
                .Include(t => t.Category)
                .Include(t => t.ProjectIssueType)
                .Include(t => t.ChildIssues)
                .Include(t => t.ParentTask)
                .Include(t => t.Project)
                .Include(t => t.AssignedUser)
                .Include(t => t.WorkflowStatus)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            // بررسی دسترسی کاربر به تسک
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == task.ProjectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این تسک دسترسی ندارید.";
                return RedirectToAction(nameof(Index), new { projectId = task.ProjectId });
            }

            ViewBag.CurrentUserId = userId;
            return View(task);
        }

       

        // GET: Delete
        public async Task<IActionResult> Delete(int id)
        {
            var task = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();
            return View(task);
        }

        // POST: Delete
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            // load the task
            var task = await _context.TaskItems.FirstOrDefaultAsync(t => t.Id == id);
            if (task == null)
                return RedirectToAction("Index", "Projects");

            var projectId = task.ProjectId; // ذخیره projectId قبل از حذف

            // detach children to satisfy self-referencing FK before delete
            var children = await _context.TaskItems
                .Where(t => t.ParentTaskId == id)
                .ToListAsync();

            foreach (var child in children)
            {
                child.ParentTaskId = null;
                _context.TaskItems.Update(child);
            }

            _context.TaskItems.Remove(task);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { projectId = projectId });
        }

        // 💬 دریافت کامنت‌های یک تسک (Ajax)
        [HttpGet]
        public async Task<IActionResult> GetComments(int taskId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی
            var task = await _context.TaskItems
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return NotFound();

            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == task.ProjectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
                return Forbid();

            var comments = await _context.TaskComments
                .Include(c => c.Attachments)
                .Where(c => c.TaskId == taskId && !c.IsDeleted)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new TaskCommentVm
                {
                    Id = c.Id,
                    Message = c.Message,
                    UserId = c.UserId,
                    UserName = c.UserName,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    IsEdited = c.IsEdited,
                    IsCurrentUser = c.UserId == userId,
                    Attachments = c.Attachments.Select(a => new TaskCommentAttachment
                    {
                        Id = a.Id,
                        FileName = a.FileName,
                        FilePath = a.FilePath,
                        FileType = a.FileType,
                        FileSize = a.FileSize,
                        MimeType = a.MimeType,
                        UploadedAt = a.UploadedAt
                    }).ToList()
                })
                .ToListAsync();

            return Json(comments);
        }

        // 💬 ارسال کامنت جدید (Ajax)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PostComment(TaskCommentCreateVm vm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var currentUser = await _userManager.GetUserAsync(User);

            if (currentUser == null)
                return Unauthorized();

            // بررسی دسترسی
            var task = await _context.TaskItems
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == vm.TaskId);

            if (task == null)
                return NotFound();

            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == task.ProjectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
                return Forbid();

            // حداقل باید پیام یا فایل داشته باشد
            if (string.IsNullOrWhiteSpace(vm.Message) && (vm.Attachments == null || !vm.Attachments.Any()))
                return BadRequest("باید حداقل یک پیام یا فایل ارسال کنید");

            var comment = new TaskComment
            {
                Message = string.IsNullOrWhiteSpace(vm.Message) ? null : vm.Message,
                TaskId = vm.TaskId,
                UserId = userId!,
                UserName = currentUser.FullName ?? currentUser.UserName ?? "کاربر",
                CreatedAt = DateTime.UtcNow
            };

            _context.TaskComments.Add(comment);
            await _context.SaveChangesAsync();

            // آپلود فایل‌های پیوست
            var uploadedAttachments = new List<TaskCommentAttachment>();
            
            if (vm.Attachments != null && vm.Attachments.Any())
            {
                var uploadErrors = new List<string>();
                
                foreach (var file in vm.Attachments)
                {
                    var uploadResult = await _fileUploadService.UploadFileAsync(file, "task-comments");
                    
                    if (uploadResult.Success)
                    {
                        var attachment = new TaskCommentAttachment
                        {
                            TaskCommentId = comment.Id,
                            FileName = file.FileName,
                            FilePath = uploadResult.FilePath,
                            FileType = _fileUploadService.GetFileType(file.FileName),
                            FileSize = file.Length,
                            MimeType = file.ContentType,
                            UploadedAt = DateTime.UtcNow
                        };
                        
                        _context.TaskCommentAttachments.Add(attachment);
                        uploadedAttachments.Add(attachment);
                    }
                    else
                    {
                        uploadErrors.Add($"{file.FileName}: {uploadResult.Error}");
                    }
                }
                
                // اگر خطا در آپلود وجود داشت، کامنت رو حذف کن و خطا برگردون
                if (uploadErrors.Any())
                {
                    _context.TaskComments.Remove(comment);
                    await _context.SaveChangesAsync();
                    
                    return BadRequest($"خطا در آپلود فایل‌ها:\n{string.Join("\n", uploadErrors)}");
                }
                
                await _context.SaveChangesAsync();
            }

            return Json(new TaskCommentVm
            {
                Id = comment.Id,
                Message = comment.Message,
                UserId = comment.UserId,
                UserName = comment.UserName,
                CreatedAt = comment.CreatedAt,
                IsCurrentUser = true,
                Attachments = uploadedAttachments.Select(a => new TaskCommentAttachment
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileType = a.FileType,
                    FileSize = a.FileSize,
                    MimeType = a.MimeType,
                    UploadedAt = a.UploadedAt
                }).ToList()
            });
        }

        // 💬 حذف کامنت (Ajax)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteComment(int commentId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var comment = await _context.TaskComments
                .Include(c => c.Task)
                .ThenInclude(t => t.Project)
                .Include(c => c.Attachments)
                .FirstOrDefaultAsync(c => c.Id == commentId);

            if (comment == null)
                return NotFound();

            // فقط صاحب کامنت یا سازنده پروژه می‌تواند حذف کند
            var isOwner = comment.UserId == userId;
            var isProjectCreator = comment.Task.Project.CreatorUserId == userId;

            if (!isOwner && !isProjectCreator)
                return Forbid();

            // حذف فایل‌های پیوست
            foreach (var attachment in comment.Attachments)
            {
                _fileUploadService.DeleteFile(attachment.FilePath);
            }

            comment.IsDeleted = true;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        private async Task MarkTaskAndDescendantsCompletedAsync(TaskItem task)
        {
            await _context.Entry(task).Collection(t => t.ChildIssues).LoadAsync();

            foreach (var child in task.ChildIssues)
            {
                await MarkTaskAndDescendantsCompletedAsync(child);
            }

            if (!task.IsCompleted)
            {
                task.IsCompleted = true;
                task.UpdatedAt = DateTime.UtcNow;
            }
        }

        private async Task UpdateParentCompletionStatusAsync(int? parentId)
        {
            if (!parentId.HasValue)
            {
                return;
            }

            var parent = await _context.TaskItems
                .Include(p => p.ChildIssues)
                .FirstOrDefaultAsync(p => p.Id == parentId.Value);

            if (parent == null)
            {
                return;
            }

            var hasChildren = parent.ChildIssues.Any();
            if (hasChildren)
            {
                var allChildrenCompleted = parent.ChildIssues.All(c => c.IsCompleted);
                if (parent.IsCompleted != allChildrenCompleted)
                {
                    parent.IsCompleted = allChildrenCompleted;
                    parent.UpdatedAt = DateTime.UtcNow;
                }
            }

            await UpdateParentCompletionStatusAsync(parent.ParentTaskId);
        }

        private async Task MarkAncestorsIncompleteAsync(int? parentId)
        {
            if (!parentId.HasValue)
            {
                return;
            }

            var parent = await _context.TaskItems
                .FirstOrDefaultAsync(t => t.Id == parentId.Value);

            if (parent == null)
            {
                return;
            }

            if (parent.IsCompleted)
            {
                parent.IsCompleted = false;
                parent.UpdatedAt = DateTime.UtcNow;
            }

            await MarkAncestorsIncompleteAsync(parent.ParentTaskId);
        }

    }
}
