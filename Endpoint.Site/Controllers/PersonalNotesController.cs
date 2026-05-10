using Endpoint.Site.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskPlanner.Application.Services.FileUpload;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Domain.Entities.Users;
using TaskPlanner.Persistence.Contexts;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class PersonalNotesController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IFileUploadService _fileUploadService;

        public PersonalNotesController(MVPTestDatabaseContext context, UserManager<User> userManager, IFileUploadService fileUploadService)
        {
            _context = context;
            _userManager = userManager;
            _fileUploadService = fileUploadService;
        }

        // 📌 لیست یادداشت‌های شخصی
        [HttpGet]
        public async Task<IActionResult> Index(int? folderId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // اگر folderId مشخص شده، بررسی کن که متعلق به کاربر باشد
            if (folderId.HasValue)
            {
                var folder = await _context.PersonalNoteFolders
                    .FirstOrDefaultAsync(f => f.Id == folderId.Value && f.UserId == userId);

                if (folder == null)
                {
                    TempData["Error"] = "پوشه یافت نشد یا به آن دسترسی ندارید.";
                    return RedirectToAction(nameof(Index));
                }

                ViewBag.CurrentFolder = folder;
            }

            // دریافت تمام پوشه‌های کاربر برای محاسبه تعداد یادداشت‌ها
            var allFolders = await _context.PersonalNoteFolders
                .Where(f => f.UserId == userId)
                .ToListAsync();

            // دریافت پوشه‌های سطح اول
            var folders = allFolders
                .Where(f => f.ParentFolderId == folderId)
                .OrderBy(f => f.Name)
                .ToList();

            // محاسبه تعداد کل یادداشت‌ها برای هر پوشه (شامل زیرپوشه‌ها)
            var folderNotesCount = new Dictionary<int, int>();
            foreach (var folder in allFolders)
            {
                var folderIdsInTree = GetFolderTreeIds(folder.Id, allFolders);
                var totalNotes = await _context.PersonalNotes
                    .Where(n => n.UserId == userId && n.FolderId.HasValue && folderIdsInTree.Contains(n.FolderId.Value))
                    .CountAsync();
                folderNotesCount[folder.Id] = totalNotes;
            }

            var notes = await _context.PersonalNotes
                .Include(n => n.Attachments)
                .Include(n => n.Folder)
                .Where(n => n.UserId == userId && n.FolderId == folderId)
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.CreatedAt)
                .ToListAsync();

            ViewBag.Folders = folders;
            ViewBag.FolderId = folderId;
            ViewBag.FolderNotesCount = folderNotesCount;

            return View(notes);
        }

        // 📌 جزئیات یادداشت
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var note = await _context.PersonalNotes
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (note == null)
            {
                TempData["Error"] = "یادداشت یافت نشد یا به آن دسترسی ندارید.";
                return RedirectToAction(nameof(Index));
            }

            return View(note);
        }

        // 📌 ایجاد یادداشت جدید
        [HttpGet]
        public async Task<IActionResult> Create(int? folderId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // اگر folderId مشخص شده، بررسی کن که متعلق به کاربر باشد
            if (folderId.HasValue)
            {
                var folder = await _context.PersonalNoteFolders
                    .FirstOrDefaultAsync(f => f.Id == folderId.Value && f.UserId == userId);

                if (folder == null)
                {
                    TempData["Error"] = "پوشه یافت نشد یا به آن دسترسی ندارید.";
                    return RedirectToAction(nameof(Index));
                }
            }

            var vm = new PersonalNoteCreateVm
            {
                FolderId = folderId
            };

            // لیست پوشه‌های موجود برای انتخاب
            ViewBag.Folders = await GetFoldersSelectListAsync(userId, folderId);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PersonalNoteCreateVm vm)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی مستقیم Request.Form برای FolderId
            // این کار برای حل مشکل Model Binding با select value="" است
            if (Request.Form.ContainsKey("FolderId"))
            {
                var folderIdValue = Request.Form["FolderId"].ToString();
                if (string.IsNullOrWhiteSpace(folderIdValue) || folderIdValue == "0")
                {
                    // اگر select خالی است، بررسی کن که آیا InitialFolderId وجود دارد
                    // این برای زمانی است که کاربر از یک پوشه به Create آمده اما select را تغییر نداده
                    if (Request.Form.ContainsKey("InitialFolderId"))
                    {
                        var initialFolderIdValue = Request.Form["InitialFolderId"].ToString();
                        if (!string.IsNullOrWhiteSpace(initialFolderIdValue) && int.TryParse(initialFolderIdValue, out int initialFolderId))
                        {
                            vm.FolderId = initialFolderId;
                        }
                        else
                        {
                            vm.FolderId = null;
                        }
                    }
                    else
                    {
                        vm.FolderId = null;
                    }
                }
                else if (int.TryParse(folderIdValue, out int folderId))
                {
                    vm.FolderId = folderId;
                }
            }
            else
            {
                // اگر FolderId اصلاً ارسال نشده، بررسی InitialFolderId
                if (Request.Form.ContainsKey("InitialFolderId"))
                {
                    var initialFolderIdValue = Request.Form["InitialFolderId"].ToString();
                    if (!string.IsNullOrWhiteSpace(initialFolderIdValue) && int.TryParse(initialFolderIdValue, out int initialFolderId))
                    {
                        vm.FolderId = initialFolderId;
                    }
                    else
                    {
                        vm.FolderId = null;
                    }
                }
                else
                {
                    vm.FolderId = null;
                }
            }

            if (!ModelState.IsValid)
            {
                ViewBag.Folders = await GetFoldersSelectListAsync(userId, vm.FolderId);
                return View(vm);
            }

            // بررسی اینکه اگر FolderId مشخص شده، متعلق به کاربر باشد
            if (vm.FolderId.HasValue)
            {
                var folder = await _context.PersonalNoteFolders
                    .FirstOrDefaultAsync(f => f.Id == vm.FolderId.Value && f.UserId == userId);

                if (folder == null)
                {
                    ModelState.AddModelError("FolderId", "پوشه یافت نشد یا به آن دسترسی ندارید.");
                    ViewBag.Folders = await GetFoldersSelectListAsync(userId, vm.FolderId);
                    return View(vm);
                }
            }

            var note = new PersonalNote
            {
                Title = vm.Title,
                Content = vm.Content,
                Color = vm.Color,
                IsPinned = vm.IsPinned,
                FolderId = vm.FolderId,
                UserId = userId!,
                CreatedAt = DateTime.UtcNow
            };

            _context.PersonalNotes.Add(note);
            await _context.SaveChangesAsync();

            // آپلود فایل‌های پیوست
            if (vm.Attachments != null && vm.Attachments.Any())
            {
                var uploadErrors = new List<string>();
                
                foreach (var file in vm.Attachments)
                {
                    var uploadResult = await _fileUploadService.UploadFileAsync(file, "personal-notes");
                    
                    if (uploadResult.Success)
                    {
                        var attachment = new PersonalNoteAttachment
                        {
                            PersonalNoteId = note.Id,
                            FileName = file.FileName,
                            FilePath = uploadResult.FilePath,
                            FileType = _fileUploadService.GetFileType(file.FileName),
                            FileSize = file.Length,
                            MimeType = file.ContentType,
                            UploadedAt = DateTime.UtcNow
                        };
                        
                        _context.PersonalNoteAttachments.Add(attachment);
                    }
                    else
                    {
                        uploadErrors.Add($"{file.FileName}: {uploadResult.Error}");
                    }
                }
                
                // اگر خطا در آپلود وجود داشت، یادداشت رو حذف کن و خطا برگردون
                if (uploadErrors.Any())
                {
                    _context.PersonalNotes.Remove(note);
                    await _context.SaveChangesAsync();
                    
                    TempData["Error"] = $"خطا در آپلود فایل‌ها:\n{string.Join("\n", uploadErrors)}";
                    return View(vm);
                }
                
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "یادداشت شخصی با موفقیت ایجاد شد.";
            
            // اگر یادداشت در یک پوشه ایجاد شده، به همان پوشه redirect کن
            if (vm.FolderId.HasValue)
            {
                return RedirectToAction(nameof(Index), new { folderId = vm.FolderId.Value });
            }
            
            return RedirectToAction(nameof(Index));
        }

        // 📌 ویرایش یادداشت
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var note = await _context.PersonalNotes
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (note == null)
            {
                TempData["Error"] = "یادداشت یافت نشد یا به آن دسترسی ندارید.";
                return RedirectToAction(nameof(Index));
            }

            ViewBag.ExistingAttachments = note.Attachments;

            var vm = new PersonalNoteEditVm
            {
                Id = note.Id,
                Title = note.Title,
                Content = note.Content,
                Color = note.Color,
                IsPinned = note.IsPinned,
                FolderId = note.FolderId
            };

            // لیست پوشه‌های موجود برای انتخاب
            ViewBag.Folders = await GetFoldersSelectListAsync(userId, note.FolderId);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PersonalNoteEditVm vm)
        {
            if (!ModelState.IsValid)
            {
                var userIdForView = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var existingNote = await _context.PersonalNotes.Include(n => n.Attachments).FirstOrDefaultAsync(n => n.Id == vm.Id);
                ViewBag.ExistingAttachments = existingNote?.Attachments;
                ViewBag.Folders = await GetFoldersSelectListAsync(userIdForView, vm.FolderId);
                return View(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var note = await _context.PersonalNotes
                .FirstOrDefaultAsync(n => n.Id == vm.Id && n.UserId == userId);

            if (note == null)
            {
                TempData["Error"] = "یادداشت یافت نشد یا به آن دسترسی ندارید.";
                return RedirectToAction(nameof(Index));
            }

            // بررسی اینکه اگر FolderId مشخص شده، متعلق به کاربر باشد
            if (vm.FolderId.HasValue)
            {
                var folder = await _context.PersonalNoteFolders
                    .FirstOrDefaultAsync(f => f.Id == vm.FolderId.Value && f.UserId == userId);

                if (folder == null)
                {
                    ModelState.AddModelError("FolderId", "پوشه یافت نشد یا به آن دسترسی ندارید.");
                    ViewBag.ExistingAttachments = note.Attachments;
                    ViewBag.Folders = await GetFoldersSelectListAsync(userId, vm.FolderId);
                    return View(vm);
                }
            }

            note.Title = vm.Title;
            note.Content = vm.Content;
            note.Color = vm.Color;
            note.IsPinned = vm.IsPinned;
            note.FolderId = vm.FolderId;
            note.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // حذف فایل‌های انتخاب شده
            if (vm.DeletedAttachmentIds != null && vm.DeletedAttachmentIds.Any())
            {
                var attachmentsToDelete = await _context.PersonalNoteAttachments
                    .Where(a => vm.DeletedAttachmentIds.Contains(a.Id) && a.PersonalNoteId == vm.Id)
                    .ToListAsync();

                foreach (var attachment in attachmentsToDelete)
                {
                    _fileUploadService.DeleteFile(attachment.FilePath);
                    _context.PersonalNoteAttachments.Remove(attachment);
                }
                
                await _context.SaveChangesAsync();
            }

            // آپلود فایل‌های جدید
            if (vm.NewAttachments != null && vm.NewAttachments.Any())
            {
                var uploadErrors = new List<string>();
                
                foreach (var file in vm.NewAttachments)
                {
                    var uploadResult = await _fileUploadService.UploadFileAsync(file, "personal-notes");
                    
                    if (uploadResult.Success)
                    {
                        var attachment = new PersonalNoteAttachment
                        {
                            PersonalNoteId = note.Id,
                            FileName = file.FileName,
                            FilePath = uploadResult.FilePath,
                            FileType = _fileUploadService.GetFileType(file.FileName),
                            FileSize = file.Length,
                            MimeType = file.ContentType,
                            UploadedAt = DateTime.UtcNow
                        };
                        
                        _context.PersonalNoteAttachments.Add(attachment);
                    }
                    else
                    {
                        uploadErrors.Add($"{file.FileName}: {uploadResult.Error}");
                    }
                }
                
                // اگر خطا در آپلود وجود داشت، خطا برگردون
                if (uploadErrors.Any())
                {
                    TempData["Error"] = $"خطا در آپلود فایل‌ها:\n{string.Join("\n", uploadErrors)}";
                    var existingNote = await _context.PersonalNotes.Include(n => n.Attachments).FirstOrDefaultAsync(n => n.Id == vm.Id);
                    ViewBag.ExistingAttachments = existingNote?.Attachments;
                    return View(vm);
                }
                
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "یادداشت شخصی با موفقیت ویرایش شد.";
            return RedirectToAction(nameof(Index), new { folderId = note.FolderId });
        }

        // Helper: محاسبه تمام IDهای پوشه‌های موجود در درخت یک پوشه
        private HashSet<int> GetFolderTreeIds(int folderId, List<PersonalNoteFolder> allFolders)
        {
            var result = new HashSet<int> { folderId };
            var stack = new Stack<int>();
            stack.Push(folderId);

            while (stack.Count > 0)
            {
                var currentId = stack.Pop();
                var children = allFolders
                    .Where(f => f.ParentFolderId == currentId)
                    .Select(f => f.Id);

                foreach (var childId in children)
                {
                    if (result.Add(childId))
                    {
                        stack.Push(childId);
                    }
                }
            }

            return result;
        }

        // Helper: لیست پوشه‌ها برای SelectList
        private async Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetFoldersSelectListAsync(string userId, int? selectedFolderId = null)
        {
            var folders = await _context.PersonalNoteFolders
                .Where(f => f.UserId == userId)
                .OrderBy(f => f.Name)
                .ToListAsync();

            var selectList = new List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>
            {
                new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem 
                { 
                    Text = "بدون پوشه", 
                    Value = "", 
                    Selected = !selectedFolderId.HasValue 
                }
            };

            foreach (var folder in folders)
            {
                selectList.Add(new Microsoft.AspNetCore.Mvc.Rendering.SelectListItem
                {
                    Text = folder.Name,
                    Value = folder.Id.ToString(),
                    Selected = selectedFolderId.HasValue && folder.Id == selectedFolderId.Value
                });
            }

            return selectList;
        }

        // 📌 حذف یادداشت
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var note = await _context.PersonalNotes
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (note == null)
            {
                TempData["Error"] = "یادداشت یافت نشد یا به آن دسترسی ندارید.";
                return RedirectToAction(nameof(Index));
            }

            return View(note);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var note = await _context.PersonalNotes
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (note == null)
            {
                TempData["Error"] = "یادداشت یافت نشد یا به آن دسترسی ندارید.";
                return RedirectToAction(nameof(Index));
            }

            var returnFolderId = note.FolderId;

            // حذف فایل‌های پیوست
            foreach (var attachment in note.Attachments)
            {
                _fileUploadService.DeleteFile(attachment.FilePath);
            }

            _context.PersonalNotes.Remove(note);
            await _context.SaveChangesAsync();

            TempData["Success"] = "یادداشت شخصی با موفقیت حذف شد.";
            return RedirectToAction(nameof(Index), new { folderId = returnFolderId });
        }

        // 📌 تغییر وضعیت پین یادداشت
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePin(int id, int? folderId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var note = await _context.PersonalNotes
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (note == null)
            {
                return Json(new { success = false, message = "یادداشت یافت نشد." });
            }

            note.IsPinned = !note.IsPinned;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index), new { folderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateContent([FromBody] PersonalNoteContentUpdateVm vm)
        {
            if (vm == null || vm.NoteId <= 0)
            {
                return BadRequest(new { success = false, message = "اطلاعات نامعتبر است." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var note = await _context.PersonalNotes
                .FirstOrDefaultAsync(n => n.Id == vm.NoteId && n.UserId == userId);

            if (note == null)
            {
                return NotFound(new { success = false, message = "یادداشت یافت نشد." });
            }

            note.Content = vm.Content;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // 📌 جابجایی یادداشت به پوشه دیگر
        [HttpGet]
        public async Task<IActionResult> MoveToFolder(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var note = await _context.PersonalNotes
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId);

            if (note == null)
            {
                TempData["Error"] = "یادداشت یافت نشد یا به آن دسترسی ندارید.";
                return RedirectToAction(nameof(Index));
            }

            var vm = new PersonalNoteMoveVm
            {
                NoteId = note.Id,
                TargetFolderId = note.FolderId
            };

            // لیست پوشه‌های موجود برای انتخاب
            ViewBag.Folders = await GetFoldersSelectListAsync(userId, note.FolderId);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveToFolder(PersonalNoteMoveVm vm)
        {
            if (!ModelState.IsValid)
            {
                var userIdForView = User.FindFirstValue(ClaimTypes.NameIdentifier);
                ViewBag.Folders = await GetFoldersSelectListAsync(userIdForView, vm.TargetFolderId);
                return View(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var noteToMove = await _context.PersonalNotes
                .FirstOrDefaultAsync(n => n.Id == vm.NoteId && n.UserId == userId);

            if (noteToMove == null)
            {
                TempData["Error"] = "یادداشت یافت نشد یا به آن دسترسی ندارید.";
                return RedirectToAction(nameof(Index));
            }

            // بررسی اینکه اگر TargetFolderId مشخص شده، متعلق به کاربر باشد
            if (vm.TargetFolderId.HasValue)
            {
                var folder = await _context.PersonalNoteFolders
                    .FirstOrDefaultAsync(f => f.Id == vm.TargetFolderId.Value && f.UserId == userId);

                if (folder == null)
                {
                    ModelState.AddModelError("TargetFolderId", "پوشه یافت نشد یا به آن دسترسی ندارید.");
                    ViewBag.Folders = await GetFoldersSelectListAsync(userId, vm.TargetFolderId);
                    return View(vm);
                }
            }

            noteToMove.FolderId = vm.TargetFolderId;
            noteToMove.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "یادداشت با موفقیت جابجا شد.";
            return RedirectToAction(nameof(Index), new { folderId = noteToMove.FolderId });
        }
    }
}


