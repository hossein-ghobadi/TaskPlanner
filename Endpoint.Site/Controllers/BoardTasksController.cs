using Endpoint.Site.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskPlanner.Domain.Entities.Boards;
using TaskPlanner.Domain.Entities.Users;
using TaskPlanner.Persistence.Contexts;
using TaskPlanner.Application.Services.FileUpload;
using Microsoft.AspNetCore.Identity;
using System.Collections.Generic;
using System.Linq;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class BoardTasksController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly IFileUploadService _fileUploadService;
        private readonly UserManager<User> _userManager;

        public BoardTasksController(
            MVPTestDatabaseContext context,
            IFileUploadService fileUploadService,
            UserManager<User> userManager)
        {
            _context = context;
            _fileUploadService = fileUploadService;
            _userManager = userManager;
        }

        // GET: ایجاد کار جدید
        public async Task<IActionResult> Create(int boardId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var board = await _context.Boards
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == boardId);

            if (board == null)
                return NotFound();

            // بررسی دسترسی
            if (board.CreatorUserId != userId && !board.Members.Any(m => m.UserId == userId))
                return Forbid();

            // دریافت ParentTaskId از query string (اگر کارک است)
            var parentTaskId = Request.Query["parentTaskId"].ToString();
            int? parentTaskIdInt = null;
            if (!string.IsNullOrEmpty(parentTaskId) && int.TryParse(parentTaskId, out var parsedId))
            {
                parentTaskIdInt = parsedId;
            }

            var vm = new BoardTaskCreateVm
            {
                BoardId = boardId,
                Status = "To Do",
                ParentTaskId = parentTaskIdInt
            };

            // لیست کاربران برای اختصاص
            var memberUserIds = board.Members.Select(m => m.UserId).ToList();
            memberUserIds.Add(board.CreatorUserId);

            var allUsers = await _context.Users
                .Where(u => memberUserIds.Contains(u.Id))
                .Select(u => new { u.Id, DisplayName = u.FullName ?? u.UserName ?? "ناشناس" })
                .ToListAsync();
            ViewBag.AvailableUsers = allUsers;
            ViewBag.BoardName = board.Name;

            return View(vm);
        }

        // POST: ایجاد کار جدید
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BoardTaskCreateVm vm)
        {
            if (!ModelState.IsValid)
            {
                var board = await _context.Boards
                    .Include(b => b.Members)
                    .FirstOrDefaultAsync(b => b.Id == vm.BoardId);

                if (board != null)
                {
                    var memberUserIds = board.Members.Select(m => m.UserId).ToList();
                    memberUserIds.Add(board.CreatorUserId);

                    var allUsers = await _context.Users
                        .Where(u => memberUserIds.Contains(u.Id))
                        .Select(u => new { u.Id, DisplayName = u.FullName ?? u.UserName ?? "ناشناس" })
                        .ToListAsync();
                    ViewBag.AvailableUsers = allUsers;
                    ViewBag.BoardName = board.Name;
                }
                return View(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var boardCheck = await _context.Boards
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == vm.BoardId);

            if (boardCheck == null)
                return NotFound();

            // بررسی دسترسی
            if (boardCheck.CreatorUserId != userId && !boardCheck.Members.Any(m => m.UserId == userId))
                return Forbid();

            // محاسبه ترتیب بعدی
            var maxOrder = await _context.BoardTasks
                .Where(t => t.BoardId == vm.BoardId && t.Status == vm.Status)
                .MaxAsync(t => (int?)t.Order) ?? 0;

            var task = new BoardTask
            {
                BoardId = vm.BoardId,
                Title = vm.Title,
                Description = vm.Description,
                Status = vm.Status,
                Order = maxOrder + 1,
                CreatorUserId = userId,
                AssignedUserId = vm.AssignedUserId,
                DueDate = vm.DueDate,
                ParentTaskId = vm.ParentTaskId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BoardTasks.Add(task);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Boards", new { id = vm.BoardId });
        }

        // POST: ایجاد کار جدید (API برای Modal)
        [HttpPost]
        public async Task<IActionResult> CreateTask([FromForm] BoardTaskCreateVm vm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = string.Join(", ", errors) });
            }

            var boardCheck = await _context.Boards
                .Include(b => b.Members)
                .FirstOrDefaultAsync(b => b.Id == vm.BoardId);

            if (boardCheck == null)
                return Json(new { success = false, message = "تخته یافت نشد" });

            // بررسی دسترسی
            if (boardCheck.CreatorUserId != userId && !boardCheck.Members.Any(m => m.UserId == userId))
                return Json(new { success = false, message = "دسترسی ندارید" });

            // محاسبه ترتیب بعدی
            int maxOrder;
            if (vm.ParentTaskId.HasValue)
            {
                // برای کارک‌ها، Order را بر اساس ParentTaskId محاسبه کن
                maxOrder = await _context.BoardTasks
                    .Where(t => t.BoardId == vm.BoardId && t.ParentTaskId == vm.ParentTaskId)
                    .MaxAsync(t => (int?)t.Order) ?? 0;
            }
            else
            {
                // برای کارهای اصلی، Order را بر اساس Status محاسبه کن
                maxOrder = await _context.BoardTasks
                    .Where(t => t.BoardId == vm.BoardId && t.Status == vm.Status && t.ParentTaskId == null)
                    .MaxAsync(t => (int?)t.Order) ?? 0;
            }

            var task = new BoardTask
            {
                BoardId = vm.BoardId,
                Title = vm.Title,
                Description = vm.Description,
                Status = vm.Status,
                Order = maxOrder + 1,
                CreatorUserId = userId,
                AssignedUserId = vm.AssignedUserId,
                DueDate = vm.DueDate,
                ParentTaskId = vm.ParentTaskId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            _context.BoardTasks.Add(task);
            await _context.SaveChangesAsync();

            // بارگذاری مجدد task با اطلاعات کامل
            var savedTask = await _context.BoardTasks
                .Include(t => t.Board)
                .FirstOrDefaultAsync(t => t.Id == task.Id);

            return Json(new { 
                success = true, 
                message = vm.ParentTaskId.HasValue ? "کارک با موفقیت ایجاد شد" : "کار با موفقیت ایجاد شد",
                task = new {
                    id = savedTask.Id,
                    title = savedTask.Title,
                    description = savedTask.Description,
                    status = savedTask.Status,
                    order = savedTask.Order,
                    parentTaskId = savedTask.ParentTaskId,
                    assignedUserId = savedTask.AssignedUserId,
                    createdAt = savedTask.CreatedAt
                }
            });
        }

        // GET: ویرایش کار
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _context.BoardTasks
                .Include(t => t.Board)
                .ThenInclude(b => b.Members)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            // بررسی دسترسی
            if (task.Board.CreatorUserId != userId && !task.Board.Members.Any(m => m.UserId == userId))
                return Forbid();

            var vm = new BoardTaskEditVm
            {
                Id = task.Id,
                BoardId = task.BoardId,
                Title = task.Title,
                Description = task.Description,
                Status = task.Status,
                Order = task.Order,
                AssignedUserId = task.AssignedUserId,
                DueDate = task.DueDate
            };

            // لیست کاربران برای اختصاص
            var memberUserIds = task.Board.Members.Select(m => m.UserId).ToList();
            memberUserIds.Add(task.Board.CreatorUserId);

            var allUsers = await _context.Users
                .Where(u => memberUserIds.Contains(u.Id))
                .Select(u => new { u.Id, DisplayName = u.FullName ?? u.UserName ?? "ناشناس" })
                .ToListAsync();
            ViewBag.AvailableUsers = allUsers;
            ViewBag.BoardName = task.Board.Name;

            return View(vm);
        }

        // POST: ویرایش کار
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, BoardTaskEditVm vm)
        {
            if (id != vm.Id)
                return NotFound();

            if (!ModelState.IsValid)
            {
                var task = await _context.BoardTasks
                    .Include(t => t.Board)
                    .ThenInclude(b => b.Members)
                    .FirstOrDefaultAsync(t => t.Id == id);

                if (task != null)
                {
                    var memberUserIds = task.Board.Members.Select(m => m.UserId).ToList();
                    memberUserIds.Add(task.Board.CreatorUserId);

                    var allUsers = await _context.Users
                        .Where(u => memberUserIds.Contains(u.Id))
                        .Select(u => new { u.Id, DisplayName = u.FullName ?? u.UserName ?? "ناشناس" })
                        .ToListAsync();
                    ViewBag.AvailableUsers = allUsers;
                    ViewBag.BoardName = task.Board.Name;
                }
                return View(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var taskToUpdate = await _context.BoardTasks
                .Include(t => t.Board)
                .ThenInclude(b => b.Members)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (taskToUpdate == null)
                return NotFound();

            // بررسی دسترسی
            if (taskToUpdate.Board.CreatorUserId != userId && !taskToUpdate.Board.Members.Any(m => m.UserId == userId))
                return Forbid();

            // اگر وضعیت تغییر کرده، ترتیب را در ستون جدید تنظیم کن
            if (taskToUpdate.Status != vm.Status)
            {
                var maxOrder = await _context.BoardTasks
                    .Where(t => t.BoardId == vm.BoardId && t.Status == vm.Status && t.Id != id)
                    .MaxAsync(t => (int?)t.Order) ?? 0;
                vm.Order = maxOrder + 1;
            }

            taskToUpdate.Title = vm.Title;
            taskToUpdate.Description = vm.Description;
            taskToUpdate.Status = vm.Status;
            taskToUpdate.Order = vm.Order;
            taskToUpdate.AssignedUserId = vm.AssignedUserId;
            taskToUpdate.DueDate = vm.DueDate;
            taskToUpdate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Boards", new { id = vm.BoardId });
        }

        // GET: دریافت اطلاعات کار برای ویرایش (API)
        [HttpGet]
        public async Task<IActionResult> GetTask(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _context.BoardTasks
                .Include(t => t.Board)
                .ThenInclude(b => b.Members)
                .Include(t => t.ChildTasks)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return Json(new { success = false, message = "کار یافت نشد" });

            // بررسی دسترسی
            if (task.Board.CreatorUserId != userId && !task.Board.Members.Any(m => m.UserId == userId))
                return Json(new { success = false, message = "دسترسی ندارید" });

            return Json(new { 
                success = true, 
                task = new {
                    id = task.Id,
                    boardId = task.BoardId,
                    title = task.Title,
                    description = task.Description,
                    status = task.Status,
                    order = task.Order,
                    assignedUserId = task.AssignedUserId,
                    dueDate = task.DueDate,
                    isCompleted = task.IsCompleted,
                    completedAt = task.CompletedAt,
                    parentTaskId = task.ParentTaskId,
                    createdAt = task.CreatedAt,
                    subtasks = task.ChildTasks.OrderBy(st => st.Order).ThenBy(st => st.CreatedAt).Select(st => new {
                        id = st.Id,
                        title = st.Title,
                        status = st.Status,
                        order = st.Order
                    }).ToList()
                }
            });
        }

        // POST: ویرایش کار (API برای Modal)
        [HttpPost]
        public async Task<IActionResult> UpdateTask([FromForm] BoardTaskPartialUpdateVm vm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (!ModelState.IsValid)
            {
                var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                return Json(new { success = false, message = string.Join(", ", errors) });
            }

            var taskToUpdate = await _context.BoardTasks
                .Include(t => t.Board)
                .ThenInclude(b => b.Members)
                .FirstOrDefaultAsync(t => t.Id == vm.Id);

            if (taskToUpdate == null)
                return Json(new { success = false, message = "کار یافت نشد" });

            // بررسی دسترسی
            if (taskToUpdate.Board.CreatorUserId != userId && !taskToUpdate.Board.Members.Any(m => m.UserId == userId))
                return Json(new { success = false, message = "دسترسی ندارید" });

            var oldStatus = taskToUpdate.Status;
            var oldOrder = taskToUpdate.Order;

            // اگر وضعیت تغییر کرده، ترتیب را در ستون جدید تنظیم کن (فقط برای کارهای اصلی)
            if (!string.IsNullOrEmpty(vm.Status) && taskToUpdate.Status != vm.Status && taskToUpdate.ParentTaskId == null)
            {
                // حذف از ستون قبلی
                var tasksInOldStatus = await _context.BoardTasks
                    .Where(t => t.BoardId == taskToUpdate.BoardId && t.Status == oldStatus && t.ParentTaskId == null && t.Order > oldOrder)
                    .ToListAsync();
                
                foreach (var t in tasksInOldStatus)
                {
                    t.Order--;
                }

                // افزودن به ستون جدید
                var maxOrder = await _context.BoardTasks
                    .Where(t => t.BoardId == taskToUpdate.BoardId && t.Status == vm.Status && t.ParentTaskId == null && t.Id != vm.Id)
                    .MaxAsync(t => (int?)t.Order) ?? 0;
                
                taskToUpdate.Order = maxOrder + 1;
            }

            // فقط فیلدهای ارسال‌شده را به‌روزرسانی کن
            if (!string.IsNullOrEmpty(vm.Title))
                taskToUpdate.Title = vm.Title;
            
            if (vm.Description != null) // null check به جای string.IsNullOrEmpty چون ممکنه بخوایم توضیحات رو خالی کنیم
                taskToUpdate.Description = vm.Description;
            
            if (!string.IsNullOrEmpty(vm.Status))
                taskToUpdate.Status = vm.Status;
            
            if (vm.AssignedUserId != null) // فقط اگر ارسال شده باشد
                taskToUpdate.AssignedUserId = vm.AssignedUserId;
            
            if (vm.DueDate.HasValue || Request.Form.ContainsKey("DueDate")) // اگر DueDate ارسال شده (حتی null)
                taskToUpdate.DueDate = vm.DueDate;
            
            if (vm.Order.HasValue)
                taskToUpdate.Order = vm.Order.Value;
            
            taskToUpdate.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                message = "کار با موفقیت ویرایش شد"
            });
        }

        // GET: حذف کار
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _context.BoardTasks
                .Include(t => t.Board)
                .ThenInclude(b => b.Members)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            // بررسی دسترسی
            if (task.Board.CreatorUserId != userId && !task.Board.Members.Any(m => m.UserId == userId))
                return Forbid();

            return View(task);
        }

        // POST: حذف کار
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _context.BoardTasks
                .Include(t => t.Board)
                .ThenInclude(b => b.Members)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return NotFound();

            // بررسی دسترسی
            if (task.Board.CreatorUserId != userId && !task.Board.Members.Any(m => m.UserId == userId))
                return Forbid();

            var boardId = task.BoardId;
            _context.BoardTasks.Remove(task);
            await _context.SaveChangesAsync();

            return RedirectToAction("Details", "Boards", new { id = boardId });
        }

        // POST: تغییر وضعیت تکمیل کار
        [HttpPost]
        public async Task<IActionResult> ToggleComplete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _context.BoardTasks
                .Include(t => t.Board)
                .ThenInclude(b => b.Members)
                .Include(t => t.ChildTasks) // بارگذاری کارک‌ها (چک لیست)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return Json(new { success = false, message = "کار یافت نشد" });

            // بررسی دسترسی
            if (task.Board.CreatorUserId != userId && !task.Board.Members.Any(m => m.UserId == userId))
                return Json(new { success = false, message = "دسترسی ندارید" });

            task.IsCompleted = !task.IsCompleted;
            task.CompletedAt = task.IsCompleted ? DateTime.UtcNow : null;
            task.UpdatedAt = DateTime.UtcNow;

            // اگر کار تکمیل شد، همه کارک‌ها رو هم تکمیل کن
            if (task.IsCompleted)
            {
                foreach (var subtask in task.ChildTasks)
                {
                    subtask.Status = "Done";
                    subtask.IsCompleted = true;
                    subtask.CompletedAt = DateTime.UtcNow;
                    subtask.UpdatedAt = DateTime.UtcNow;
                }
            }
            // اگر کار uncomplete شد، همه کارک‌ها رو هم uncomplete کن
            else
            {
                foreach (var subtask in task.ChildTasks)
                {
                    subtask.Status = "To Do";
                    subtask.IsCompleted = false;
                    subtask.CompletedAt = null;
                    subtask.UpdatedAt = DateTime.UtcNow;
                }
            }

            await _context.SaveChangesAsync();

            return Json(new { 
                success = true, 
                isCompleted = task.IsCompleted,
                completedAt = task.CompletedAt
            });
        }

        // POST: حذف کار (API برای Modal)
        [HttpPost]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _context.BoardTasks
                .Include(t => t.Board)
                .ThenInclude(b => b.Members)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (task == null)
                return Json(new { success = false, message = "کار یافت نشد" });

            // بررسی دسترسی
            if (task.Board.CreatorUserId != userId && !task.Board.Members.Any(m => m.UserId == userId))
                return Json(new { success = false, message = "دسترسی ندارید" });

            var boardId = task.BoardId;
            var oldStatus = task.Status;
            var oldOrder = task.Order;

            // اگر کار اصلی است، ابتدا کارک‌های آن را حذف کن
            if (task.ParentTaskId == null)
            {
                var subtasks = await _context.BoardTasks
                    .Where(t => t.ParentTaskId == task.Id)
                    .ToListAsync();
                
                if (subtasks.Any())
                {
                    _context.BoardTasks.RemoveRange(subtasks);
                }

                // ترتیب کارهای بعدی را تنظیم کن
                var tasksInSameStatus = await _context.BoardTasks
                    .Where(t => t.BoardId == boardId && t.Status == oldStatus && t.ParentTaskId == null && t.Order > oldOrder)
                    .ToListAsync();
                
                foreach (var t in tasksInSameStatus)
                {
                    t.Order--;
                }
            }

            _context.BoardTasks.Remove(task);
            await _context.SaveChangesAsync();

            return Json(new { success = true, message = "کار با موفقیت حذف شد", boardId = boardId });
        }

        // POST: به‌روزرسانی ترتیب کارها (برای drag and drop)
        [HttpPost]
        public async Task<IActionResult> UpdateTaskOrder([FromBody] UpdateTaskOrderRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (request == null || request.TaskOrders == null || !request.TaskOrders.Any())
                return Json(new { success = false, message = "داده‌های نامعتبر" });

            try
            {
                // بررسی دسترسی به تخته
                var firstTask = await _context.BoardTasks
                    .Include(t => t.Board)
                    .ThenInclude(b => b.Members)
                    .FirstOrDefaultAsync(t => t.Id == request.TaskOrders.First().TaskId);

                if (firstTask == null)
                    return Json(new { success = false, message = "کار یافت نشد" });

                if (firstTask.Board.CreatorUserId != userId && !firstTask.Board.Members.Any(m => m.UserId == userId))
                    return Json(new { success = false, message = "دسترسی ندارید" });

                // به‌روزرسانی ترتیب و وضعیت کارها
                foreach (var taskOrder in request.TaskOrders)
                {
                    var task = await _context.BoardTasks.FindAsync(taskOrder.TaskId);
                    if (task != null && task.BoardId == firstTask.BoardId)
                    {
                        task.Status = taskOrder.Status;
                        task.Order = taskOrder.Order;
                        task.UpdatedAt = DateTime.UtcNow;
                    }
                }

                await _context.SaveChangesAsync();

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = $"خطا: {ex.Message}" });
            }
        }

        // GET: دریافت لیست کاربران برای dropdown
        [HttpGet]
        public async Task<IActionResult> GetAvailableUsers(int boardId)
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

            var memberUserIds = board.Members.Select(m => m.UserId).ToList();
            memberUserIds.Add(board.CreatorUserId);

            var allUsers = await _context.Users
                .Where(u => memberUserIds.Contains(u.Id))
                .Select(u => new { 
                    id = u.Id, 
                    displayName = u.FullName ?? u.UserName ?? "ناشناس" 
                })
                .ToListAsync();

            return Json(new { success = true, users = allUsers });
        }

        // GET: دریافت یادداشت‌های یک کار
        [HttpGet]
        public async Task<IActionResult> GetComments(int taskId)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var task = await _context.BoardTasks
                .Include(t => t.Board)
                .ThenInclude(b => b.Members)
                .FirstOrDefaultAsync(t => t.Id == taskId);

            if (task == null)
                return Json(new { success = false, message = "کار یافت نشد" });

            // بررسی دسترسی
            if (task.Board.CreatorUserId != userId && !task.Board.Members.Any(m => m.UserId == userId))
                return Json(new { success = false, message = "دسترسی ندارید" });

            var comments = await _context.BoardTaskComments
                .Include(c => c.Attachments)
                .Where(c => c.BoardTaskId == taskId && !c.IsDeleted)
                .OrderBy(c => c.CreatedAt)
                .Select(c => new BoardTaskCommentVm
                {
                    Id = c.Id,
                    Message = c.Message,
                    BoardTaskId = c.BoardTaskId,
                    UserId = c.UserId,
                    UserName = c.UserName,
                    CreatedAt = c.CreatedAt,
                    UpdatedAt = c.UpdatedAt,
                    IsEdited = c.IsEdited,
                    IsDeleted = c.IsDeleted,
                    Attachments = c.Attachments.Select(a => new BoardTaskCommentAttachmentVm
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

            return Json(new { success = true, comments = comments });
        }

        // POST: افزودن یادداشت به کار
        [HttpPost]
        public async Task<IActionResult> AddComment([FromForm] BoardTaskCommentCreateVm vm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // لاگ برای دیباگ
            System.Diagnostics.Debug.WriteLine($"AddComment called - Message: {vm?.Message}, Attachments count: {vm?.Attachments?.Count ?? 0}");
            
            if (vm?.Attachments != null)
            {
                foreach (var file in vm.Attachments)
                {
                    System.Diagnostics.Debug.WriteLine($"File received: {file.FileName}, Size: {file.Length}, ContentType: {file.ContentType}");
                }
            }

            if (string.IsNullOrWhiteSpace(vm?.Message) && (vm?.Attachments == null || !vm.Attachments.Any()))
                return Json(new { success = false, message = "یادداشت یا فایل پیوست الزامی است" });

            var task = await _context.BoardTasks
                .Include(t => t.Board)
                .ThenInclude(b => b.Members)
                .FirstOrDefaultAsync(t => t.Id == vm.BoardTaskId);

            if (task == null)
                return Json(new { success = false, message = "کار یافت نشد" });

            // بررسی دسترسی
            if (task.Board.CreatorUserId != userId && !task.Board.Members.Any(m => m.UserId == userId))
                return Json(new { success = false, message = "دسترسی ندارید" });

            var user = await _userManager.FindByIdAsync(userId);
            var userName = user?.FullName ?? user?.UserName ?? "ناشناس";

            var comment = new BoardTaskComment
            {
                BoardTaskId = vm.BoardTaskId,
                Message = vm.Message,
                UserId = userId,
                UserName = userName,
                CreatedAt = DateTime.UtcNow
            };

            _context.BoardTaskComments.Add(comment);
            await _context.SaveChangesAsync();

            // آپلود فایل‌های پیوست
            if (vm.Attachments != null && vm.Attachments.Any())
            {
                var uploadErrors = new List<string>();
                
                foreach (var file in vm.Attachments)
                {
                    var uploadResult = await _fileUploadService.UploadFileAsync(file, "board-task-comments");

                    if (uploadResult.Success)
                    {
                        var attachment = new BoardTaskCommentAttachment
                        {
                            BoardTaskCommentId = comment.Id,
                            FileName = file.FileName,
                            FilePath = uploadResult.FilePath,
                            FileType = _fileUploadService.GetFileType(file.FileName),
                            FileSize = file.Length,
                            MimeType = file.ContentType,
                            UploadedAt = DateTime.UtcNow
                        };

                        _context.BoardTaskCommentAttachments.Add(attachment);
                    }
                    else
                    {
                        uploadErrors.Add($"{file.FileName}: {uploadResult.Error}");
                    }
                }

                await _context.SaveChangesAsync();
                
                // اگر خطا در آپلود وجود داشت، پیام خطا را برگردان (اما یادداشت قبلاً ذخیره شده)
                if (uploadErrors.Any())
                {
                    return Json(new { success = false, message = $"خطا در آپلود برخی فایل‌ها:\n{string.Join("\n", uploadErrors)}" });
                }
            }

            // بارگذاری مجدد comment با attachments
            var savedComment = await _context.BoardTaskComments
                .Include(c => c.Attachments)
                .FirstOrDefaultAsync(c => c.Id == comment.Id);

            var commentVm = new BoardTaskCommentVm
            {
                Id = savedComment.Id,
                Message = savedComment.Message,
                BoardTaskId = savedComment.BoardTaskId,
                UserId = savedComment.UserId,
                UserName = savedComment.UserName,
                CreatedAt = savedComment.CreatedAt,
                UpdatedAt = savedComment.UpdatedAt,
                IsEdited = savedComment.IsEdited,
                IsDeleted = savedComment.IsDeleted,
                Attachments = savedComment.Attachments.Select(a => new BoardTaskCommentAttachmentVm
                {
                    Id = a.Id,
                    FileName = a.FileName,
                    FilePath = a.FilePath,
                    FileType = a.FileType,
                    FileSize = a.FileSize,
                    MimeType = a.MimeType,
                    UploadedAt = a.UploadedAt
                }).ToList()
            };

            return Json(new { success = true, comment = commentVm });
        }

        // POST: حذف یادداشت
        [HttpPost]
        public async Task<IActionResult> DeleteComment([FromBody] DeleteCommentRequest request)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            if (request == null || request.CommentId <= 0)
                return Json(new { success = false, message = "شناسه یادداشت نامعتبر است" });

            var comment = await _context.BoardTaskComments
                .Include(c => c.BoardTask)
                .ThenInclude(t => t.Board)
                .ThenInclude(b => b.Members)
                .FirstOrDefaultAsync(c => c.Id == request.CommentId);

            if (comment == null)
                return Json(new { success = false, message = "یادداشت یافت نشد" });

            // بررسی دسترسی - فقط سازنده comment یا سازنده تخته می‌تواند حذف کند
            var isCommentOwner = comment.UserId == userId;
            var isBoardCreator = comment.BoardTask.Board.CreatorUserId == userId;

            if (!isCommentOwner && !isBoardCreator)
                return Json(new { success = false, message = "دسترسی ندارید" });

            comment.IsDeleted = true;
            comment.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }


        public class DeleteCommentRequest
        {
            public int CommentId { get; set; }
        }
    }

    public class UpdateTaskOrderRequest
    {
        public List<TaskOrderItem> TaskOrders { get; set; } = new();
    }

    public class TaskOrderItem
    {
        public int TaskId { get; set; }
        public string Status { get; set; } = null!;
        public int Order { get; set; }
    }
}




