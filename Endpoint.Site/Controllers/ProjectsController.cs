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

            // دریافت لیست همکاران (شامل همکاران دعوت سیستم و همکاران دعوت پروژه)
            
            // 1. همکاران دعوت سیستم (ProjectId == null)
            // کسانی که من دعوتشان دادم و قبول کردند
            var systemInvitationsISent = await _context.ProjectInvitations
                .Where(i => i.ProjectId == null && i.Status == InvitationStatus.Accepted && 
                    i.InviterId == userId && !string.IsNullOrEmpty(i.InviteeId))
                .Select(i => i.InviteeId)
                .Distinct()
                .ToListAsync();

            // کسانی که من دعوتشان را قبول کردم
            var systemInvitationsIAccepted = await _context.ProjectInvitations
                .Where(i => i.ProjectId == null && i.Status == InvitationStatus.Accepted && 
                    i.InviteePhone == phone && !string.IsNullOrEmpty(i.InviteeId))
                .Select(i => i.InviterId)
                .Distinct()
                .ToListAsync();

            // همچنین کسانی که من دعوتشان دادم و با شماره تلفن قبول کردند (اگر InviteeId خالی باشد)
            var systemInvitationsByPhone = await _context.ProjectInvitations
                .Where(i => i.ProjectId == null && i.Status == InvitationStatus.Accepted && 
                    i.InviterId == userId && string.IsNullOrEmpty(i.InviteeId) && !string.IsNullOrEmpty(i.InviteePhone))
                .ToListAsync();

            // پیدا کردن کاربران با شماره تلفن (دعوت سیستم)
            var systemPhoneNumbers = systemInvitationsByPhone.Select(i => i.InviteePhone).Distinct().ToList();
            var systemUsersByPhone = await _userManager.Users
                .Where(u => systemPhoneNumbers.Contains(u.Phone))
                .Select(u => u.Id)
                .ToListAsync();

            // 2. همکاران دعوت پروژه (ProjectId != null)
            // کسانی که من دعوتشان دادم به پروژه و قبول کردند
            var projectInvitationsISent = await _context.ProjectInvitations
                .Where(i => i.ProjectId != null && i.Status == InvitationStatus.Accepted && 
                    i.InviterId == userId && !string.IsNullOrEmpty(i.InviteeId))
                .Select(i => i.InviteeId)
                .Distinct()
                .ToListAsync();

            // کسانی که من دعوتشان را در پروژه قبول کردم
            var projectInvitationsIAccepted = await _context.ProjectInvitations
                .Where(i => i.ProjectId != null && i.Status == InvitationStatus.Accepted && 
                    i.InviteePhone == phone && !string.IsNullOrEmpty(i.InviteeId))
                .Select(i => i.InviterId)
                .Distinct()
                .ToListAsync();

            // همچنین کسانی که من دعوتشان دادم به پروژه و با شماره تلفن قبول کردند
            var projectInvitationsByPhone = await _context.ProjectInvitations
                .Where(i => i.ProjectId != null && i.Status == InvitationStatus.Accepted && 
                    i.InviterId == userId && string.IsNullOrEmpty(i.InviteeId) && !string.IsNullOrEmpty(i.InviteePhone))
                .ToListAsync();

            // پیدا کردن کاربران با شماره تلفن (دعوت پروژه)
            var projectPhoneNumbers = projectInvitationsByPhone.Select(i => i.InviteePhone).Distinct().ToList();
            var projectUsersByPhone = await _userManager.Users
                .Where(u => projectPhoneNumbers.Contains(u.Phone))
                .Select(u => u.Id)
                .ToListAsync();

            // 3. اعضای پروژه‌ها (کسانی که عضو پروژه‌های من هستند یا من عضو پروژه‌های آنها هستم)
            // پروژه‌هایی که من سازنده آنها هستم
            var myProjects = await _context.Projects
                .Where(p => p.CreatorUserId == userId)
                .Select(p => p.Id)
                .ToListAsync();

            // اعضای پروژه‌های من
            var membersOfMyProjects = await _context.ProjectMembers
                .Where(pm => myProjects.Contains(pm.ProjectId.Value) && pm.UserId != userId)
                .Select(pm => pm.UserId)
                .Distinct()
                .ToListAsync();

            // پروژه‌هایی که من عضو آنها هستم
            var projectsIMemberOf = await _context.ProjectMembers
                .Where(pm => pm.UserId == userId)
                .Select(pm => pm.ProjectId.Value)
                .ToListAsync();

            // سازندگان و اعضای پروژه‌هایی که من عضو آنها هستم
            var creatorsOfMyProjects = await _context.Projects
                .Where(p => projectsIMemberOf.Contains(p.Id) && p.CreatorUserId != userId)
                .Select(p => p.CreatorUserId)
                .Distinct()
                .ToListAsync();

            var otherMembersOfMyProjects = await _context.ProjectMembers
                .Where(pm => projectsIMemberOf.Contains(pm.ProjectId.Value) && pm.UserId != userId)
                .Select(pm => pm.UserId)
                .Distinct()
                .ToListAsync();

            // ترکیب همه ID های همکاران
            var collaboratorIds = systemInvitationsISent
                .Concat(systemInvitationsIAccepted)
                .Concat(systemUsersByPhone)
                .Concat(projectInvitationsISent)
                .Concat(projectInvitationsIAccepted)
                .Concat(projectUsersByPhone)
                .Concat(membersOfMyProjects)
                .Concat(creatorsOfMyProjects)
                .Concat(otherMembersOfMyProjects)
                .Distinct()
                .Where(id => id != userId && !string.IsNullOrEmpty(id))
                .ToList();

            // دریافت اطلاعات همکاران
            var collaborators = await _userManager.Users
                .Where(u => collaboratorIds.Contains(u.Id))
                .Select(u => new Dictionary<string, object>
                {
                    { "Id", u.Id },
                    { "DisplayName", !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : (u.UserName ?? "کاربر ناشناس") },
                    { "Phone", u.Phone ?? "" },
                    { "Email", u.Email ?? "" }
                })
                .ToListAsync();

            ViewBag.Collaborators = collaborators;

            // دریافت یادداشت‌های شخصی اخیر
            var recentNotes = await _context.PersonalNotes
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.CreatedAt)
                .Take(5)
                .ToListAsync();
            recentNotes.ForEach(n =>
            {
                if (n.Content?.Length > 100)
                    n.Content = n.Content.Substring(0, 100);
            });
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
                
                // دریافت لیست همکاران برای دعوت به پروژه (شامل همکاران دعوت سیستم و همکاران دعوت پروژه)
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var currentUser = await _userManager.FindByIdAsync(currentUserId);
                var phone = currentUser?.Phone;
                
                // 1. همکاران دعوت سیستم (ProjectId == null)
                // کسانی که من دعوتشان دادم و قبول کردند
                var systemInvitationsISent = await _context.ProjectInvitations
                    .Where(i => i.ProjectId == null && i.Status == InvitationStatus.Accepted && 
                        i.InviterId == currentUserId && !string.IsNullOrEmpty(i.InviteeId))
                    .Select(i => i.InviteeId)
                    .Distinct()
                    .ToListAsync();

                // کسانی که من دعوتشان را قبول کردم
                var systemInvitationsIAccepted = await _context.ProjectInvitations
                    .Where(i => i.ProjectId == null && i.Status == InvitationStatus.Accepted && 
                        i.InviteePhone == phone && !string.IsNullOrEmpty(i.InviteeId))
                    .Select(i => i.InviterId)
                    .Distinct()
                    .ToListAsync();

                // همچنین کسانی که من دعوتشان دادم و با شماره تلفن قبول کردند (اگر InviteeId خالی باشد)
                var systemInvitationsByPhone = await _context.ProjectInvitations
                    .Where(i => i.ProjectId == null && i.Status == InvitationStatus.Accepted && 
                        i.InviterId == currentUserId && string.IsNullOrEmpty(i.InviteeId) && !string.IsNullOrEmpty(i.InviteePhone))
                    .ToListAsync();

                // پیدا کردن کاربران با شماره تلفن (دعوت سیستم)
                var systemPhoneNumbers = systemInvitationsByPhone.Select(i => i.InviteePhone).Distinct().ToList();
                var systemUsersByPhone = await _userManager.Users
                    .Where(u => systemPhoneNumbers.Contains(u.Phone))
                    .Select(u => u.Id)
                    .ToListAsync();

                // 2. همکاران دعوت پروژه (ProjectId != null)
                // کسانی که من دعوتشان دادم به پروژه و قبول کردند
                var projectInvitationsISent = await _context.ProjectInvitations
                    .Where(i => i.ProjectId != null && i.Status == InvitationStatus.Accepted && 
                        i.InviterId == currentUserId && !string.IsNullOrEmpty(i.InviteeId))
                    .Select(i => i.InviteeId)
                    .Distinct()
                    .ToListAsync();

                // کسانی که من دعوتشان را در پروژه قبول کردم
                var projectInvitationsIAccepted = await _context.ProjectInvitations
                    .Where(i => i.ProjectId != null && i.Status == InvitationStatus.Accepted && 
                        i.InviteePhone == phone && !string.IsNullOrEmpty(i.InviteeId))
                    .Select(i => i.InviterId)
                    .Distinct()
                    .ToListAsync();

                // همچنین کسانی که من دعوتشان دادم به پروژه و با شماره تلفن قبول کردند
                var projectInvitationsByPhone = await _context.ProjectInvitations
                    .Where(i => i.ProjectId != null && i.Status == InvitationStatus.Accepted && 
                        i.InviterId == currentUserId && string.IsNullOrEmpty(i.InviteeId) && !string.IsNullOrEmpty(i.InviteePhone))
                    .ToListAsync();

                // پیدا کردن کاربران با شماره تلفن (دعوت پروژه)
                var projectPhoneNumbers = projectInvitationsByPhone.Select(i => i.InviteePhone).Distinct().ToList();
                var projectUsersByPhone = await _userManager.Users
                    .Where(u => projectPhoneNumbers.Contains(u.Phone))
                    .Select(u => u.Id)
                    .ToListAsync();

                // 3. اعضای پروژه‌ها (کسانی که عضو پروژه‌های من هستند یا من عضو پروژه‌های آنها هستم)
                // پروژه‌هایی که من سازنده آنها هستم
                var myProjects = await _context.Projects
                    .Where(p => p.CreatorUserId == currentUserId)
                    .Select(p => p.Id)
                    .ToListAsync();

                // اعضای پروژه‌های من
                var membersOfMyProjects = await _context.ProjectMembers
                    .Where(pm => myProjects.Contains(pm.ProjectId.Value) && pm.UserId != currentUserId)
                    .Select(pm => pm.UserId)
                    .Distinct()
                    .ToListAsync();

                // پروژه‌هایی که من عضو آنها هستم
                var projectsIMemberOf = await _context.ProjectMembers
                    .Where(pm => pm.UserId == currentUserId)
                    .Select(pm => pm.ProjectId.Value)
                    .ToListAsync();

                // سازندگان و اعضای پروژه‌هایی که من عضو آنها هستم
                var creatorsOfMyProjects = await _context.Projects
                    .Where(p => projectsIMemberOf.Contains(p.Id) && p.CreatorUserId != currentUserId)
                    .Select(p => p.CreatorUserId)
                    .Distinct()
                    .ToListAsync();

                var otherMembersOfMyProjects = await _context.ProjectMembers
                    .Where(pm => projectsIMemberOf.Contains(pm.ProjectId.Value) && pm.UserId != currentUserId)
                    .Select(pm => pm.UserId)
                    .Distinct()
                    .ToListAsync();

                // ترکیب همه ID های همکاران
                var collaboratorIds = systemInvitationsISent
                    .Concat(systemInvitationsIAccepted)
                    .Concat(systemUsersByPhone)
                    .Concat(projectInvitationsISent)
                    .Concat(projectInvitationsIAccepted)
                    .Concat(projectUsersByPhone)
                    .Concat(membersOfMyProjects)
                    .Concat(creatorsOfMyProjects)
                    .Concat(otherMembersOfMyProjects)
                    .Distinct()
                    .Where(id => id != currentUserId && !string.IsNullOrEmpty(id))
                    .ToList();

                // دریافت اطلاعات همکاران
                var collaborators = await _userManager.Users
                    .Where(u => collaboratorIds.Contains(u.Id))
                    .Select(u => new Dictionary<string, object>
                    {
                        { "Id", u.Id },
                        { "DisplayName", !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : (u.UserName ?? "کاربر ناشناس") },
                        { "Phone", u.Phone ?? "" },
                        { "Email", u.Email ?? "" }
                    })
                    .ToListAsync();

                // فیلتر کردن همکارانی که قبلاً عضو پروژه هستند یا دعوت در انتظار دارند
                var existingMemberIds = vm.Members.Select(m => m.UserId).ToList();
                var pendingInviteePhones = vm.Invitations.Where(i => i.Status == InvitationStatus.Pending)
                    .Select(i => i.InviteePhone)
                    .ToList();
                
                var availableCollaborators = collaborators
                    .Where(c => !existingMemberIds.Contains(c["Id"].ToString()) && 
                                !pendingInviteePhones.Contains(c["Phone"].ToString()))
                    .ToList();

                ViewBag.Collaborators = availableCollaborators;
                
                // تعداد یادداشت‌های پروژه
                var notesCount = await _context.ProjectNotes
                    .Where(n => n.ProjectId == id)
                    .CountAsync();
                ViewBag.NotesCount = notesCount;
                
                // تعداد عکس‌های گالری پروژه
                var imagesCount = await _context.ProjectImageGalleries
                    .Where(img => img.ProjectId == id)
                    .CountAsync();
                ViewBag.ImagesCount = imagesCount;

                ViewBag.ProjectId = id;
                ViewBag.SidebarTotalTasks = totalTasks;
                ViewBag.SidebarPendingInvites = vm.Invitations.Count(i => i.Status == InvitationStatus.Pending);
                var sidebarMemberCount = vm.Members?.Count ?? 0;
                if (vm.Members == null || !vm.Members.Any(m => m.UserId == dto.CreatorUserId))
                {
                    sidebarMemberCount++;
                }
                ViewBag.SidebarMemberCount = sidebarMemberCount;

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

                ViewBag.ProjectId = id;

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

        /// <summary>
        /// دعوت همکار به پروژه (از طریق ID همکار)
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteCollaboratorToProject(int projectId, string collaboratorId)
        {
            var inviterId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(collaboratorId))
            {
                TempData["InviteError"] = "شناسه همکار نامعتبر است.";
                return RedirectToAction("Details", new { id = projectId });
            }

            // دریافت اطلاعات همکار
            var collaborator = await _userManager.FindByIdAsync(collaboratorId);
            if (collaborator == null || string.IsNullOrWhiteSpace(collaborator.Phone))
            {
                TempData["InviteError"] = "همکار مورد نظر یافت نشد یا شماره تلفن ندارد.";
                return RedirectToAction("Details", new { id = projectId });
            }

            try
            {
                // استفاده از متد موجود برای دعوت
                await _projectCommandService.InviteUserToProjectAsync(projectId, collaborator.Phone, inviterId);
                TempData["InviteSuccess"] = $"دعوت برای {collaborator.FullName ?? collaborator.UserName ?? collaborator.Phone} ارسال شد.";
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

        // 📊 گزارش آماری پروژه‌ها
        [HttpGet]
        public async Task<IActionResult> Statistics()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // دریافت پروژه‌های کاربر
            var projects = await _context.Projects
                .Where(p => p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId))
                .Select(p => new { p.Id, p.Name })
                .ToListAsync();

            var now = DateTime.UtcNow;
            var statistics = new List<ProjectStatisticItem>();

            foreach (var project in projects)
            {
                var tasks = await _context.TaskItems
                    .Where(t => t.ProjectId == project.Id)
                    .ToListAsync();

                var totalTasks = tasks.Count;
                var completedTasks = tasks.Count(t => t.IsCompleted);
                var overdueTasks = tasks.Count(t => 
                    !t.IsCompleted && 
                    t.DueDate.HasValue && 
                    t.DueDate.Value < now);
                var remainingTasks = totalTasks - completedTasks;

                // محاسبه امتیاز سلامت پروژه (ساده و کاربردی)
                int successProbability = 0;
                string successStatus = "نامشخص";
                
                if (totalTasks == 0)
                {
                    successProbability = 100;
                    successStatus = "بدون تسک";
                }
                else
                {
                    // درصد انجام و درصد تاخیر نسبت به کل تسک‌ها
                    var completionRate = (double)completedTasks / totalTasks; // 0..1
                    var overdueRate = (double)overdueTasks / totalTasks;       // 0..1

                    // منطق ساده:
                    // امتیاز پایه = درصد انجام * 100
                    // جریمه تاخیر = درصد تاخیر * 50
                    // خروجی نهایی در بازه 0..100
                    var rawScore = (completionRate * 100.0) - (overdueRate * 50.0);
                    successProbability = (int)Math.Round(rawScore);
                    successProbability = Math.Max(0, Math.Min(100, successProbability));

                    // وضعیت قابل فهم و عملی
                    if (completedTasks == totalTasks)
                    {
                        successStatus = "تکمیل شده";
                    }
                    else if (successProbability >= 85)
                    {
                        successStatus = "عالی";
                    }
                    else if (successProbability >= 70)
                    {
                        successStatus = "خوب";
                    }
                    else if (successProbability >= 50)
                    {
                        successStatus = "نیازمند توجه";
                    }
                    else
                    {
                        successStatus = "بحرانی";
                    }
                }

                statistics.Add(new ProjectStatisticItem
                {
                    ProjectId = project.Id,
                    ProjectName = project.Name,
                    TotalTasks = totalTasks,
                    CompletedTasks = completedTasks,
                    OverdueTasks = overdueTasks,
                    RemainingTasks = remainingTasks,
                    SuccessProbability = successProbability,
                    SuccessStatus = successStatus
                });
            }

            // دریافت کاربران مرتبط با پروژه‌ها
            var projectIds = projects.Select(p => p.Id).ToList();
            var assignedUserIds = await _context.TaskItems
                .Where(t => projectIds.Contains(t.ProjectId) && t.AssignedUserId != null)
                .Select(t => t.AssignedUserId)
                .Distinct()
                .ToListAsync();

            var users = await _userManager.Users
                .Where(u => assignedUserIds.Contains(u.Id))
                .Select(u => new UserOption
                {
                    Id = u.Id,
                    Name = !string.IsNullOrWhiteSpace(u.FullName) ? u.FullName : (u.UserName ?? "کاربر ناشناس")
                })
                .ToListAsync();

            var vm = new ProjectStatisticsVm
            {
                Projects = statistics,
                ProjectOptions = projects.Select(p => new ProjectOption
                {
                    Id = p.Id,
                    Name = p.Name
                }).ToList(),
                UserOptions = users
            };

            return View(vm);
        }

        // 📈 API برای دریافت داده‌های نمودار زمانی
        [HttpGet]
        public async Task<IActionResult> GetTaskTimeSeriesData(int? projectId = null, string? assignedUserId = null, string? startDate = null, string? endDate = null, string dateType = "CreatedAt")
        {
            System.Diagnostics.Debug.WriteLine($"=== GetTaskTimeSeriesData called ===");
            System.Diagnostics.Debug.WriteLine($"Parameters: startDate={startDate}, endDate={endDate}, dateType={dateType}, projectId={projectId}, assignedUserId={assignedUserId}");
            
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            
            // دریافت پروژه‌های کاربر
            var userProjectIds = await _context.Projects
                .Where(p => p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId))
                .Select(p => p.Id)
                .ToListAsync();

            DateTime startDateValue;
            DateTime endDateValue;

            // تابع کمکی برای تبدیل اعداد فارسی به انگلیسی
            string ConvertPersianToEnglishNumbers(string input)
            {
                if (string.IsNullOrEmpty(input)) return input;
                
                var persianDigits = new[] { '۰', '۱', '۲', '۳', '۴', '۵', '۶', '۷', '۸', '۹' };
                var englishDigits = new[] { '0', '1', '2', '3', '4', '5', '6', '7', '8', '9' };
                
                for (int i = 0; i < persianDigits.Length; i++)
                {
                    input = input.Replace(persianDigits[i], englishDigits[i]);
                }
                return input;
            }
            
            // تبدیل تاریخ شمسی به میلادی
            if (!string.IsNullOrEmpty(startDate))
            {
                try
                {
                    // تبدیل اعداد فارسی به انگلیسی
                    var normalizedStartDate = ConvertPersianToEnglishNumbers(startDate);
                    System.Diagnostics.Debug.WriteLine($"Parsing startDate: '{startDate}' -> '{normalizedStartDate}'");
                    
                    var pc = new System.Globalization.PersianCalendar();
                    var parts = normalizedStartDate.Split('/');
                    System.Diagnostics.Debug.WriteLine($"StartDate parts: [{string.Join(", ", parts)}]");
                    if (parts.Length == 3)
                    {
                        var year = int.Parse(parts[0]);
                        var month = int.Parse(parts[1]);
                        var day = int.Parse(parts[2]);
                        System.Diagnostics.Debug.WriteLine($"Parsed: Year={year}, Month={month}, Day={day}");
                        // تبدیل مستقیم به DateTime بدون timezone
                        var gregorianDate = pc.ToDateTime(year, month, day, 0, 0, 0, 0);
                        System.Diagnostics.Debug.WriteLine($"Gregorian date: {gregorianDate:yyyy-MM-dd}");
                        // تبدیل به UTC بدون تغییر تاریخ
                        startDateValue = new DateTime(gregorianDate.Year, gregorianDate.Month, gregorianDate.Day, 0, 0, 0, DateTimeKind.Utc);
                        System.Diagnostics.Debug.WriteLine($"Final startDateValue: {startDateValue:yyyy-MM-dd}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Invalid startDate format, using default");
                        startDateValue = DateTime.UtcNow.AddDays(-30).Date;
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing startDate: {startDate}, Error: {ex.Message}");
                    startDateValue = DateTime.UtcNow.AddDays(-30).Date;
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"startDate is empty, using default");
                startDateValue = DateTime.UtcNow.AddDays(-30).Date;
            }

            if (!string.IsNullOrEmpty(endDate))
            {
                try
                {
                    // تبدیل اعداد فارسی به انگلیسی
                    var normalizedEndDate = ConvertPersianToEnglishNumbers(endDate);
                    System.Diagnostics.Debug.WriteLine($"Parsing endDate: '{endDate}' -> '{normalizedEndDate}'");
                    
                    var pc = new System.Globalization.PersianCalendar();
                    var parts = normalizedEndDate.Split('/');
                    System.Diagnostics.Debug.WriteLine($"EndDate parts: [{string.Join(", ", parts)}]");
                    if (parts.Length == 3)
                    {
                        var year = int.Parse(parts[0]);
                        var month = int.Parse(parts[1]);
                        var day = int.Parse(parts[2]);
                        System.Diagnostics.Debug.WriteLine($"Parsed: Year={year}, Month={month}, Day={day}");
                        // تبدیل مستقیم به DateTime بدون timezone
                        var gregorianDate = pc.ToDateTime(year, month, day, 23, 59, 59, 999);
                        System.Diagnostics.Debug.WriteLine($"Gregorian date: {gregorianDate:yyyy-MM-dd}");
                        // تبدیل به UTC بدون تغییر تاریخ - استفاده از آخرین لحظه همان روز
                        endDateValue = new DateTime(gregorianDate.Year, gregorianDate.Month, gregorianDate.Day, 23, 59, 59, 999, DateTimeKind.Utc);
                        System.Diagnostics.Debug.WriteLine($"Final endDateValue: {endDateValue:yyyy-MM-dd}");
                    }
                    else
                    {
                        System.Diagnostics.Debug.WriteLine($"Invalid endDate format, using default");
                        endDateValue = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
                    }
                }
                catch (Exception ex)
                {
                    System.Diagnostics.Debug.WriteLine($"Error parsing endDate: {endDate}, Error: {ex.Message}");
                    endDateValue = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
                }
            }
            else
            {
                System.Diagnostics.Debug.WriteLine($"endDate is empty, using default");
                endDateValue = DateTime.UtcNow.Date.AddDays(1).AddTicks(-1);
            }
            
            System.Diagnostics.Debug.WriteLine($"Date Range: {startDateValue:yyyy-MM-dd HH:mm:ss} to {endDateValue:yyyy-MM-dd HH:mm:ss}");
            
            // فیلتر بر اساس پروژه و تاریخ (بر اساس معیار انتخاب شده)
            IQueryable<TaskItem> tasksQuery = _context.TaskItems
                .Where(t => userProjectIds.Contains(t.ProjectId));

            // فیلتر بر اساس معیار زمانی انتخاب شده
            if (dateType == "StartDate")
            {
                tasksQuery = tasksQuery.Where(t => t.StartDate >= startDateValue && t.StartDate <= endDateValue);
            }
            else if (dateType == "DueDate")
            {
                tasksQuery = tasksQuery.Where(t => t.DueDate.HasValue && 
                                                   t.DueDate.Value >= startDateValue && 
                                                   t.DueDate.Value <= endDateValue);
            }
            else // CreatedAt (پیش‌فرض)
            {
                tasksQuery = tasksQuery.Where(t => t.CreatedAt >= startDateValue && t.CreatedAt <= endDateValue);
            }

            if (projectId.HasValue && userProjectIds.Contains(projectId.Value))
            {
                tasksQuery = tasksQuery.Where(t => t.ProjectId == projectId.Value);
            }

            // فیلتر بر اساس کاربر
            if (!string.IsNullOrEmpty(assignedUserId))
            {
                tasksQuery = tasksQuery.Where(t => t.AssignedUserId == assignedUserId);
            }

            // انتخاب فیلد تاریخ بر اساس معیار
            var tasks = dateType switch
            {
                "StartDate" => await tasksQuery
                    .Select(t => new { t.ProjectId, Date = t.StartDate })
                    .ToListAsync(),
                "DueDate" => await tasksQuery
                    .Where(t => t.DueDate.HasValue)
                    .Select(t => new { t.ProjectId, Date = t.DueDate!.Value })
                    .ToListAsync(),
                _ => await tasksQuery
                    .Select(t => new { t.ProjectId, Date = t.CreatedAt })
                    .ToListAsync()
            };

            // گروه‌بندی بر اساس تاریخ و پروژه
            var timeSeriesDataByProject = tasks
                .GroupBy(t => new { Date = t.Date.Date, ProjectId = t.ProjectId })
                .Select(g => new
                {
                    Date = g.Key.Date,
                    ProjectId = g.Key.ProjectId,
                    Count = g.Count()
                })
                .OrderBy(x => x.Date)
                .ToList();

            // ایجاد لیست کامل تاریخ‌ها
            var allDates = new List<DateTime>();
            var currentDate = startDateValue.Date;
            var endDateOnly = endDateValue.Date;
            
            while (currentDate <= endDateOnly)
            {
                allDates.Add(currentDate);
                currentDate = currentDate.AddDays(1);
            }
            
            System.Diagnostics.Debug.WriteLine($"Total dates generated: {allDates.Count}, First: {allDates.FirstOrDefault():yyyy-MM-dd}, Last: {allDates.LastOrDefault():yyyy-MM-dd}");

            // ساخت داده‌های نمودار
            var result = new List<TaskTimeSeriesData>();
            var projectIds = projectId.HasValue 
                ? new List<int> { projectId.Value }
                : userProjectIds;

            foreach (var date in allDates)
            {
                var dateData = new TaskTimeSeriesData
                {
                    Date = date.ToString("yyyy-MM-dd"),
                    ProjectTaskCounts = new Dictionary<int, int>()
                };

                // محاسبه تعداد کارها بر اساس پروژه
                foreach (var pid in projectIds)
                {
                    var count = timeSeriesDataByProject
                        .FirstOrDefault(t => t.Date.Date == date.Date && t.ProjectId == pid)?.Count ?? 0;
                    dateData.ProjectTaskCounts[pid] = count;
                }

                result.Add(dateData);
            }

            // دریافت نام پروژه‌ها
            var projectNames = await _context.Projects
                .Where(p => projectIds.Contains(p.Id))
                .ToDictionaryAsync(p => p.Id, p => p.Name);

            // دریافت نام کاربر انتخاب شده (اگر انتخاب شده باشد)
            string? selectedUserName = null;
            if (!string.IsNullOrEmpty(assignedUserId))
            {
                var selectedUser = await _userManager.FindByIdAsync(assignedUserId);
                if (selectedUser != null)
                {
                    selectedUserName = !string.IsNullOrWhiteSpace(selectedUser.FullName) 
                        ? selectedUser.FullName 
                        : (selectedUser.UserName ?? "کاربر ناشناس");
                }
            }

            var responseDates = result.Select(r => r.Date).ToList();
            System.Diagnostics.Debug.WriteLine($"Response dates count: {responseDates.Count}");
            if (responseDates.Any())
            {
                System.Diagnostics.Debug.WriteLine($"First date: {responseDates.First():yyyy-MM-dd}, Last date: {responseDates.Last():yyyy-MM-dd}");
            }
            
            return Json(new
            {
                dates = responseDates,
                projects = projectIds.Select(id => new
                {
                    id = id,
                    name = projectNames.ContainsKey(id) ? projectNames[id] : $"پروژه {id}"
                }).ToList(),
                selectedUserId = assignedUserId,
                selectedUserName = selectedUserName,
                data = result.Select(r => new
                {
                    date = r.Date,
                    counts = projectIds.ToDictionary(
                        pid => pid,
                        pid => r.ProjectTaskCounts.ContainsKey(pid) ? r.ProjectTaskCounts[pid] : 0
                    )
                }).ToList()
            });
        }

    }
}
