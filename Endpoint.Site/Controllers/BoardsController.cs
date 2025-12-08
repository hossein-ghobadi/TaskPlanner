using Endpoint.Site.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskPlanner.Domain.Entities.Boards;
using TaskPlanner.Domain.Entities.Users;
using TaskPlanner.Persistence.Contexts;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class BoardsController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;

        public BoardsController(MVPTestDatabaseContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: لیست تخته‌های کاربر
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // تخته‌هایی که کاربر ایجاد کرده یا عضو آن است
            var boards = await _context.Boards
                .Where(b => b.CreatorUserId == userId || 
                           b.Members.Any(m => m.UserId == userId))
                .OrderByDescending(b => b.UpdatedAt)
                .ToListAsync();

            return View(boards);
        }

        // GET: جزئیات تخته
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی به تخته
            var board = await _context.Boards
                .Include(b => b.Members)
                .Include(b => b.Tasks)
                    .ThenInclude(t => t.ChildTasks)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (board == null)
                return NotFound();

            // بررسی دسترسی
            if (board.CreatorUserId != userId && !board.Members.Any(m => m.UserId == userId))
                return Forbid();

            // دریافت اطلاعات سازنده
            var creator = await _userManager.FindByIdAsync(board.CreatorUserId);
            
            // دریافت اطلاعات اعضا
            var memberInfos = new List<BoardMemberInfo>();
            foreach (var member in board.Members)
            {
                var user = await _userManager.FindByIdAsync(member.UserId);
                memberInfos.Add(new BoardMemberInfo
                {
                    UserId = member.UserId,
                    DisplayName = user?.FullName ?? user?.UserName ?? "ناشناس"
                });
            }

            // گروه‌بندی کارها بر اساس وضعیت (فقط کارهای اصلی، نه کارک‌ها)
            var tasksByStatus = board.Tasks
                .Where(t => t.ParentTaskId == null) // فقط کارهای اصلی
                .OrderBy(t => t.Order)
                .ThenBy(t => t.CreatedAt)
                .GroupBy(t => t.Status)
                .ToDictionary(g => g.Key, g => g.ToList());

            // دریافت وضعیت‌های تخته از دیتابیس
            var boardStatuses = await _context.BoardStatuses
                .Where(s => s.BoardId == board.Id)
                .OrderBy(s => s.Order)
                .ToListAsync();

            // اگر وضعیت‌ای وجود ندارد، فقط "To Do" را ایجاد کن
            if (!boardStatuses.Any())
            {
                var toDoStatus = new BoardStatus 
                { 
                    Name = "To Do", 
                    Order = 1, 
                    BoardId = board.Id, 
                    Color = "#6c757d",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                _context.BoardStatuses.Add(toDoStatus);
                await _context.SaveChangesAsync();
                boardStatuses = new List<BoardStatus> { toDoStatus };
            }

            var vm = new BoardDetailsVm
            {
                Id = board.Id,
                Name = board.Name,
                Description = board.Description,
                CreatorUserId = board.CreatorUserId,
                CreatorUserName = creator?.FullName ?? creator?.UserName ?? "ناشناس",
                Members = memberInfos,
                TasksByStatus = tasksByStatus,
                AllTasks = board.Tasks.OrderBy(t => t.Order).ThenBy(t => t.CreatedAt).ToList(),
                TotalTasks = board.Tasks.Count,
                CompletedTasks = board.Tasks.Count(t => t.Status == "Done"),
                Statuses = boardStatuses
            };

            // لیست کاربران برای اختصاص کار
            var memberUserIds = board.Members.Select(m => m.UserId).ToList();
            memberUserIds.Add(board.CreatorUserId);
            
            var allUsers = await _userManager.Users
                .Where(u => memberUserIds.Contains(u.Id))
                .Select(u => new { u.Id, DisplayName = u.FullName ?? u.UserName ?? "ناشناس" })
                .ToListAsync();
            ViewBag.AvailableUsers = allUsers;

            return View(vm);
        }

        // GET: ایجاد تخته جدید
        public IActionResult Create()
        {
            return View();
        }

        // POST: ایجاد تخته جدید
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BoardCreateVm vm)
        {
            if (!ModelState.IsValid)
                return View(vm);

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var board = new Board
            {
                Name = vm.Name,
                Description = vm.Description,
                CreatorUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.Boards.Add(board);
            await _context.SaveChangesAsync();

            // ایجاد فقط وضعیت "To Do" به عنوان وضعیت پیش‌فرض
            var toDoStatus = new BoardStatus 
            { 
                Name = "To Do", 
                Order = 1, 
                BoardId = board.Id, 
                Color = "#6c757d",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            _context.BoardStatuses.Add(toDoStatus);
            await _context.SaveChangesAsync();

            // افزودن اعضای انتخاب‌شده
            if (vm.SelectedUserIds != null && vm.SelectedUserIds.Any())
            {
                foreach (var selectedUserId in vm.SelectedUserIds.Distinct())
                {
                    if (selectedUserId != userId) // کاربر نمی‌تواند خودش را اضافه کند
                    {
                        var member = new BoardMember
                        {
                            BoardId = board.Id,
                            UserId = selectedUserId,
                            JoinedAt = DateTime.UtcNow
                        };
                        _context.BoardMembers.Add(member);
                    }
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = board.Id });
        }

        // GET: ویرایش تخته
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var board = await _context.Boards.FindAsync(id);
            if (board == null)
                return NotFound();

            // فقط سازنده می‌تواند ویرایش کند
            if (board.CreatorUserId != userId)
                return Forbid();

            var vm = new BoardEditVm
            {
                Id = board.Id,
                Name = board.Name,
                Description = board.Description
            };

            return View(vm);
        }

        // POST: ویرایش تخته
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BoardEditVm vm)
        {
            if (id != vm.Id)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var board = await _context.Boards.FindAsync(id);
            if (board == null)
                return NotFound();

            // فقط سازنده می‌تواند ویرایش کند
            if (board.CreatorUserId != userId)
                return Forbid();

            if (!ModelState.IsValid)
            {
                if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
                {
                    return Json(new { success = false, message = "داده‌های نامعتبر", errors = ModelState });
                }
                return View(vm);
            }

            board.Name = vm.Name;
            board.Description = vm.Description;
            board.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            if (Request.Headers["X-Requested-With"] == "XMLHttpRequest")
            {
                return Json(new { success = true, message = "تخته با موفقیت ویرایش شد" });
            }

            return RedirectToAction(nameof(Details), new { id = board.Id });
        }

        // GET: دریافت اطلاعات تخته برای ویرایش (API)
        [HttpGet]
        public async Task<IActionResult> GetBoard(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var board = await _context.Boards.FindAsync(id);
            if (board == null)
                return Json(new { success = false, message = "تخته یافت نشد" });

            // فقط سازنده می‌تواند ویرایش کند
            if (board.CreatorUserId != userId)
                return Json(new { success = false, message = "دسترسی ندارید" });

            return Json(new { 
                success = true, 
                board = new 
                { 
                    id = board.Id, 
                    name = board.Name, 
                    description = board.Description 
                } 
            });
        }

        // GET: حذف تخته
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var board = await _context.Boards
                .Include(b => b.Tasks)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (board == null)
                return NotFound();

            // فقط سازنده می‌تواند حذف کند
            if (board.CreatorUserId != userId)
                return Forbid();

            return View(board);
        }

        // POST: حذف تخته
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var board = await _context.Boards
                .Include(b => b.Tasks)
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == id);

            if (board == null)
                return NotFound();

            // فقط سازنده می‌تواند حذف کند
            if (board.CreatorUserId != userId)
                return Forbid();

            _context.Boards.Remove(board);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: دعوت کاربر به تخته
        public async Task<IActionResult> Invite(int boardId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var board = await _context.Boards
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null)
                return NotFound();

            // فقط سازنده یا اعضا می‌توانند دعوت کنند
            if (board.CreatorUserId != userId && !board.Members.Any(m => m.UserId == userId))
                return Forbid();

            // لیست کاربران موجود (به جز اعضای فعلی)
            var existingMemberIds = board.Members.Select(m => m.UserId).ToList();
            existingMemberIds.Add(board.CreatorUserId);

            var availableUsers = await _userManager.Users
                .Where(u => !existingMemberIds.Contains(u.Id))
                .Select(u => new { u.Id, DisplayName = u.FullName ?? u.UserName ?? "ناشناس" })
                .ToListAsync();

            ViewBag.BoardId = boardId;
            ViewBag.BoardName = board.Name;
            ViewBag.AvailableUsers = availableUsers;

            return View();
        }

        // POST: دعوت کاربر به تخته
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Invite(int boardId, List<string> selectedUserIds)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var board = await _context.Boards
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null)
                return NotFound();

            // فقط سازنده یا اعضا می‌توانند دعوت کنند
            if (board.CreatorUserId != userId && !board.Members.Any(m => m.UserId == userId))
                return Forbid();

            if (selectedUserIds != null && selectedUserIds.Any())
            {
                var existingMemberIds = board.Members.Select(m => m.UserId).ToList();
                existingMemberIds.Add(board.CreatorUserId);

                foreach (var selectedUserId in selectedUserIds.Distinct())
                {
                    if (!existingMemberIds.Contains(selectedUserId))
                    {
                        var member = new BoardMember
                        {
                            BoardId = boardId,
                            UserId = selectedUserId,
                            JoinedAt = DateTime.UtcNow
                        };
                        _context.BoardMembers.Add(member);
                    }
                }
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = boardId });
        }

        // POST: حذف عضو از تخته
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RemoveMember(int boardId, string userId)
        {
            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var board = await _context.Boards
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null)
                return NotFound();

            // فقط سازنده می‌تواند اعضا را حذف کند
            if (board.CreatorUserId != currentUserId)
                return Forbid();

            var member = board.Members.FirstOrDefault(m => m.UserId == userId);
            if (member != null)
            {
                _context.BoardMembers.Remove(member);
                await _context.SaveChangesAsync();
            }

            return RedirectToAction(nameof(Details), new { id = boardId });
        }

        // GET: دریافت وضعیت‌های یک تخته
        [HttpGet]
        public async Task<IActionResult> GetStatuses(int boardId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var board = await _context.Boards
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null)
                return Json(new { success = false, message = "تخته یافت نشد" });

            // بررسی دسترسی
            if (board.CreatorUserId != userId && !board.Members.Any(m => m.UserId == userId))
                return Json(new { success = false, message = "دسترسی ندارید" });

            var statuses = await _context.BoardStatuses
                .Where(s => s.BoardId == boardId)
                .OrderBy(s => s.Order)
                .Select(s => new
                {
                    id = s.Id,
                    name = s.Name,
                    description = s.Description,
                    color = s.Color,
                    order = s.Order
                })
                .ToListAsync();

            return Json(new { success = true, statuses = statuses });
        }

        // POST: افزودن وضعیت جدید
        [HttpPost]
        public async Task<IActionResult> AddStatus(int boardId, string name, string? description = null, string? color = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (string.IsNullOrWhiteSpace(name))
                return Json(new { success = false, message = "نام وضعیت الزامی است" });

            var board = await _context.Boards
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null)
                return Json(new { success = false, message = "تخته یافت نشد" });

            // بررسی دسترسی
            if (board.CreatorUserId != userId && !board.Members.Any(m => m.UserId == userId))
                return Json(new { success = false, message = "دسترسی ندارید" });

            // بررسی تکراری نبودن نام
            var existingStatus = await _context.BoardStatuses
                .FirstOrDefaultAsync(s => s.BoardId == boardId && s.Name == name);
            
            if (existingStatus != null)
                return Json(new { success = false, message = "وضعیت با این نام قبلاً وجود دارد" });

            // محاسبه Order بعدی برای اضافه شدن بعد از "To Do"
            // "To Do" همیشه Order = 1 دارد، پس وضعیت‌های جدید از Order = 2 شروع می‌شوند
            var allStatuses = await _context.BoardStatuses
                .Where(s => s.BoardId == boardId)
                .ToListAsync();
            
            var toDoStatus = allStatuses.FirstOrDefault(s => s.Name.ToLower() == "to do");
            
            int newOrder;
            if (toDoStatus != null)
            {
                // محاسبه Order بعدی (بیشترین Order بین وضعیت‌های غیر "To Do" + 1)
                var maxOrder = allStatuses
                    .Where(s => s.Name.ToLower() != "to do")
                    .Select(s => (int?)s.Order)
                    .DefaultIfEmpty(1)
                    .Max() ?? 1;
                
                newOrder = maxOrder + 1;
            }
            else
            {
                // اگر "To Do" وجود نداشت، Order = 2 می‌دهیم
                newOrder = 2;
            }

            var status = new BoardStatus
            {
                BoardId = boardId,
                Name = name,
                Description = description,
                Color = color ?? "#6c757d",
                Order = newOrder, // اضافه شدن بعد از "To Do" (از سمت راست)
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BoardStatuses.Add(status);
            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = "وضعیت با موفقیت اضافه شد",
                status = new
                {
                    id = status.Id,
                    name = status.Name,
                    description = status.Description,
                    color = status.Color,
                    order = status.Order
                }
            });
        }

        // POST: حذف وضعیت
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteStatus(int statusId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var status = await _context.BoardStatuses
                .Include(s => s.Board)
                .ThenInclude(b => b.Members)
                .FirstOrDefaultAsync(s => s.Id == statusId);

            if (status == null)
                return Json(new { success = false, message = "وضعیت یافت نشد" });

            // بررسی دسترسی
            if (status.Board.CreatorUserId != userId && !status.Board.Members.Any(m => m.UserId == userId))
                return Json(new { success = false, message = "دسترسی ندارید" });

            // جلوگیری از حذف وضعیت "To Do"
            if (status.Name.ToLower() == "to do")
                return Json(new { success = false, message = "وضعیت 'To Do' قابل حذف نیست" });

            // بررسی اینکه آیا کار در این وضعیت وجود دارد
            var tasksCount = await _context.BoardTasks
                .CountAsync(t => t.BoardId == status.BoardId && t.Status == status.Name && t.ParentTaskId == null);
            
            if (tasksCount > 0)
                return Json(new { success = false, message = $"نمی‌توان این وضعیت را حذف کرد. {tasksCount} کار در این وضعیت وجود دارد." });

            _context.BoardStatuses.Remove(status);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "وضعیت با موفقیت حذف شد" });
        }

        // POST: تغییر ترتیب وضعیت‌ها
        [HttpPost]
        public async Task<IActionResult> UpdateStatusOrder([FromBody] UpdateStatusOrderRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (request == null || request.StatusOrders == null || !request.StatusOrders.Any())
                return Json(new { success = false, message = "داده‌های نامعتبر" });

            var board = await _context.Boards
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == request.BoardId);

            if (board == null)
                return Json(new { success = false, message = "تخته یافت نشد" });

            // بررسی دسترسی
            if (board.CreatorUserId != userId && !board.Members.Any(m => m.UserId == userId))
                return Json(new { success = false, message = "دسترسی ندارید" });

            // اطمینان از اینکه "To Do" همیشه اول باشد (Order = 1)
            var allStatuses = await _context.BoardStatuses
                .Where(s => s.BoardId == request.BoardId)
                .ToListAsync();
            
            var toDoStatus = allStatuses.FirstOrDefault(s => s.Name.ToLower() == "to do");
            
            if (toDoStatus != null)
            {
                toDoStatus.Order = 1; // همیشه اول
                toDoStatus.UpdatedAt = DateTime.UtcNow;
            }

            foreach (var item in request.StatusOrders)
            {
                var status = allStatuses.FirstOrDefault(s => s.Id == item.StatusId);
                
                if (status != null && status.Name.ToLower() != "to do")
                {
                    // "To Do" همیشه Order = 1 است، پس Order بقیه از 2 شروع می‌شود
                    status.Order = item.Order >= 1 ? item.Order + 1 : item.Order;
                    status.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "ترتیب وضعیت‌ها با موفقیت به‌روزرسانی شد" });
        }
    }

    public class UpdateStatusOrderRequest
    {
        public int BoardId { get; set; }
        public List<BoardStatusOrderItem> StatusOrders { get; set; } = new();
    }

    public class BoardStatusOrderItem
    {
        public int StatusId { get; set; }
        public int Order { get; set; }
    }
}

