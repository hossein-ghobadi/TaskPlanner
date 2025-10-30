using DNTPersianUtils.Core;

using Endpoint.Site.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskPlanner.Persistence.Contexts;
using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using TaskPlanner.Domain.Entities.Users;
using System.Security.Claims;
using System.Text.Json;
using TaskPlanner.Application.Services.FileUpload;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class TasksController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IFileUploadService _fileUploadService;

        public TasksController(MVPTestDatabaseContext context, UserManager<User> userManager, IFileUploadService fileUploadService)
        {
            _context = context;
            _userManager = userManager;
            _fileUploadService = fileUploadService;

        }


        // GET: لیست تسک‌ها
        [Authorize]
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var userProjectIds = await _context.Projects
                .Where(p => p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId))
                .Select(p => p.Id)
                .ToListAsync();

            var tasks = await _context.TaskItems
                .Include(t => t.Project)
                .Include(t => t.Category)
                .Include(t => t.Sprint)
                .Include(t => t.WorkflowStatus)
                .Include(t => t.ChildIssues)
                .ThenInclude(st => st.ChildIssues)
                .Where(t => userProjectIds.Contains(t.ProjectId) || t.AssignedUserId == userId)
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
                    u => $"{u.FullName ?? u.UserName} ({u.PhoneNumber})"
                );

            ViewBag.UserLookup = userLookup;
            ViewBag.Categories = await _context.TaskCategories.ToListAsync(); // 🔹 اضافه شد

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

            var subTask = new TaskItem
            {
                Title = title,
                Description = null,
                IssueType = IssueType.Subtask, // ✅ به صورت خودکار Subtask
                IssueKey = project.GenerateNextIssueKey(), // ✅ IssueKey اتوماتیک
                StartDate = DateTime.Today,
                DueDate = parent.DueDate, // وراثت DueDate از parent
                ProjectId = parent.ProjectId,
                CategoryId = categoryId > 0 ? categoryId : parent.CategoryId, // اگه category نداد، از parent بگیر
                ParentTaskId = parent.Id,
                AssignedUserId = parent.AssignedUserId, // وراثت مسئول از parent
                IsCompleted = false,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.TaskItems.Add(subTask);
            _context.Projects.Update(project); // به‌روزرسانی LastIssueNumber

            await _context.SaveChangesAsync();

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

            // 🔍 دریافت Parent Issue
            var parent = await _context.TaskItems
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == parentId);

            if (parent == null)
                return NotFound("Parent Issue یافت نشد.");

            // 🎯 Smart Logic: تعیین IssueType بر اساس Parent
            IssueType finalIssueType;

            if (parent.IssueType == IssueType.Epic)
            {
                // اگه Parent = Epic → باید IssueType مشخص شده باشد
                if (!issueType.HasValue)
                {
                    return BadRequest("برای افزودن Issue به Epic، باید نوع Issue (Story/Task/Bug) مشخص شود.");
                }

                // فقط Story, Task, Bug مجاز هستن
                if (issueType.Value != IssueType.Story && 
                    issueType.Value != IssueType.Task && 
                    issueType.Value != IssueType.Bug)
                {
                    return BadRequest("Epic فقط می‌تواند Story، Task یا Bug داشته باشد.");
                }

                finalIssueType = issueType.Value;
            }
            else
            {
                // اگه Parent = Story/Task/Bug → خودکار Subtask
                finalIssueType = IssueType.Subtask;
            }

            // 🔒 Validation: بررسی سلسله مراتبی Jira
            var validationResult = IssueHierarchyValidator.ValidateParentChild(
                finalIssueType,
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

            // 🏗️ ساخت Issue جدید
            var newIssue = new TaskItem
            {
                Title = title,
                Description = null,
                IssueType = finalIssueType,
                IssueKey = project.GenerateNextIssueKey(),
                StartDate = DateTime.Today,
                DueDate = parent.DueDate, // وراثت DueDate از parent
                ProjectId = parent.ProjectId,
                CategoryId = categoryId > 0 ? categoryId : parent.CategoryId,
                ParentTaskId = parent.Id,
                AssignedUserId = parent.AssignedUserId, // وراثت مسئول از parent
                StoryPoints = storyPoints, // فقط برای Story/Task
                IsCompleted = false,
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.TaskItems.Add(newIssue);
            _context.Projects.Update(project); // به‌روزرسانی LastIssueNumber

            await _context.SaveChangesAsync();

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
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return NotFound();

            // 🔹 تابع بازگشتی برای حذف Child Issues
            void DeleteRecursive(TaskItem item)
            {
                foreach (var sub in item.ChildIssues.ToList())
                {
                    DeleteRecursive(sub);
                }
                _context.TaskItems.Remove(item);
            }

            DeleteRecursive(task);
            await _context.SaveChangesAsync();

            return Ok();
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
            if (!data.TryGetProperty("toStatusId", out var toStatusProp) || toStatusProp.ValueKind != JsonValueKind.Number)
                return BadRequest("toStatusId الزامی است.");

            int taskId = taskIdProp.GetInt32();
            int toStatusId = toStatusProp.GetInt32();
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

            // وضعیت مقصد باید در همان پروژه باشد
            var toStatus = await _context.WorkflowStatuses
                .FirstOrDefaultAsync(ws => ws.Id == toStatusId && ws.ProjectId == task.ProjectId);
            if (toStatus == null)
                return BadRequest("وضعیت مقصد معتبر نیست.");

            int? fromStatusId = task.StatusId; // ممکن است null باشد

            // اعتبارسنجی Transition فقط وقتی fromStatus داریم
            WorkflowTransition? usedTransition = null;
            if (fromStatusId.HasValue)
            {
                usedTransition = await _context.WorkflowTransitions
                    .FirstOrDefaultAsync(tr => tr.ProjectId == task.ProjectId
                                            && tr.FromStatusId == fromStatusId.Value
                                            && tr.ToStatusId == toStatusId);
                if (usedTransition == null)
                    return BadRequest("این انتقال در Workflow پروژه مجاز نیست.");

                if (usedTransition.OnlyAssigneeCanTransition && task.AssignedUserId != userId)
                    return BadRequest("فقط مسئول Issue می‌تواند این انتقال را انجام دهد.");
            }

            // بروزرسانی وضعیت Issue
            task.StatusId = toStatus.Id;
            task.UpdatedAt = DateTime.UtcNow;
            if (toStatus.IsFinal)
            {
                task.IsCompleted = true;
            }

            // ثبت تاریخچه
            var history = new IssueStatusHistory
            {
                TaskId = task.Id,
                FromStatusId = fromStatusId,
                ToStatusId = toStatus.Id,
                TransitionId = usedTransition?.Id,
                ChangedByUserId = userId,
                ChangeReason = reason,
                ChangedAt = DateTime.UtcNow
            };
            _context.IssueStatusHistories.Add(history);
            await _context.SaveChangesAsync();

            return Ok(new
            {
                success = true,
                message = $"وضعیت Issue به '{toStatus.Name}' تغییر کرد.",
                toStatusId = toStatus.Id,
                toStatusName = toStatus.Name,
                isCompleted = task.IsCompleted
            });
        }

        // 📅 برنامه هفتگی کاربر
        public async Task<IActionResult> Weekly(int? offset)
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
                    u => $"{u.FullName ?? u.UserName} ({u.PhoneNumber})"
                );

            ViewBag.UserLookup = userLookup;
            ViewBag.StartOfWeek = startOfWeek;
            ViewBag.EndOfWeek = endOfWeek;
            ViewBag.Offset = weekOffset;

            return View(weeklyTasks);
        }
        [HttpGet]
        public async Task<IActionResult> Create(int? projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // 🎯 فقط پروژه‌هایی که کاربر عضو یا سازنده‌ی آنهاست
            var userProjects = await _context.Projects
                .Where(p => p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId))
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();

            ViewBag.Projects = userProjects;

            // 📂 دسته‌بندی‌ها
            ViewBag.Categories = await _context.TaskCategories.ToListAsync();

            // 🧩 تسک‌های همین پروژه‌ها برای انتخاب Parent Task
            ViewBag.Tasks = await _context.TaskItems
                .Where(t => userProjects.Select(p => p.Id).Contains(t.ProjectId))
                .ToListAsync();

            // 🏃 اسپرینت‌ها و چرخه کاری حذف شدند - تسک‌ها مستقل ایجاد می‌شوند

            // 👤 کاربران قابل انتخاب برای AssignedUserId
            var assignedUsers = new List<dynamic>();

            // 1️⃣ اعضای پذیرفته‌شده پروژه (اگر projectId داده شده باشد)
            if (projectId.HasValue)
            {
                var acceptedProjectMembers = await _context.ProjectInvitations
                    .Where(i => i.ProjectId == projectId.Value && i.Status == InvitationStatus.Accepted)
                    .Select(i => i.InviteeId)
                    .ToListAsync();

                foreach (var memberId in acceptedProjectMembers)
                {
                    var userInfo = await _userManager.FindByIdAsync(memberId);
                    if (userInfo != null)
                    {
                        assignedUsers.Add(new
                        {
                            Id = memberId,
                            Name = $"{userInfo.FullName ?? userInfo.UserName} ({userInfo.PhoneNumber})"
                        });
                    }
                }
            }

            // 2️⃣ کاربران دعوت‌شده سیستمی که با کاربر فعلی ارتباط دارند (دعوت Accepted)
            var acceptedSystemRelations = await _context.ProjectInvitations
                .Where(i =>
                    i.Status == InvitationStatus.Accepted &&
                    (
                        i.InviterId == userId ||      // کاربر دعوت‌کننده بوده
                        i.InviteeId == userId         // کاربر دعوت‌شده بوده
                    ) &&
                    i.ProjectId == null)              // دعوت سیستمی
                .Select(i => i.InviterId == userId ? i.InviteeId : i.InviterId)
                .Distinct()
                .ToListAsync();

            foreach (var relatedUserId in acceptedSystemRelations)
            {
                var userInfo = await _userManager.FindByIdAsync(relatedUserId);
                if (userInfo != null && !assignedUsers.Any(u => u.Id == relatedUserId))
                {
                    assignedUsers.Add(new
                    {
                        Id = relatedUserId,
                        Name = $"{userInfo.FullName ?? userInfo.UserName} ({userInfo.PhoneNumber})"
                    });
                }
            }

            // 3️⃣ خود کاربر لاگین‌شده نیز باید در لیست باشد
            var currentUser = await _userManager.FindByIdAsync(userId);
            if (currentUser != null && !assignedUsers.Any(u => u.Id == userId))
            {
                assignedUsers.Add(new
                {
                    Id = userId,
                    Name = $"{currentUser.FullName ?? currentUser.UserName} ({currentUser.PhoneNumber})"
                });
            }

            ViewBag.AssignedUsers = assignedUsers.OrderBy(u => u.Name).ToList();

            // 📦 مدل اولیه
            var model = new TaskCreateVm();
            if (projectId.HasValue)
                model.ProjectId = projectId.Value;

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

            // ✅ اگر AssignedUserId ست شده، حتماً عضو پروژه باشد
            if (!string.IsNullOrWhiteSpace(vm.AssignedUserId))
            {
                var isMember = await _context.ProjectMembers
                    .AnyAsync(m => m.ProjectId == vm.ProjectId && m.UserId == vm.AssignedUserId);
                if (!isMember)
                {
                    ModelState.AddModelError(nameof(vm.AssignedUserId), "کاربر انتخاب‌شده عضو این پروژه نیست.");
                    await FillListsForCreate(vm.ProjectId);
                    return View(vm);
                }
            }

            _context.TaskItems.Add(new TaskItem
            {
                Title = vm.Title,
                Description = vm.Description,
                IssueType = vm.IssueType,
                CategoryId = vm.CategoryId,
                ParentTaskId = vm.ParentId,
                ProjectId = vm.ProjectId,
                StartDate = start.Value,
                DueDate = due,
                AssignedUserId = vm.AssignedUserId,
                StoryPoints = vm.StoryPoints
            });

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        private async Task FillListsForCreate(int projectId)
        {
            ViewBag.Categories = await _context.TaskCategories.ToListAsync();
            ViewBag.Tasks = await _context.TaskItems.ToListAsync();
            ViewBag.Projects = await _context.Projects.ToListAsync();

            var memberIds = await _context.ProjectMembers
                .Where(m => m.ProjectId == projectId)
                .Select(m => m.UserId)
                .ToListAsync();

            ViewBag.AssignedUsers = memberIds.Select(id => new { Id = id, Name = id }).ToList();
        }


        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var task = await _context.TaskItems
                .Include(t => t.Project)
                .Include(t => t.Category)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            var pc = new System.Globalization.PersianCalendar();
            string ToJalali(DateTime d) => $"{pc.GetYear(d):0000}/{pc.GetMonth(d):00}/{pc.GetDayOfMonth(d):00}";

            var vm = new TaskEditVm
            {
                Id = task.Id,
                Title = task.Title,
                Description = task.Description,
                CategoryId = task.CategoryId,
                ParentId = task.ParentTaskId,
                ProjectId = task.ProjectId,
                StartDateSh = ToJalali(task.StartDate),
                DueDateSh = task.DueDate.HasValue ? ToJalali(task.DueDate.Value) : null,
                AssignedUserId = task.AssignedUserId
            };

            ViewBag.Categories = _context.TaskCategories.ToList();
            ViewBag.Tasks = _context.TaskItems.Where(t => t.Id != id).ToList();
            ViewBag.Projects = _context.Projects.ToList();

            // 🔹 گرفتن کاربران تاییدشده پروژه برای انتخاب "مسئول تسک"
            var acceptedMembers = await _context.ProjectInvitations
                .Where(i => i.ProjectId == task.ProjectId && i.Status == InvitationStatus.Accepted)
                .Select(i => i.InviteeId)
                .ToListAsync();

            var users = new List<dynamic>();
            foreach (var userId in acceptedMembers)
            {
                var userInfo = await _userManager.FindByIdAsync(userId);
                if (userInfo != null)
                    users.Add(new { Id = userId, Name = $"{userInfo.FullName} ({userInfo.PhoneNumber})" });
            }
            ViewBag.AssignedUsers = users;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(TaskEditVm vm)
        {
            if (!ModelState.IsValid)
            {
                ViewBag.Categories = _context.TaskCategories.ToList();
                ViewBag.Tasks = _context.TaskItems.Where(t => t.Id != vm.Id).ToList();
                ViewBag.Projects = _context.Projects.ToList();
                return View(vm);
            }

            var task = await _context.TaskItems.FindAsync(vm.Id);
            if (task == null) return NotFound();

            task.Title = vm.Title;
            task.Description = vm.Description;
            task.CategoryId = vm.CategoryId;
            task.ParentTaskId = vm.ParentId;
            task.ProjectId = vm.ProjectId;
            task.StartDate = vm.StartDateSh.ToGregorianDateTime()!.Value;
            task.DueDate = string.IsNullOrWhiteSpace(vm.DueDateSh)
                ? null
                : vm.DueDateSh.ToGregorianDateTime();

            // 👇 مسئول تسک
            task.AssignedUserId = vm.AssignedUserId;

            _context.Update(task);
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }


        // GET: Details
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _context.TaskItems
                .Include(t => t.Category)
                .Include(t => t.ChildIssues)
                .Include(t => t.Project)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null) return NotFound();

            // بررسی دسترسی کاربر به تسک
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == task.ProjectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این تسک دسترسی ندارید.";
                return RedirectToAction(nameof(Index));
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
                return RedirectToAction(nameof(Index));

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
            return RedirectToAction(nameof(Index));
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

    }
}
