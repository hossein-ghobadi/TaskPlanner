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
                .Where(s => projectIds.Contains(s.ProjectId) && s.IsActive)
                .Select(s => new { s.ProjectId, s.Id })
                .ToListAsync();

            ViewBag.ActiveSprintByProject = activeSprints
                .GroupBy(x => x.ProjectId)
                .ToDictionary(g => g.Key, g => g.First().Id);

            return View(projects);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            try
            {
                var dto = await _projectQueryService.GetProjectDetailsDtoAsync(id);
                
                var vm = new ProjectDetailsVm
                {
                    Id = dto.Id,
                    Name = dto.Name,
                    Description = dto.Description,
                    CreatorUserName = dto.CreatorUserName,
                    MemberUserNames = dto.MemberUserNames,
                    Tasks = dto.Tasks,
                    Invitations = dto.Invitations.Select(i => new ProjectInvitationVm
                    {
                        InviteePhone = i.InviteePhone,
                        Status = i.Status,
                        CreatedAt = i.CreatedAt
                    }).ToList(),
                    ActiveSprintId = dto.ActiveSprintId,
                    ActiveSprintName = dto.ActiveSprintName
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


    }
}
