using DocumentFormat.OpenXml.Spreadsheet;
using Endpoint.Site.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Domain.Entities.Users;
using TaskPlanner.Persistence.Contexts;
using TaskPlanner.Application.Services.ProjectService;
using Microsoft.Data.SqlClient;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class ProjectsController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IProjectQueryService _projectQueryService;
        private readonly IProjectCommandService _projectCommandService;

        public ProjectsController(
            MVPTestDatabaseContext context,
            UserManager<User> userManager,
            IProjectQueryService projectQueryService,
            IProjectCommandService projectCommandService)
        {
            _context = context;
            _userManager = userManager;
            _projectQueryService = projectQueryService;
            _projectCommandService = projectCommandService;
        }
        // 📌 لیست پروژه‌ها
        //public async Task<IActionResult> Index()
        //{
        //    var projects = await _context.Projects.Where(p=>p.)
        //        .Include(p => p.Tasks)
        //        .ToListAsync();
        //    return View(projects);
        //}

        //📌 جزئیات پروژه
        //public async Task<IActionResult> Details(int id)
        //{
        //    var project = await _context.Projects
        //        .Include(p => p.Tasks)
        //        .Include(p => p.Members)
        //        .FirstOrDefaultAsync(p => p.Id == id);

        //    if (project == null) return NotFound();
        //    var creator = await _userManager.FindByIdAsync(project.CreatorUserId);
        //    var vm = new ProjectDetailsVm
        //    {
        //        Id = project.Id,
        //        Name = project.Name,
        //        Description = project.Description,
        //        CreatorUserName = creator != null ? creator.FullName : "", // موقتی
        //        Tasks = project.Tasks.ToList()
        //    };

        //    // 👇 اینجا باید اضافه بشه
        //    var memberUsernames = new List<string>();
        //    foreach (var member in project.Members)
        //    {
        //        var userInfo = await _userManager.FindByIdAsync(member.UserId);
        //        memberUsernames.Add($"{userInfo?.FullName ?? "ناشناس"} ({userInfo?.PhoneNumber})");
        //    }
        //    vm.MemberUserNames = memberUsernames;

        //    return View(vm);
        //}


        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var projects = await _projectQueryService.GetUserProjectsAsync(userId);

            // Active sprint lookup for quick access in cards
            var projectIds = projects.Select(p => p.Id).ToList();
            var activeSprints = await _context.Sprints
                .Where(s => projectIds.Contains(s.ProjectId) && s.Status == SprintStatus.Active)
                .Select(s => new { s.ProjectId, s.Id })
                .ToListAsync();

            ViewBag.ActiveSprintByProject = activeSprints
                .GroupBy(x => x.ProjectId)
                .ToDictionary(g => g.Key, g => g.First().Id);

            // دریافت دعوت‌های در انتظار برای سایدبار
            var user = await _userManager.FindByIdAsync(userId);
            var phone = user?.Phone;
            
            // دعوت‌های پروژه در انتظار
            var pendingProjectInvitations = await _context.ProjectInvitations
                .Include(i => i.Project)
                .Where(i => i.InviteePhone == phone && i.ProjectId != null && i.Status == InvitationStatus.Pending)
                .OrderByDescending(i => i.CreatedAt)
                .Take(5)
                .ToListAsync();

            // دعوت‌های سیستم در انتظار
            var pendingSystemInvitations = await _context.ProjectInvitations
                .Where(i => i.InviteePhone == phone && i.ProjectId == null && i.Status == InvitationStatus.Pending)
                .OrderByDescending(i => i.CreatedAt)
                .Take(5)
                .ToListAsync();

            // دریافت نام دعوت‌کنندگان
            var inviterIds = pendingProjectInvitations.Select(i => i.InviterId)
                .Concat(pendingSystemInvitations.Select(i => i.InviterId))
                .Distinct()
                .ToList();

            var inviterLookup = await _userManager.Users
                .Where(u => inviterIds.Contains(u.Id))
                .ToDictionaryAsync(u => u.Id, u => 
                    !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : (u.UserName ?? "کاربر ناشناس"));

            ViewBag.PendingProjectInvitations = pendingProjectInvitations;
            ViewBag.PendingSystemInvitations = pendingSystemInvitations;
            ViewBag.InviterLookup = inviterLookup;

            // دریافت یادداشت‌های شخصی اخیر
            var recentNotes = await _context.PersonalNotes
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();

            ViewBag.RecentNotes = recentNotes;

            return View(projects);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var dto = await _projectQueryService.GetProjectDetailsDtoAsync(id);
                
                // محاسبه آمار تسک‌ها (از پیش محاسبه شده برای بهینه‌سازی)
                var tasks = dto.Tasks ?? new List<TaskItem>();
                var totalTasks = dto.TotalTasks;
                var completedTasks = dto.CompletedTasks;
                var progressPercentage = dto.ProgressPercentage;
                
                // محاسبه تعداد تسک‌های هر دسته‌بندی (از پیش محاسبه شده برای بهینه‌سازی)
                var categoryTaskCounts = dto.CategoryTaskCounts ?? new Dictionary<int, int>();
                
                var vm = new ProjectDetailsVm
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Description = dto.Description,
                    CreatorUserId = dto.CreatorUserId,
                    CreatorUserName = dto.CreatorUserName,
                    MemberUserNames = dto.MemberUserNames,
                    Members = dto.Members.Select(m => new ProjectMemberInfo
                    {
                        UserId = m.UserId,
                        DisplayName = m.DisplayName
                    }).ToList(),
                    Tasks = tasks,
                    Invitations = dto.Invitations.Select(i => new ProjectInvitationVm
                    {
                        Id = i.Id,
                        InviteePhone = i.InviteePhone,
                        Status = i.Status,
                        CreatedAt = i.CreatedAt
                    }).ToList(),
                    Categories = dto.Categories,
                    ActiveSprintId = dto.ActiveSprintId,
                    ActiveSprintName = dto.ActiveSprintName,
                    // مقادیر از پیش محاسبه شده
                    TotalTasks = totalTasks,
                    CompletedTasks = completedTasks,
                    ProgressPercentage = progressPercentage,
                    CategoryTaskCounts = categoryTaskCounts
                };
                
                return View(vm);
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }

        private static string ToJalaliDate(DateTime date)
        {
            var pc = new System.Globalization.PersianCalendar();
            return $"{pc.GetYear(date):0000}/{pc.GetMonth(date):00}/{pc.GetDayOfMonth(date):00}";
        }

        // 📌 ایجاد پروژه
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var availableUsers = await _projectQueryService.GetAvailableUsersForCreateAsync(userId);
            var pendingInvites = await _projectQueryService.GetPendingInvitationsAsync(userId);

            ViewBag.Users = availableUsers.Select(u => new { Id = u.Id, DisplayName = u.DisplayName }).ToList();
            ViewBag.PendingInvitations = pendingInvites ?? new List<string>();

            return View(new ProjectCreateVm());
        }



        // افزودن کاربر به پروژه
        [HttpPost]
        public async Task<IActionResult> AddMember(int projectId, string userId)
        {
            var member = new ProjectMember
            {
                ProjectId = projectId,
                UserId = userId
            };
            _context.ProjectMembers.Add(member);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", new { id = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectCreateVm vm)
        {
            if (!ModelState.IsValid)
            {
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var availableUsers = await _projectQueryService.GetAvailableUsersForCreateAsync(userId);
                ViewBag.Users = availableUsers.Select(u => new { Id = u.Id, DisplayName = u.DisplayName }).ToList();
                return View(vm);
            }

            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["Error"] = "کاربر لاگین‌شده یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var dto = new CreateProjectDto
                {
                    Name = vm.Name,
                    Description = vm.Description,
                    SelectedUserIds = vm.SelectedUserIds ?? new List<string>()
                };

                var projectId = await _projectCommandService.CreateProjectAsync(dto, currentUser.Id);

                var pendingInvitations = await _context.ProjectInvitations
                    .CountAsync(i => i.InviterId == currentUser.Id && i.ProjectId == projectId && i.Status == InvitationStatus.Pending);

                TempData["Success"] = $"پروژه «{vm.Name}» با موفقیت ایجاد شد و {pendingInvitations} دعوت در انتظار به آن متصل گردید.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"خطا در ایجاد پروژه: {ex.Message}";
                var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var availableUsers = await _projectQueryService.GetAvailableUsersForCreateAsync(userId);
                ViewBag.Users = availableUsers.Select(u => new { Id = u.Id, DisplayName = u.DisplayName }).ToList();
                return View(vm);
            }
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> Edit(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["Error"] = "کاربر لاگین‌شده یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                var project = await _projectQueryService.GetProjectWithDetailsAsync(id);
                if (project == null)
                    return NotFound();

                // بررسی اینکه فقط سازنده پروژه می‌تواند آن را ویرایش کند
                var projectEntity = await _context.Projects.FindAsync(id);
                if (projectEntity == null || projectEntity.CreatorUserId != currentUser.Id)
                {
                    TempData["Error"] = "فقط سازنده پروژه می‌تواند آن را ویرایش کند.";
                    return RedirectToAction(nameof(Index));
                }

                var availableUsers = await _projectQueryService.GetAvailableUsersForEditAsync(currentUser.Id, id);

                ViewBag.Users = availableUsers.Select(u => new { Id = u.Id, DisplayName = u.DisplayName }).ToList();

                var vm = new ProjectEditVm
                {
                    Id = project.Id,
                    Name = project.Name,
                    Description = project.Description,
                    SelectedUserIds = project.Members.Select(m => m.UserId).ToList()
                };

                return View(vm);
            }
            catch (InvalidOperationException)
            {
                return NotFound();
            }
        }



        [HttpPost("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProjectEditVm vm)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["Error"] = "کاربر لاگین‌شده یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            // بررسی اینکه فقط سازنده پروژه می‌تواند آن را ویرایش کند
            var projectEntity = await _context.Projects.FindAsync(vm.Id);
            if (projectEntity == null)
            {
                TempData["Error"] = "پروژه یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            if (projectEntity.CreatorUserId != currentUser.Id)
            {
                TempData["Error"] = "فقط سازنده پروژه می‌تواند آن را ویرایش کند.";
                return RedirectToAction(nameof(Index));
            }

            if (!ModelState.IsValid)
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                try
                {
                    var availableUsers = await _projectQueryService.GetAvailableUsersForEditAsync(currentUserId, vm.Id);
                    ViewBag.Users = availableUsers.Select(u => new { Id = u.Id, DisplayName = u.DisplayName }).ToList();
                }
                catch
                {
                    ViewBag.Users = new List<dynamic>();
                }
                return View(vm);
            }

            try
            {
                var dto = new UpdateProjectDto
                {
                    Id = vm.Id,
                    Name = vm.Name,
                    Description = vm.Description,
                    SelectedUserIds = vm.SelectedUserIds ?? new List<string>()
                };

                await _projectCommandService.UpdateProjectAsync(dto);
                TempData["Success"] = "تغییرات پروژه با موفقیت ذخیره شد ✅";

                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
        }



        // 📌 حذف پروژه
        [HttpGet("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["Error"] = "کاربر لاگین‌شده یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            // بررسی اینکه فقط سازنده پروژه می‌تواند آن را حذف کند
            if (project.CreatorUserId != currentUser.Id)
            {
                TempData["Error"] = "فقط سازنده پروژه می‌تواند آن را حذف کند.";
                return RedirectToAction(nameof(Index));
            }

            return View(project);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["Error"] = "کاربر لاگین‌شده یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _projectCommandService.DeleteProjectAsync(id, currentUser.Id);
                TempData["Success"] = "پروژه با موفقیت حذف شد.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                // بررسی نوع خطای Foreign Key
                if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 547)
                {
                    TempData["Error"] = "نمی‌توان پروژه را حذف کرد زیرا دارای وابستگی‌های مهمی است. لطفاً ابتدا تمام کارهای مرتبط با اسپرینت‌ها را بررسی کنید.";
                }
                else
                {
                    TempData["Error"] = "خطا در حذف پروژه. لطفاً دوباره تلاش کنید یا با پشتیبانی تماس بگیرید.";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "خطای غیرمنتظره‌ای رخ داد. لطفاً دوباره تلاش کنید.";
                return RedirectToAction(nameof(Index));
            }
        }

        // ✅ برخی View ها ممکن است به طور مستقیم به /DeleteConfirmed پست کنند
        [HttpPost]
        [ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmedRoute(int id)
        {
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["Error"] = "کاربر لاگین‌شده یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            try
            {
                await _projectCommandService.DeleteProjectAsync(id, currentUser.Id);
                TempData["Success"] = "پروژه با موفقیت حذف شد.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                // بررسی نوع خطای Foreign Key
                if (ex.InnerException is SqlException sqlEx && sqlEx.Number == 547)
                {
                    TempData["Error"] = "نمی‌توان پروژه را حذف کرد زیرا دارای وابستگی‌های مهمی است. لطفاً ابتدا تمام کارهای مرتبط با اسپرینت‌ها را بررسی کنید.";
                }
                else
                {
                    TempData["Error"] = "خطا در حذف پروژه. لطفاً دوباره تلاش کنید یا با پشتیبانی تماس بگیرید.";
                }
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                TempData["Error"] = "خطای غیرمنتظره‌ای رخ داد. لطفاً دوباره تلاش کنید.";
                return RedirectToAction(nameof(Index));
            }
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteUserToProject(int projectId, string phone)
        {
            var inviterId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                await _projectCommandService.InviteUserToProjectAsync(projectId, phone, inviterId);
                TempData["InviteSuccess"] = $"دعوت برای شماره {phone} ارسال شد.";
            }
            catch (ArgumentException ex)
            {
                TempData["InviteError"] = ex.Message;
            }
            catch (InvalidOperationException ex)
            {
                if (ex.Message.Contains("قبلاً دعوت"))
                    TempData["InviteWarning"] = ex.Message;
                else
                    TempData["InviteError"] = ex.Message;
            }

            return RedirectToAction("Details", new { id = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteProjectInvite(int projectId, int inviteId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var invite = await _context.ProjectInvitations
                .FirstOrDefaultAsync(i => i.Id == inviteId && i.ProjectId == projectId && i.InviterId == currentUserId);

            if (invite == null)
            {
                TempData["InviteError"] = "دعوت مورد نظر یافت نشد.";
                return RedirectToAction("Details", new { id = projectId });
            }

            if (invite.Status != InvitationStatus.Pending)
            {
                TempData["InviteWarning"] = "فقط دعوت‌های در انتظار قابل حذف هستند.";
                return RedirectToAction("Details", new { id = projectId });
            }

            _context.ProjectInvitations.Remove(invite);
            await _context.SaveChangesAsync();

            TempData["InviteSuccess"] = "دعوت با موفقیت حذف شد.";
            return RedirectToAction("Details", new { id = projectId });
        }

        /// <summary>
        /// حذف عضو از پروژه
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int projectId, string memberUserId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrEmpty(memberUserId))
            {
                TempData["Error"] = "شناسه کاربر نامعتبر است.";
                return RedirectToAction("Details", new { id = projectId });
            }

            try
            {
                await _projectCommandService.RemoveMemberFromProjectAsync(projectId, memberUserId, currentUserId);
                
                TempData["Success"] = "عضو با موفقیت از پروژه حذف شد و تسک‌های مرتبط بدون مسئول شدند.";
                return RedirectToAction("Details", new { id = projectId });
            }
            catch (InvalidOperationException ex)
            {
                TempData["Error"] = ex.Message;
                return RedirectToAction("Details", new { id = projectId });
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"خطا در حذف عضو: {ex.Message}";
                return RedirectToAction("Details", new { id = projectId });
            }
        }

        // 📋 مدیریت انواع تسک (ProjectIssueTypes)
        [HttpGet("{projectId}")]
        public async Task<IActionResult> IssueTypes(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                TempData["Error"] = "پروژه یافت نشد.";
                return RedirectToAction("Index");
            }

            // بررسی دسترسی (فقط سازنده یا اعضای پروژه)
            var hasAccess = project.CreatorUserId == userId || 
                           await _context.ProjectInvitations
                               .AnyAsync(pi => pi.ProjectId == projectId && 
                                              pi.InviteeId == userId && 
                                              pi.Status == InvitationStatus.Accepted);

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index");
            }

            // نمایش همه issue type ها - مرتب بر اساس Order
            var issueTypes = await _context.ProjectIssueTypes
                .Where(pit => pit.ProjectId == projectId)
                .OrderBy(pit => pit.Order)
                .ThenBy(pit => pit.Level)
                .ThenBy(pit => pit.Name)
                .ToListAsync();

            ViewBag.ProjectId = projectId;
            ViewBag.ProjectName = project.Name;
            ViewBag.IsCreator = project.CreatorUserId == userId;

            return View(issueTypes);
        }

        // 📝 ایجاد نوع تسک جدید
        [HttpGet("{projectId}")]
        public async Task<IActionResult> CreateIssueType(int projectId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                TempData["Error"] = "پروژه یافت نشد.";
                return RedirectToAction("Index");
            }

            // بررسی دسترسی
            var hasAccess = project.CreatorUserId == userId || 
                           await _context.ProjectInvitations
                               .AnyAsync(pi => pi.ProjectId == projectId && 
                                              pi.InviteeId == userId && 
                                              pi.Status == InvitationStatus.Accepted);

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index");
            }

            ViewBag.ProjectId = projectId;
            ViewBag.ProjectName = project.Name;

            return View();
        }

        [HttpPost("{projectId}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateIssueType(int projectId, ProjectIssueType model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                TempData["Error"] = "پروژه یافت نشد.";
                return RedirectToAction("Index");
            }

            // بررسی دسترسی
            var hasAccess = project.CreatorUserId == userId || 
                           await _context.ProjectInvitations
                               .AnyAsync(pi => pi.ProjectId == projectId && 
                                              pi.InviteeId == userId && 
                                              pi.Status == InvitationStatus.Accepted);

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index");
            }

            // تنظیم خودکار ProjectId و Project برای جلوگیری از خطای validation
            model.ProjectId = projectId;
            model.Project = project;
            
            // حذف Project از ModelState validation چون navigation property است و به صورت دستی تنظیم می‌شود
            ModelState.Remove("Project");

            if (ModelState.IsValid)
            {
                try
                {
                    // بررسی تکراری نبودن نام
                    var exists = await _context.ProjectIssueTypes
                        .AnyAsync(pit => pit.ProjectId == projectId && 
                                        pit.Name == model.Name);

                    if (exists)
                    {
                        ModelState.AddModelError("Name", "این نام قبلاً استفاده شده است.");
                        ViewBag.ProjectId = projectId;
                        ViewBag.ProjectName = project.Name;
                        ViewBag.DefaultOrder = model.Order;
                        return View(model);
                    }

                    model.ProjectId = projectId;
                    model.Project = project;
                    model.IsCustom = true;
                    model.CreatedByUserId = userId;
                    model.CreatedAt = DateTime.UtcNow;

                    // فقط StoryLevel قابل ایجاد است
                    model.Level = IssueTypeLevel.StoryLevel;
                    
                    // همه انواع سفارشی هم‌ارز با Task هستند
                    model.BaseType = IssueType.Task;
                    
                    // همه StoryLevel ها باید این تنظیمات را داشته باشند
                    model.CanAddToSprint = true;
                    model.CanHaveChildren = true;
                    model.IncludeInReports = true;
                    
                    // Order برای customها همیشه 1 است
                    model.Order = 1;

                    _context.ProjectIssueTypes.Add(model);
                    await _context.SaveChangesAsync();

                    TempData["Success"] = "نوع تسک با موفقیت ایجاد شد.";
                    return RedirectToAction("IssueTypes", new { projectId });
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", "خطا در ایجاد نوع تسک در پایگاه داده. لطفاً دوباره تلاش کنید.");
                    if (ex.InnerException != null)
                    {
                        ModelState.AddModelError("", $"جزئیات خطا: {ex.InnerException.Message}");
                    }
                    ViewBag.ProjectId = projectId;
                    ViewBag.ProjectName = project.Name;
                    return View(model);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"خطای غیرمنتظره: {ex.Message}");
                    ViewBag.ProjectId = projectId;
                    ViewBag.ProjectName = project.Name;
                    return View(model);
                }
            }

            // تنظیم Project برای model در صورت وجود خطای validation
            model.ProjectId = projectId;
            model.Project = project;
            ModelState.Remove("Project");
            ViewBag.ProjectId = projectId;
            ViewBag.ProjectName = project.Name;
            return View(model);
        }

        // ✏️ ویرایش نوع تسک
        [HttpGet("{id}")]
        public async Task<IActionResult> EditIssueType(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var issueType = await _context.ProjectIssueTypes
                .Include(pit => pit.Project)
                .FirstOrDefaultAsync(pit => pit.Id == id);

            if (issueType == null)
            {
                TempData["Error"] = "نوع تسک یافت نشد.";
                return RedirectToAction("Index");
            }

            // بررسی دسترسی
            var hasAccess = issueType.Project.CreatorUserId == userId || 
                           await _context.ProjectInvitations
                               .AnyAsync(pi => pi.ProjectId == issueType.ProjectId && 
                                              pi.InviteeId == userId && 
                                              pi.Status == InvitationStatus.Accepted);

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index");
            }

            // بررسی اینکه آیا می‌تواند ویرایش شود (نوع‌های پیش‌فرض فقط توسط سازنده)
            if (!issueType.IsCustom && issueType.Project.CreatorUserId != userId)
            {
                TempData["Error"] = "فقط سازنده پروژه می‌تواند انواع پیش‌فرض را ویرایش کند.";
                return RedirectToAction("IssueTypes", new { projectId = issueType.ProjectId });
            }

            ViewBag.ProjectId = issueType.ProjectId;
            ViewBag.ProjectName = issueType.Project.Name;
            ViewBag.IsDefault = !issueType.IsCustom;

            return View(issueType);
        }

        [HttpPost("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditIssueType(int id, ProjectIssueType model)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var issueType = await _context.ProjectIssueTypes
                .Include(pit => pit.Project)
                .FirstOrDefaultAsync(pit => pit.Id == id);

            if (issueType == null)
            {
                TempData["Error"] = "نوع تسک یافت نشد.";
                return RedirectToAction("Index");
            }

            // بررسی دسترسی
            var hasAccess = issueType.Project.CreatorUserId == userId || 
                           await _context.ProjectInvitations
                               .AnyAsync(pi => pi.ProjectId == issueType.ProjectId && 
                                              pi.InviteeId == userId && 
                                              pi.Status == InvitationStatus.Accepted);

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index");
            }

            // بررسی اینکه آیا می‌تواند ویرایش شود
            if (!issueType.IsCustom && issueType.Project.CreatorUserId != userId)
            {
                TempData["Error"] = "فقط سازنده پروژه می‌تواند انواع پیش‌فرض را ویرایش کند.";
                return RedirectToAction("IssueTypes", new { projectId = issueType.ProjectId });
            }

            // تنظیم خودکار ProjectId و Project برای جلوگیری از خطای validation
            model.ProjectId = issueType.ProjectId;
            model.Project = issueType.Project;
            
            // حذف Project از ModelState validation چون navigation property است و به صورت دستی تنظیم می‌شود
            ModelState.Remove("Project");

            if (ModelState.IsValid)
            {
                try
                {
                    // بررسی تکراری نبودن نام (به جز خودش)
                    var exists = await _context.ProjectIssueTypes
                        .AnyAsync(pit => pit.ProjectId == issueType.ProjectId && 
                                        pit.Name == model.Name && 
                                        pit.Id != id);

                    if (exists)
                    {
                        ModelState.AddModelError("Name", "این نام قبلاً استفاده شده است.");
                        ViewBag.ProjectId = issueType.ProjectId;
                        ViewBag.ProjectName = issueType.Project.Name;
                        ViewBag.IsDefault = !issueType.IsCustom;
                        return View(model);
                    }

                // به‌روزرسانی فیلدها (Level و BaseType همیشه ثابت می‌مانند)
                issueType.Name = model.Name;
                issueType.Description = model.Description;
                issueType.Icon = model.Icon;
                issueType.Color = model.Color;
                // Order برای customها همیشه 1 است
                if (issueType.IsCustom)
                {
                    issueType.Order = 1;
                }
                else
                {
                    issueType.Order = model.Order;
                }
                
                // BaseType را تغییر نده - انواع سفارشی همیشه Task هستند
                if (issueType.IsCustom)
                {
                    issueType.BaseType = IssueType.Task;
                }
                else
                {
                    // برای انواع پیش‌فرض، BaseType را حفظ کن
                    issueType.BaseType = model.BaseType;
                }
                
                // تنظیمات بر اساس Level
                // Level را تغییر نده - Level اصلی را حفظ کن
                if (issueType.Level == IssueTypeLevel.StoryLevel)
                {
                    // StoryLevel: قابل افزودن به Sprint، قابل داشتن زیرتسک
                    issueType.CanAddToSprint = true;
                    issueType.CanHaveChildren = true;
                    issueType.IncludeInReports = true;
                }
                else if (issueType.Level == IssueTypeLevel.Epic)
                {
                    // Epic: غیرقابل افزودن به Sprint، قابل داشتن زیرتسک
                    issueType.CanAddToSprint = false;
                    issueType.CanHaveChildren = true;
                    issueType.IncludeInReports = true;
                }
                else if (issueType.Level == IssueTypeLevel.Subtask)
                {
                    // Subtask: غیرقابل افزودن به Sprint، غیرقابل داشتن زیرتسک
                    issueType.CanAddToSprint = false;
                    issueType.CanHaveChildren = false;
                    issueType.IncludeInReports = true;
                }

                    await _context.SaveChangesAsync();

                    TempData["Success"] = "نوع تسک با موفقیت به‌روزرسانی شد.";
                    return RedirectToAction("IssueTypes", new { projectId = issueType.ProjectId });
                }
                catch (DbUpdateException ex)
                {
                    ModelState.AddModelError("", "خطا در به‌روزرسانی نوع تسک در پایگاه داده. لطفاً دوباره تلاش کنید.");
                    if (ex.InnerException != null)
                    {
                        ModelState.AddModelError("", $"جزئیات خطا: {ex.InnerException.Message}");
                    }
                    // تنظیم Project برای model
                    model.ProjectId = issueType.ProjectId;
                    model.Project = issueType.Project;
                    ModelState.Remove("Project");
                    ViewBag.ProjectId = issueType.ProjectId;
                    ViewBag.ProjectName = issueType.Project.Name;
                    ViewBag.IsDefault = !issueType.IsCustom;
                    return View(model);
                }
                catch (Exception ex)
                {
                    ModelState.AddModelError("", $"خطای غیرمنتظره: {ex.Message}");
                    // تنظیم Project برای model
                    model.ProjectId = issueType.ProjectId;
                    model.Project = issueType.Project;
                    ModelState.Remove("Project");
                    ViewBag.ProjectId = issueType.ProjectId;
                    ViewBag.ProjectName = issueType.Project.Name;
                    ViewBag.IsDefault = !issueType.IsCustom;
                    return View(model);
                }
            }

            // تنظیم Project برای model در صورت وجود خطای validation
            model.ProjectId = issueType.ProjectId;
            model.Project = issueType.Project;
            ModelState.Remove("Project");
            ViewBag.ProjectId = issueType.ProjectId;
            ViewBag.ProjectName = issueType.Project.Name;
            ViewBag.IsDefault = !issueType.IsCustom;
            return View(model);
        }

        // 🗑️ حذف نوع تسک
        [HttpPost("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteIssueType(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var issueType = await _context.ProjectIssueTypes
                .Include(pit => pit.Project)
                .FirstOrDefaultAsync(pit => pit.Id == id);

            if (issueType == null)
            {
                TempData["Error"] = "نوع تسک یافت نشد.";
                return RedirectToAction("Index");
            }

            var projectId = issueType.ProjectId;

            // بررسی دسترسی
            var hasAccess = issueType.Project.CreatorUserId == userId || 
                           await _context.ProjectInvitations
                               .AnyAsync(pi => pi.ProjectId == issueType.ProjectId && 
                                              pi.InviteeId == userId && 
                                              pi.Status == InvitationStatus.Accepted);

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("IssueTypes", new { projectId });
            }

            // فقط کاستوم‌ها قابل حذف هستند
            if (!issueType.IsCustom)
            {
                TempData["Error"] = "فقط انواع تسک سفارشی قابل حذف هستند.";
                return RedirectToAction("IssueTypes", new { projectId });
            }

            // پیدا کردن نوع پیش‌فرض "کار" برای جایگزینی
            var defaultTaskType = await _context.ProjectIssueTypes
                .FirstOrDefaultAsync(pit => pit.ProjectId == projectId && 
                                            !pit.IsCustom && 
                                            pit.BaseType == IssueType.Task &&
                                            pit.Level == IssueTypeLevel.StoryLevel);

            if (defaultTaskType == null)
            {
                TempData["Error"] = "نوع پیش‌فرض کار یافت نشد. امکان حذف وجود ندارد.";
                return RedirectToAction("IssueTypes", new { projectId });
            }

            // پیدا کردن تمام تسک‌هایی که از این نوع استفاده می‌کنند
            var tasksWithThisType = await _context.TaskItems
                .Where(t => t.ProjectIssueTypeId == id)
                .ToListAsync();

            // تغییر نوع تسک‌های مرتبط به نوع پیش‌فرض
            foreach (var task in tasksWithThisType)
            {
                task.ProjectIssueTypeId = defaultTaskType.Id;
                task.IssueType = defaultTaskType.BaseType;
            }

            _context.ProjectIssueTypes.Remove(issueType);
            await _context.SaveChangesAsync();

            if (tasksWithThisType.Any())
            {
                TempData["Success"] = $"نوع تسک با موفقیت حذف شد. {tasksWithThisType.Count} تسک مرتبط به نوع پیش‌فرض \"{defaultTaskType.Name}\" تغییر وضعیت دادند.";
            }
            else
            {
                TempData["Success"] = "نوع تسک با موفقیت حذف شد.";
            }
            return RedirectToAction("IssueTypes", new { projectId });
        }

        // 🔄 به‌روزرسانی ترتیب انواع تسک
        [HttpPost("UpdateIssueTypeOrder")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateIssueTypeOrder([FromBody] List<IssueTypeOrderDto> orders)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (orders == null || !orders.Any())
            {
                return Json(new { success = false, message = "داده‌های نامعتبر." });
            }

            var issueTypeIds = orders.Select(o => o.Id).ToList();
            var issueTypes = await _context.ProjectIssueTypes
                .Include(pit => pit.Project)
                .Where(pit => issueTypeIds.Contains(pit.Id))
                .ToListAsync();

            if (!issueTypes.Any())
            {
                return Json(new { success = false, message = "انواع تسک یافت نشدند." });
            }

            // بررسی دسترسی (همه باید از یک پروژه باشند)
            var projectId = issueTypes.First().ProjectId;
            var project = await _context.Projects.FirstOrDefaultAsync(p => p.Id == projectId);

            if (project == null)
            {
                return Json(new { success = false, message = "پروژه یافت نشد." });
            }

            var hasAccess = project.CreatorUserId == userId || 
                           await _context.ProjectInvitations
                               .AnyAsync(pi => pi.ProjectId == projectId && 
                                              pi.InviteeId == userId && 
                                              pi.Status == InvitationStatus.Accepted);

            if (!hasAccess)
            {
                return Json(new { success = false, message = "شما به این پروژه دسترسی ندارید." });
            }

            foreach (var order in orders)
            {
                var issueType = issueTypes.FirstOrDefault(it => it.Id == order.Id);
                if (issueType != null)
                {
                    // فقط برای customها Order را به‌روزرسانی کن (همیشه 1)
                    // برای انواع پیش‌فرض، Order را حفظ کن
                    if (issueType.IsCustom)
                    {
                        issueType.Order = 1;
                    }
                    else
                    {
                        issueType.Order = order.Order;
                    }
                }
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "ترتیب با موفقیت به‌روزرسانی شد." });
        }

        // 🔄 Migration: به‌روزرسانی نام‌های پیش‌فرض Issue Types
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MigrateIssueTypeNames(int? projectId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            try
            {
                // پیدا کردن تمام انواع پیش‌فرض با نام‌های قدیمی
                var epicTypes = await _context.ProjectIssueTypes
                    .Where(pit => !pit.IsCustom && 
                                 pit.BaseType == IssueType.Epic && 
                                 pit.Name == "اپیک")
                    .ToListAsync();

                var taskTypes = await _context.ProjectIssueTypes
                    .Where(pit => !pit.IsCustom && 
                                 pit.BaseType == IssueType.Task && 
                                 pit.Level == IssueTypeLevel.StoryLevel &&
                                 pit.Name == "تسک")
                    .ToListAsync();

                var subtaskTypes = await _context.ProjectIssueTypes
                    .Where(pit => !pit.IsCustom && 
                                 pit.BaseType == IssueType.Subtask && 
                                 pit.Name == "زیرتسک")
                    .ToListAsync();

                int updatedCount = 0;

                // به‌روزرسانی نام Epic
                foreach (var epicType in epicTypes)
                {
                    epicType.Name = "ویژگی";
                    epicType.Description = "نوع پیش‌فرض ویژگی";
                    updatedCount++;
                }

                // به‌روزرسانی نام Task
                foreach (var taskType in taskTypes)
                {
                    taskType.Name = "کار";
                    taskType.Description = "نوع پیش‌فرض کار";
                    updatedCount++;
                }

                // به‌روزرسانی نام Subtask
                foreach (var subtaskType in subtaskTypes)
                {
                    subtaskType.Name = "کارک";
                    subtaskType.Description = "نوع پیش‌فرض کارک";
                    updatedCount++;
                }

                if (updatedCount > 0)
                {
                    await _context.SaveChangesAsync();
                    
                    // پیدا کردن projectId از اولین رکورد به‌روزرسانی شده
                    int? foundProjectId = null;
                    if (epicTypes.Any())
                        foundProjectId = epicTypes.First().ProjectId;
                    else if (taskTypes.Any())
                        foundProjectId = taskTypes.First().ProjectId;
                    else if (subtaskTypes.Any())
                        foundProjectId = subtaskTypes.First().ProjectId;
                    
                    var message = $"{updatedCount} نوع تسک پیش‌فرض با موفقیت به‌روزرسانی شدند.";
                    if (epicTypes.Any() || taskTypes.Any() || subtaskTypes.Any())
                    {
                        message += $" (ویژگی: {epicTypes.Count}, کار: {taskTypes.Count}, کارک: {subtaskTypes.Count})";
                    }
                    
                    TempData["Success"] = message;
                    
                    // استفاده از projectId ارسال شده یا projectId از رکوردهای به‌روزرسانی شده
                    int redirectProjectId = projectId ?? foundProjectId ?? 0;
                    if (redirectProjectId > 0)
                    {
                        return RedirectToAction("IssueTypes", new { projectId = redirectProjectId });
                    }
                    else
                    {
                        return RedirectToAction("Index");
                    }
                }
                else
                {
                    TempData["Info"] = "هیچ نوع تسک پیش‌فرضی با نام‌های قدیمی یافت نشد. همه به‌روزرسانی شده‌اند.";
                    
                    if (projectId.HasValue && projectId.Value > 0)
                    {
                        return RedirectToAction("IssueTypes", new { projectId = projectId.Value });
                    }
                    
                    // پیدا کردن projectId از یک پروژه موجود
                    var anyProject = await _context.Projects.FirstOrDefaultAsync();
                    if (anyProject != null)
                    {
                        return RedirectToAction("IssueTypes", new { projectId = anyProject.Id });
                    }
                    return RedirectToAction("Index");
                }
            }
            catch (Exception ex)
            {
                TempData["Error"] = $"خطا در به‌روزرسانی نام‌ها: {ex.Message}";
                
                if (projectId.HasValue && projectId.Value > 0)
                {
                    return RedirectToAction("IssueTypes", new { projectId = projectId.Value });
                }
                
                // پیدا کردن projectId از یک پروژه موجود
                var anyProject = await _context.Projects.FirstOrDefaultAsync();
                if (anyProject != null)
                {
                    return RedirectToAction("IssueTypes", new { projectId = anyProject.Id });
                }
                return RedirectToAction("Index");
            }
        }

        // DTO برای به‌روزرسانی ترتیب
        public class IssueTypeOrderDto
        {
            public int Id { get; set; }
            public int Order { get; set; }
        }

    }
}
