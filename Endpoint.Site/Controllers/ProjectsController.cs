using DocumentFormat.OpenXml.Spreadsheet;
using Endpoint.Site.Areas.TaskPlanner.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Domain.Entities.Users;
using TaskPlanner.Persistence.Contexts;
using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Areas.TaskPlanner.Controllers
{
    [Authorize]
    [Area("TaskPlanner")]
    [Route("TaskPlanner/[controller]/[action]")]
    public class ProjectsController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;

        public ProjectsController(MVPTestDatabaseContext context,UserManager<User> userManager)
        {
            _userManager = userManager;

            _context = context;
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

            // تمام پروژه‌هایی که کاربر ایجاد کرده یا عضو آن است
            var projects = await _context.Projects
                .Include(p => p.Tasks)
                .Include(p => p.Members)
                .Where(p =>
                    p.CreatorUserId == userId ||            // خودش سازنده پروژه باشد
                    p.Members.Any(m => m.UserId == userId)  // یا در لیست اعضا باشد
                )
                .ToListAsync();

            return View(projects);
        }
        [HttpGet("{id}")]
        public async Task<IActionResult> Details(int id)
        {
            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            var creator = await _userManager.FindByIdAsync(project.CreatorUserId);

            var vm = new ProjectDetailsVm
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                CreatorUserName = creator != null ? creator.FullName : "ناشناس",
                Tasks = new List<TaskItem>()
            };

            // 👥 اعضای پروژه
            var memberUsernames = new List<string>();
            foreach (var member in project.Members)
            {
                var userInfo = await _userManager.FindByIdAsync(member.UserId);
                if (userInfo != null)
                    memberUsernames.Add($"{userInfo.FullName ?? "بدون نام"} ({userInfo.PhoneNumber})");
            }
            vm.MemberUserNames = memberUsernames;

            // 📋 تسک‌های نمایش‌پذیر در بخش پروژه (بدون Epic)
            vm.Tasks = await _context.TaskItems
                .Where(t => t.ProjectId == id
                    && t.ParentTaskId == null
                    && (t.IssueType == IssueType.Story || t.IssueType == IssueType.Task || t.IssueType == IssueType.Bug))
                .Include(t => t.Category)
                .Include(t => t.AssignedUser)
                .Include(t => t.ChildIssues.Where(c => c.IssueType == IssueType.Subtask))
                    .ThenInclude(c => c.AssignedUser)
                .ToListAsync();

            // 🕓 دعوت‌های در انتظار (Pending)
            var pendingInvitations = await _context.ProjectInvitations
                .Where(i => i.ProjectId == id && i.Status == InvitationStatus.Pending)
                .Select(i => new ProjectInvitationVm
                {
                    InviteePhone = i.InviteePhone,
                    Status = i.Status,
                    CreatedAt = i.CreatedAt
                })
                .ToListAsync();

            vm.Invitations = pendingInvitations;

            // 🎯 اسپرینت فعال
            var activeSprint = await _context.Sprints
                .Where(s => s.ProjectId == id && s.IsActive)
                .Select(s => new { s.Id, s.Name })
                .FirstOrDefaultAsync();

            if (activeSprint != null)
            {
                vm.ActiveSprintId = activeSprint.Id;
                vm.ActiveSprintName = activeSprint.Name;
            }

            return View(vm);
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

            // ✅ فقط کاربران تاییدشده (Accepted) از جدول دعوت‌ها
            var acceptedInvitePhones = await _context.ProjectInvitations
                .Where(i => i.InviterId == userId && i.Status == InvitationStatus.Accepted)
                .Select(i => i.InviteeId)
                .ToListAsync();
            Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>   {acceptedInvitePhones.Count}");
            // ✅ تطبیق با کاربران سیستم بر اساس شماره موبایل
            var acceptedUsers = _userManager.Users
                .Where(u => acceptedInvitePhones.Contains(u.Id))
                .Select(u => new
                {
                    Id = u.Id,
                    DisplayName = !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.UserName
                })
                .ToList();
            Console.WriteLine($">>>>>>>>>>>>>>>>>>>>>   {acceptedUsers.Count}");

            // 👇 کاربران تاییدشده برای افزودن به پروژه
            ViewBag.Users = acceptedUsers;

            // 👇 دعوت‌های در انتظار (هنوز تایید نشده)
            var pendingInvites = await _context.ProjectInvitations
                .Where(i => i.InviterId == userId && i.Status == InvitationStatus.Pending)
                .Select(i => i.InviteePhone)
                .ToListAsync();

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
                ViewBag.Users = _userManager.Users.ToList();
                return View(vm);
            }

            // 🟢 گرفتن کاربر لاگین‌شده
            var currentUser = await _userManager.GetUserAsync(User);
            if (currentUser == null)
            {
                TempData["Error"] = "کاربر لاگین‌شده یافت نشد.";
                return RedirectToAction(nameof(Index));
            }

            var userId = currentUser.Id;

            // 🟢 ایجاد پروژه جدید
            var project = new Project
            {
                Name = vm.Name,
                Description = vm.Description,
                CreatorUserId = userId
            };

            _context.Projects.Add(project);
            await _context.SaveChangesAsync();
            
            // 🧩 Seed کردن Workflow پیش‌فرض برای پروژه‌های جدید (اگر قبلاً وجود ندارد)
            var hasAnyStatuses = await _context.WorkflowStatuses.AnyAsync(ws => ws.ProjectId == project.Id);
            if (!hasAnyStatuses)
            {
                // 1) ایجاد وضعیت‌های پیش‌فرض
                var wf = WorkflowHelper.CreateDefaultWorkflow(project.Id);
                _context.WorkflowStatuses.AddRange(wf.statuses);
                await _context.SaveChangesAsync();

                // 2) پیدا کردن ID وضعیت‌ها برای ساخت Transition ها
                var statuses = await _context.WorkflowStatuses
                    .Where(ws => ws.ProjectId == project.Id)
                    .OrderBy(ws => ws.Order)
                    .ToListAsync();

                var todoId = statuses.FirstOrDefault(s => s.Type == WorkflowType.Todo)?.Id;
                var inProgressId = statuses.FirstOrDefault(s => s.Type == WorkflowType.InProgress)?.Id;
                var doneId = statuses.FirstOrDefault(s => s.Type == WorkflowType.Done)?.Id;
                var blockedId = statuses.FirstOrDefault(s => s.Type == WorkflowType.Blocked)?.Id;

                if (todoId.HasValue && inProgressId.HasValue && doneId.HasValue && blockedId.HasValue)
                {
                    var transitions = WorkflowHelper.CreateDefaultTransitions(
                        project.Id, todoId.Value, inProgressId.Value, doneId.Value, blockedId.Value);

                    // جلوگیری از تکرار (در صورت ایجاد دستی)
                    var existingKeys = await _context.WorkflowTransitions
                        .Where(t => t.ProjectId == project.Id)
                        .Select(t => new { t.FromStatusId, t.ToStatusId })
                        .ToListAsync();

                    var toAdd = transitions.Where(t => !existingKeys
                        .Any(k => k.FromStatusId == t.FromStatusId && k.ToStatusId == t.ToStatusId))
                        .ToList();

                    if (toAdd.Any())
                    {
                        _context.WorkflowTransitions.AddRange(toAdd);
                        await _context.SaveChangesAsync();
                    }
                }

                TempData["Success"] = (TempData["Success"]?.ToString() + " \n" +
                    "Workflow پیش‌فرض (وضعیت‌ها و ترنزیشن‌ها) برای پروژه ایجاد شد ✅").Trim();
            }

            // 🟢 اضافه کردن اعضای انتخاب‌شده (کاربران موجود در سیستم)
            if (vm.SelectedUserIds != null && vm.SelectedUserIds.Any())
            {
                foreach (var memberId in vm.SelectedUserIds)
                {
                    _context.ProjectMembers.Add(new ProjectMember
                    {
                        ProjectId = project.Id,
                        UserId = memberId
                    });
                }
            }

            // 🟢 اتصال دعوت‌های در انتظار به این پروژه
            var pendingInvitations = await _context.ProjectInvitations
                .Where(i => i.InviterId == userId && i.ProjectId == null && i.Status == InvitationStatus.Pending)
                .ToListAsync();

            if (pendingInvitations.Any())
            {
                foreach (var invite in pendingInvitations)
                {
                    invite.ProjectId = project.Id;
                }
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = $"پروژه «{project.Name}» با موفقیت ایجاد شد و {pendingInvitations.Count} دعوت در انتظار به آن متصل گردید.";

            return RedirectToAction(nameof(Index));
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

            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null)
                return NotFound();

            var userId = currentUser.Id;

            // 👥 اعضای پروژه فعلی
            var projectMembers = project.Members.Select(m => m.UserId).ToList();

            // 📨 تمام دعوت‌هایی که من فرستاده‌ام (در کل سیستم)
            var allMyInvites = await _context.ProjectInvitations
                .Where(i => i.InviterId == userId)
                .ToListAsync();

            // 📨 دعوت‌های همین پروژه
            var projectInvites = allMyInvites
                .Where(i => i.ProjectId == id)
                .ToList();

            // 📨 دعوت‌های عمومی (قبلاً دعوت‌شده ولی هنوز در پروژه خاصی نیستند)
            var globalInvites = allMyInvites
                .Where(i => i.ProjectId == null)
                .ToList();

            // 📱 شماره موبایل تمام افرادی که تاکنون دعوت‌شده‌اند
            var invitedPhones = allMyInvites.Select(i => i.InviteePhone).Distinct().ToList();

            // 📋 کاربران سیستم
            var allUsers = await _userManager.Users
                .Where(u => invitedPhones.Contains(u.PhoneNumber) || projectMembers.Contains(u.Id))
                .Select(u => new
                {
                    u.Id,
                    u.PhoneNumber,
                    DisplayName = !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.UserName
                })
                .ToListAsync();

            var selectableUsers = new List<dynamic>();
            var addedKeys = new HashSet<string>();

            // 👤 اعضای فعلی پروژه
            foreach (var member in allUsers.Where(u => projectMembers.Contains(u.Id)))
            {
                selectableUsers.Add(new
                {
                    Id = member.Id,
                    DisplayName = $"👤 {member.DisplayName} (عضو پروژه)"
                });
                addedKeys.Add(member.Id);
            }

            // 📨 دعوت‌شده‌های همین پروژه
            foreach (var invite in projectInvites)
            {
                var user = allUsers.FirstOrDefault(u => u.PhoneNumber == invite.InviteePhone);
                string statusText = invite.Status switch
                {
                    InvitationStatus.Pending => "🕓 در انتظار پذیرش",
                    InvitationStatus.Accepted => "✅ پذیرفته‌شده",
                    InvitationStatus.Rejected => "❌ رد شده",
                    _ => "نامشخص"
                };

                string uniqueKey = user?.Id ?? $"ghost-{invite.InviteePhone}";
                if (addedKeys.Contains(uniqueKey)) continue;

                selectableUsers.Add(new
                {
                    Id = uniqueKey,
                    DisplayName = user != null
                        ? $"📱 {user.DisplayName} ({statusText})"
                        : $"📞 {invite.InviteePhone} ({statusText})"
                });
                addedKeys.Add(uniqueKey);
            }

            // ➕ کاربرانی که قبلاً توسط من دعوت‌شده‌اند ولی هنوز در این پروژه دعوت نشده‌اند
            foreach (var invite in globalInvites)
            {
                var user = allUsers.FirstOrDefault(u => u.PhoneNumber == invite.InviteePhone);
                if (user == null || addedKeys.Contains(user.Id)) continue;

                selectableUsers.Add(new
                {
                    Id = user.Id,
                    DisplayName = $"➕ {user.DisplayName} (قابل دعوت به این پروژه)"
                });
                addedKeys.Add(user.Id);
            }

            ViewBag.Users = selectableUsers;

            var vm = new ProjectEditVm
            {
                Id = project.Id,
                Name = project.Name,
                Description = project.Description,
                SelectedUserIds = project.Members.Select(m => m.UserId).ToList()
            };

            return View(vm);
        }



        [HttpPost("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProjectEditVm vm)
        {
            if (!ModelState.IsValid)
            {
                var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

                var acceptedInvitePhones = await _context.ProjectInvitations
                    .Where(i => i.InviterId == currentUserId && i.Status == InvitationStatus.Accepted)
                    .Select(i => i.InviteePhone)
                    .ToListAsync();

                var allowedUsers = _userManager.Users
                    .Where(u => acceptedInvitePhones.Contains(u.PhoneNumber))
                    .Select(u => new
                    {
                        Id = u.Id,
                        DisplayName = !string.IsNullOrEmpty(u.FullName) ? u.FullName : u.UserName
                    })
                    .ToList();

                ViewBag.Users = allowedUsers;
                return View(vm);
            }

            var project = await _context.Projects
                .Include(p => p.Members)
                .FirstOrDefaultAsync(p => p.Id == vm.Id);

            if (project == null)
                return NotFound();

            // 🔄 بروزرسانی اطلاعات پروژه
            project.Name = vm.Name;
            project.Description = vm.Description;

            var currentMembers = project.Members.Select(m => m.UserId).ToList();
            var newMembers = vm.SelectedUserIds ?? new List<string>();

            // ✅ افزودن اعضای جدید
            var toAdd = newMembers.Except(currentMembers).ToList();
            foreach (var userId in toAdd)
            {
                _context.ProjectMembers.Add(new ProjectMember
                {
                    ProjectId = project.Id,
                    UserId = userId
                });
            }

            // ✅ حذف اعضای حذف‌شده
            var toRemove = currentMembers.Except(newMembers).ToList();
            foreach (var userId in toRemove)
            {
                var member = project.Members.FirstOrDefault(m => m.UserId == userId);
                if (member != null)
                    _context.ProjectMembers.Remove(member);
            }

            await _context.SaveChangesAsync();
            TempData["Success"] = "تغییرات پروژه با موفقیت ذخیره شد ✅";

            return RedirectToAction(nameof(Index));
        }



        // 📌 حذف پروژه
        [HttpGet("{id}")]
        public async Task<IActionResult> Delete(int id)
        {
            var project = await _context.Projects
                .FirstOrDefaultAsync(p => p.Id == id);

            if (project == null) return NotFound();

            return View(project);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            await DeleteProjectAndChildren(id);
            return RedirectToAction(nameof(Index));
        }

        // ✅ برخی View ها ممکن است به طور مستقیم به /DeleteConfirmed پست کنند
        [HttpPost]
        [ActionName("DeleteConfirmed")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmedRoute(int id)
        {
            await DeleteProjectAndChildren(id);
            return RedirectToAction(nameof(Index));
        }

        private async Task DeleteProjectAndChildren(int id)
        {
            var project = await _context.Projects.FindAsync(id);
            if (project == null) return;

            // 🔥 حذف به ترتیب صحیح (از وابسته‌ترین به مستقل‌ترین)
            
            // 1) حذف IssueStatusHistories (وابسته به TaskItems)
            var histories = await _context.IssueStatusHistories
                .Where(h => _context.TaskItems.Any(t => t.Id == h.TaskId && t.ProjectId == id))
                .ToListAsync();
            _context.IssueStatusHistories.RemoveRange(histories);

            // 2) حذف WorkflowTransitions (وابسته به WorkflowStatuses)
            var transitions = _context.WorkflowTransitions.Where(t => t.ProjectId == id);
            _context.WorkflowTransitions.RemoveRange(transitions);

            // 3) حذف WorkflowStatuses (وابسته به Project)
            var statuses = _context.WorkflowStatuses.Where(s => s.ProjectId == id);
            _context.WorkflowStatuses.RemoveRange(statuses);

            // 4) حذف TaskItems (وابسته به Project)
            var tasks = _context.TaskItems.Where(t => t.ProjectId == id);
            _context.TaskItems.RemoveRange(tasks);

            // 5) حذف ProjectMembers (وابسته به Project)
            var members = _context.ProjectMembers.Where(m => m.ProjectId == id);
            _context.ProjectMembers.RemoveRange(members);

            // 6) حذف ProjectInvitations (وابسته به Project)
            var invitations = _context.ProjectInvitations.Where(i => i.ProjectId == id);
            _context.ProjectInvitations.RemoveRange(invitations);

            // 7) حذف ProjectNotes (وابسته به Project)
            var notes = _context.ProjectNotes.Where(n => n.ProjectId == id);
            _context.ProjectNotes.RemoveRange(notes);

            // 8) حذف Sprints (وابسته به Project)
            var sprints = _context.Sprints.Where(s => s.ProjectId == id);
            _context.Sprints.RemoveRange(sprints);

            // 9) حذف Project (آخرین)
            _context.Projects.Remove(project);
            
            await _context.SaveChangesAsync();
        }





        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> InviteUserToProject(int projectId, string phone)
        {
            var inviterId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(phone))
            {
                TempData["InviteError"] = "شماره تلفن وارد نشده است.";
                return RedirectToAction("Details", new { id = projectId });
            }

            // بررسی وجود دعوت در انتظار
            var exists = await _context.ProjectInvitations
                .AnyAsync(i => i.InviteePhone == phone && i.ProjectId == projectId && i.Status == InvitationStatus.Pending);

            if (exists)
            {
                TempData["InviteWarning"] = "برای این شماره قبلاً دعوت در انتظار ارسال شده است.";
                return RedirectToAction("Details", new { id = projectId });
            }

            // پیدا کردن کاربر
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.PhoneNumber == phone);
            if (user == null)
            {
                TempData["InviteError"] = "کاربری با این شماره پیدا نشد.";
                return RedirectToAction("Details", new { id = projectId });
            }

            // ثبت دعوت
            var invite = new ProjectInvitation
            {
                InviterId = inviterId,
                InviteePhone = phone,
                InviteeId = user.Id,
                ProjectId = projectId,
                Status = InvitationStatus.Pending
            };

            _context.ProjectInvitations.Add(invite);
            await _context.SaveChangesAsync();

            TempData["InviteSuccess"] = $"دعوت برای شماره {phone} ارسال شد.";
            return RedirectToAction("Details", new { id = projectId });
        }


    }
}
