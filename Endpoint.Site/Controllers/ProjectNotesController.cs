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
        public async Task<IActionResult> Index(int projectId)
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

            var project = await _context.Projects.FindAsync(projectId);
            ViewBag.ProjectName = project?.Name;
            ViewBag.ProjectId = projectId;

            var notes = await _context.ProjectNotes
                .Include(n => n.Attachments) // 📎 اضافه شد برای نمایش فایل‌ها در لیست
                .Where(n => n.ProjectId == projectId)
                .OrderByDescending(n => n.CreatedAt)
                .ToListAsync();

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

            // بررسی دسترسی
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == note.ProjectId && 
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
        public async Task<IActionResult> Create(int projectId)
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

            var project = await _context.Projects.FindAsync(projectId);
            ViewBag.ProjectName = project?.Name;

            return View(new ProjectNoteCreateVm { ProjectId = projectId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectNoteCreateVm vm)
        {
            if (!ModelState.IsValid)
            {
                var project = await _context.Projects.FindAsync(vm.ProjectId);
                ViewBag.ProjectName = project?.Name;
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

            var note = new ProjectNote
            {
                Title = vm.Title,
                Content = vm.Content,
                ProjectId = vm.ProjectId,
                CreatorUserId = userId!,
                CreatedAt = DateTime.UtcNow
            };

            _context.ProjectNotes.Add(note);
            await _context.SaveChangesAsync();

            // آپلود فایل‌های پیوست
            if (vm.Attachments != null && vm.Attachments.Any())
            {
                var uploadErrors = new List<string>();
                
                foreach (var file in vm.Attachments)
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
                    return View(vm);
                }
                
                await _context.SaveChangesAsync();
            }

            TempData["Success"] = "یادداشت با موفقیت ایجاد شد.";
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

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == note.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این یادداشت دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            ViewBag.ProjectName = note.Project?.Name;
            ViewBag.ExistingAttachments = note.Attachments;

            var vm = new ProjectNoteEditVm
            {
                Id = note.Id,
                Title = note.Title,
                Content = note.Content,
                ProjectId = note.ProjectId
            };

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
                return View(vm);
            }

            var note = await _context.ProjectNotes.FindAsync(vm.Id);
            if (note == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == note.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این یادداشت دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            note.Title = vm.Title;
            note.Content = vm.Content;
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

            // آپلود فایل‌های جدید
            if (vm.NewAttachments != null && vm.NewAttachments.Any())
            {
                var uploadErrors = new List<string>();
                
                foreach (var file in vm.NewAttachments)
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
            return RedirectToAction(nameof(Index), new { projectId = note.ProjectId });
        }

        // 📌 حذف یادداشت
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var note = await _context.ProjectNotes
                .Include(n => n.Project)
                .FirstOrDefaultAsync(n => n.Id == id);

            if (note == null)
                return NotFound();

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == note.ProjectId && 
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
            return RedirectToAction(nameof(Index), new { projectId });
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
    }
}


