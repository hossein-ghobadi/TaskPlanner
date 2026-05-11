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
    public class ProjectNotesController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IFileUploadService _fileUploadService;

        public ProjectNotesController(MVPTestDatabaseContext context, UserManager<User> userManager, IFileUploadService fileUploadService)
        {
            _context = context;
            _userManager = userManager;
            _fileUploadService = fileUploadService;
        }

        // 📌 لیست یادداشت‌های یک پروژه
        [HttpGet]
        public async Task<IActionResult> Index(int projectId, int? folderId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی کاربر به پروژه
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == projectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            // اگر folderId مشخص شده، بررسی کن که متعلق به همین پروژه باشد
            if (folderId.HasValue)
            {
                var folder = await _context.ProjectNoteFolders
                    .FirstOrDefaultAsync(f => f.Id == folderId.Value && f.ProjectId == projectId);

                if (folder == null)
                {
                    TempData["Error"] = "پوشه یافت نشد یا متعلق به این پروژه نیست.";
                    return RedirectToAction(nameof(Index), new { projectId });
                }

                ViewBag.CurrentFolder = folder;
            }

            var project = await _context.Projects.FindAsync(projectId);
            ViewBag.ProjectName = project?.Name;
            ViewBag.ProjectId = projectId;

            // دریافت تمام پوشه‌های پروژه برای محاسبه تعداد یادداشت‌ها
            var allFolders = await _context.ProjectNoteFolders
                .Where(f => f.ProjectId == projectId)
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
                var totalNotes = await _context.ProjectNotes
                    .Where(n => n.ProjectId == projectId && n.FolderId.HasValue && folderIdsInTree.Contains(n.FolderId.Value))
                    .CountAsync();
                folderNotesCount[folder.Id] = totalNotes;
            }

            var notes = await _context.ProjectNotes
                .Include(n => n.Attachments)
                .Include(n => n.Folder)
                .Where(n => n.ProjectId == projectId && n.FolderId == folderId)
                .OrderByDescending(n => n.CreatedAt)
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
            var note = await _context.ProjectNotes
                .Include(n => n.Project)
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null)
                return NotFound();
            if (!note.ProjectId.HasValue)
            {
                TempData["Error"] = "این یادداشت هنوز به پروژه‌ای متصل نیست.";
                return RedirectToAction("Index", "Projects");
            }
            if (!note.ProjectId.HasValue)
            {
                TempData["Error"] = "این یادداشت هنوز به پروژه‌ای متصل نیست.";
                return RedirectToAction("Index", "Projects");
            }
            if (!note.ProjectId.HasValue)
            {
                TempData["Error"] = "این یادداشت هنوز به پروژه‌ای متصل نیست.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی دسترسی
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == note.ProjectId.Value && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این یادداشت دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            return View(note);
        }

        // 📌 ایجاد یادداشت جدید
        [HttpGet]
        public async Task<IActionResult> Create(int projectId, int? folderId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی به پروژه
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == projectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            // اگر folderId مشخص شده، بررسی کن که متعلق به پروژه باشد
            if (folderId.HasValue)
            {
                var folder = await _context.ProjectNoteFolders
                    .FirstOrDefaultAsync(f => f.Id == folderId.Value && f.ProjectId == projectId);

                if (folder == null)
                {
                    TempData["Error"] = "پوشه یافت نشد یا به آن دسترسی ندارید.";
                    return RedirectToAction(nameof(Index), new { projectId = projectId });
                }
            }

            var project = await _context.Projects.FindAsync(projectId);
            ViewBag.ProjectName = project?.Name;

            var vm = new ProjectNoteCreateVm 
            { 
                ProjectId = projectId,
                FolderId = folderId
            };

            // لیست پوشه‌های موجود برای انتخاب
            ViewBag.Folders = await GetFoldersSelectListAsync(projectId, userId, folderId);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectNoteCreateVm vm)
        {
            // بررسی مستقیم Request.Form برای FolderId
            // این کار برای حل مشکل Model Binding با select value="" است
            if (Request.Form.ContainsKey("FolderId"))
            {
                var folderIdValue = Request.Form["FolderId"].ToString();
                if (string.IsNullOrWhiteSpace(folderIdValue) || folderIdValue == "0")
                {
                    vm.FolderId = null;
                }
                else if (int.TryParse(folderIdValue, out int folderId))
                {
                    vm.FolderId = folderId;
                }
            }
            else
            {
                vm.FolderId = null;
            }

            if (!ModelState.IsValid)
            {
                var project = await _context.Projects.FindAsync(vm.ProjectId);
                ViewBag.ProjectName = project?.Name;
                ViewBag.Folders = await GetFoldersSelectListAsync(vm.ProjectId, User.FindFirstValue(ClaimTypes.NameIdentifier), vm.FolderId);
                return View(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == vm.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی اینکه اگر FolderId مشخص شده، متعلق به همین پروژه باشد
            if (vm.FolderId.HasValue)
            {
                var folder = await _context.ProjectNoteFolders
                    .FirstOrDefaultAsync(f => f.Id == vm.FolderId.Value && f.ProjectId == vm.ProjectId);

                if (folder == null)
                {
                    ModelState.AddModelError("FolderId", "پوشه یافت نشد یا متعلق به این پروژه نیست.");
                    var project = await _context.Projects.FindAsync(vm.ProjectId);
                    ViewBag.ProjectName = project?.Name;
                    ViewBag.Folders = await GetFoldersSelectListAsync(vm.ProjectId, userId, vm.FolderId);
                    return View(vm);
                }
            }

            var note = new ProjectNote
            {
                Title = vm.Title,
                Content = vm.Content,
                ProjectId = vm.ProjectId,
                FolderId = vm.FolderId,
                CreatorUserId = userId!,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProjectNotes.Add(note);
            await _context.SaveChangesAsync();

            // آپلود فایل‌های پیوست (fallback به Request.Form.Files برای پایداری بیشتر model binding)
            var createFilesToUpload = (vm.Attachments ?? new List<IFormFile>())
                .Concat(Request.Form?.Files?.Where(f => f.Length > 0) ?? Enumerable.Empty<IFormFile>())
                .GroupBy(f => new { f.FileName, f.Length, f.ContentType })
                .Select(g => g.First())
                .ToList();

            if (createFilesToUpload.Any())
            {
                var uploadErrors = new List<string>();
                
                foreach (var file in createFilesToUpload)
                {
                    var uploadResult = await _fileUploadService.UploadFileAsync(file, "project-notes");
                    
                    if (uploadResult.Success)
                    {
                        var attachment = new ProjectNoteAttachment
                        {
                            ProjectNoteId = note.Id,
                            FileName = file.FileName,
                            FilePath = uploadResult.FilePath,
                            FileType = _fileUploadService.GetFileType(file.FileName),
                            FileSize = file.Length,
                            MimeType = file.ContentType,
                            UploadedAt = DateTime.UtcNow
                        };
                        
                        _context.ProjectNoteAttachments.Add(attachment);
                    }
                    else
                    {
                        uploadErrors.Add($"{file.FileName}: {uploadResult.Error}");
                    }
                }
                
                // اگر خطا در آپلود وجود داشت، یادداشت رو حذف کن و خطا برگردون
                if (uploadErrors.Any())
                {
                    _context.ProjectNotes.Remove(note);
                    await _context.SaveChangesAsync();
                    
                    TempData["Error"] = $"خطا در آپلود فایل‌ها:\n{string.Join("\n", uploadErrors)}";
                    var project = await _context.Projects.FindAsync(vm.ProjectId);
                    ViewBag.ProjectName = project?.Name;
                    ViewBag.Folders = await GetFoldersSelectListAsync(vm.ProjectId, userId, vm.FolderId);
                    return View(vm);
                }
                
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "یادداشت با موفقیت ایجاد شد.";
            
            // اگر یادداشت در یک پوشه ایجاد شده، به همان پوشه redirect کن
            if (vm.FolderId.HasValue)
            {
                return RedirectToAction(nameof(Index), new { projectId = vm.ProjectId, folderId = vm.FolderId.Value });
            }
            
            return RedirectToAction(nameof(Index), new { projectId = vm.ProjectId });
        }

        // 📌 ویرایش یادداشت
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var note = await _context.ProjectNotes
                .Include(n => n.Project)
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null)
                return NotFound();
            if (!note.ProjectId.HasValue)
            {
                TempData["Error"] = "این یادداشت هنوز به پروژه‌ای متصل نیست.";
                return RedirectToAction("Index", "Projects");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == note.ProjectId.Value && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این یادداشت دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            if (!note.ProjectId.HasValue)
            {
                TempData["Error"] = "این یادداشت هنوز به پروژه‌ای متصل نیست.";
                return RedirectToAction("Index", "Projects");
            }

            ViewBag.ProjectName = note.Project?.Name;
            ViewBag.ExistingAttachments = note.Attachments;

            var vm = new ProjectNoteEditVm
            {
                Id = note.Id,
                Title = note.Title,
                Content = note.Content,
                ProjectId = note.ProjectId.Value,
                FolderId = note.FolderId
            };

            // لیست پوشه‌های موجود برای انتخاب
            ViewBag.Folders = await GetFoldersSelectListAsync(note.ProjectId.Value, userId, note.FolderId);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProjectNoteEditVm vm)
        {
            if (!ModelState.IsValid)
            {
                var project = await _context.Projects.FindAsync(vm.ProjectId);
                ViewBag.ProjectName = project?.Name;
                var existingNote = await _context.ProjectNotes.Include(n => n.Attachments).FirstOrDefaultAsync(n => n.Id == vm.Id);
                ViewBag.ExistingAttachments = existingNote?.Attachments;
                ViewBag.Folders = await GetFoldersSelectListAsync(vm.ProjectId, User.FindFirstValue(ClaimTypes.NameIdentifier), vm.FolderId);
                return View(vm);
            }

            var note = await _context.ProjectNotes.FindAsync(vm.Id);
            if (note == null)
                return NotFound();
            if (!note.ProjectId.HasValue)
            {
                TempData["Error"] = "این یادداشت هنوز به پروژه‌ای متصل نیست.";
                return RedirectToAction("Index", "Projects");
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == note.ProjectId.Value && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این یادداشت دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی اینکه اگر FolderId مشخص شده، متعلق به همین پروژه باشد
            if (vm.FolderId.HasValue)
            {
                var folder = await _context.ProjectNoteFolders
                    .FirstOrDefaultAsync(f => f.Id == vm.FolderId.Value && f.ProjectId == vm.ProjectId);

                if (folder == null)
                {
                    ModelState.AddModelError("FolderId", "پوشه یافت نشد یا متعلق به این پروژه نیست.");
                    ViewBag.ExistingAttachments = note.Attachments;
                    ViewBag.Folders = await GetFoldersSelectListAsync(vm.ProjectId, userId, vm.FolderId);
                    var project = await _context.Projects.FindAsync(vm.ProjectId);
                    ViewBag.ProjectName = project?.Name;
                    return View(vm);
                }
            }

            note.Title = vm.Title;
            note.Content = vm.Content;
            note.FolderId = vm.FolderId;
            note.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            // حذف فایل‌های انتخاب شده
            if (vm.DeletedAttachmentIds != null && vm.DeletedAttachmentIds.Any())
            {
                var attachmentsToDelete = await _context.ProjectNoteAttachments
                    .Where(a => vm.DeletedAttachmentIds.Contains(a.Id) && a.ProjectNoteId == vm.Id)
                    .ToListAsync();

                foreach (var attachment in attachmentsToDelete)
                {
                    _fileUploadService.DeleteFile(attachment.FilePath);
                    _context.ProjectNoteAttachments.Remove(attachment);
                }
                
                await _context.SaveChangesAsync();
            }

            // آپلود فایل‌های جدید (fallback به Request.Form.Files برای پایداری بیشتر model binding)
            var editFilesToUpload = (vm.NewAttachments ?? new List<IFormFile>())
                .Concat(Request.Form?.Files?.Where(f => f.Length > 0) ?? Enumerable.Empty<IFormFile>())
                .GroupBy(f => new { f.FileName, f.Length, f.ContentType })
                .Select(g => g.First())
                .ToList();

            if (editFilesToUpload.Any())
            {
                var uploadErrors = new List<string>();
                
                foreach (var file in editFilesToUpload)
                {
                    var uploadResult = await _fileUploadService.UploadFileAsync(file, "project-notes");
                    
                    if (uploadResult.Success)
                    {
                        var attachment = new ProjectNoteAttachment
                        {
                            ProjectNoteId = note.Id,
                            FileName = file.FileName,
                            FilePath = uploadResult.FilePath,
                            FileType = _fileUploadService.GetFileType(file.FileName),
                            FileSize = file.Length,
                            MimeType = file.ContentType,
                            UploadedAt = DateTime.UtcNow
                        };
                        
                        _context.ProjectNoteAttachments.Add(attachment);
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
                    var project = await _context.Projects.FindAsync(vm.ProjectId);
                    ViewBag.ProjectName = project?.Name;
                    var existingNote = await _context.ProjectNotes.Include(n => n.Attachments).FirstOrDefaultAsync(n => n.Id == vm.Id);
                    ViewBag.ExistingAttachments = existingNote?.Attachments;
                    return View(vm);
                }
                
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "یادداشت با موفقیت ویرایش شد.";
            return RedirectToAction(nameof(Index), new { projectId = note.ProjectId, folderId = note.FolderId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateContent([FromBody] ProjectNoteContentUpdateVm vm)
        {
            if (vm == null || vm.NoteId <= 0)
            {
                return BadRequest(new { success = false, message = "اطلاعات نامعتبر است." });
            }

            var note = await _context.ProjectNotes
                .FirstOrDefaultAsync(n => n.Id == vm.NoteId);
            if (note == null)
            {
                return NotFound(new { success = false, message = "یادداشت یافت نشد." });
            }

            if (!note.ProjectId.HasValue)
            {
                return BadRequest(new { success = false, message = "یادداشت به پروژه متصل نیست." });
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == note.ProjectId.Value &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                return Unauthorized(new { success = false, message = "دسترسی ندارید." });
            }

            note.Content = vm.Content;
            note.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return Json(new { success = true });
        }

        // Helper: محاسبه تمام IDهای پوشه‌های موجود در درخت یک پوشه
        private HashSet<int> GetFolderTreeIds(int folderId, List<ProjectNoteFolder> allFolders)
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
        private async Task<List<Microsoft.AspNetCore.Mvc.Rendering.SelectListItem>> GetFoldersSelectListAsync(int projectId, string userId, int? selectedFolderId = null)
        {
            var folders = await _context.ProjectNoteFolders
                .Where(f => f.ProjectId == projectId)
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
            var note = await _context.ProjectNotes
                .Include(n => n.Project)
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == note.ProjectId.Value && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این یادداشت دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            return View(note);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var note = await _context.ProjectNotes
                .Include(n => n.Attachments)
                .FirstOrDefaultAsync(n => n.Id == id);
            
            if (note == null)
                return NotFound();

            var projectId = note.ProjectId;
            var returnFolderId = note.FolderId;

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == projectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این یادداشت دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            // حذف فایل‌های پیوست
            foreach (var attachment in note.Attachments)
            {
                _fileUploadService.DeleteFile(attachment.FilePath);
            }

            _context.ProjectNotes.Remove(note);
            await _context.SaveChangesAsync();

            TempData["Success"] = "یادداشت با موفقیت حذف شد.";
            return RedirectToAction(nameof(Index), new { projectId = projectId!.Value, folderId = returnFolderId });
        }
        
        // 🗑️ حذف یک فایل پیوست (Ajax)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteAttachment(int attachmentId)
        {
            var attachment = await _context.ProjectNoteAttachments
                .Include(a => a.ProjectNote)
                .FirstOrDefaultAsync(a => a.Id == attachmentId);

            if (attachment == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == attachment.ProjectNote.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
                return Unauthorized();

            _fileUploadService.DeleteFile(attachment.FilePath);
            _context.ProjectNoteAttachments.Remove(attachment);
            await _context.SaveChangesAsync();

            return Ok();
        }

        // 📌 جابجایی یادداشت به پوشه دیگر
        [HttpGet]
        public async Task<IActionResult> MoveToFolder(int id)
        {
            var note = await _context.ProjectNotes
                .Include(n => n.Project)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == note.ProjectId.Value && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این یادداشت دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            var vm = new ProjectNoteMoveVm
            {
                NoteId = note.Id,
                ProjectId = note.ProjectId.Value,
                TargetFolderId = note.FolderId
            };

            // لیست پوشه‌های موجود برای انتخاب
            ViewBag.Folders = await GetFoldersSelectListAsync(note.ProjectId.Value, userId, note.FolderId);
            ViewBag.ProjectName = note.Project?.Name;

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MoveToFolder(ProjectNoteMoveVm vm)
        {
            if (!ModelState.IsValid)
            {
                var note = await _context.ProjectNotes
                    .Include(n => n.Project)
                    .FirstOrDefaultAsync(n => n.Id == vm.NoteId);
                
                if (note != null)
                {
                    ViewBag.Folders = await GetFoldersSelectListAsync(vm.ProjectId, User.FindFirstValue(ClaimTypes.NameIdentifier), vm.TargetFolderId);
                    ViewBag.ProjectName = note.Project?.Name;
                }
                return View(vm);
            }

            var noteToMove = await _context.ProjectNotes.FindAsync(vm.NoteId);
            if (noteToMove == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == noteToMove.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این یادداشت دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی اینکه اگر TargetFolderId مشخص شده، متعلق به همین پروژه باشد
            if (vm.TargetFolderId.HasValue)
            {
                var folder = await _context.ProjectNoteFolders
                    .FirstOrDefaultAsync(f => f.Id == vm.TargetFolderId.Value && f.ProjectId == vm.ProjectId);

                if (folder == null)
                {
                    ModelState.AddModelError("TargetFolderId", "پوشه یافت نشد یا متعلق به این پروژه نیست.");
                    ViewBag.Folders = await GetFoldersSelectListAsync(vm.ProjectId, userId, vm.TargetFolderId);
                    var note = await _context.ProjectNotes.Include(n => n.Project).FirstOrDefaultAsync(n => n.Id == vm.NoteId);
                    ViewBag.ProjectName = note?.Project?.Name;
                    return View(vm);
                }
            }

            noteToMove.FolderId = vm.TargetFolderId;
            noteToMove.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "یادداشت با موفقیت جابجا شد.";
            return RedirectToAction(nameof(Index), new { projectId = noteToMove.ProjectId, folderId = noteToMove.FolderId });
        }
    }
}


