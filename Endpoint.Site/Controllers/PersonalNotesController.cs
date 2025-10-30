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
        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var notes = await _context.PersonalNotes
                .Where(n => n.UserId == userId)
                .OrderByDescending(n => n.IsPinned)
                .ThenByDescending(n => n.CreatedAt)
                .ToListAsync();

            return View(notes);
        }

        // 📌 جزئیات یادداشت
        [HttpGet("{id}")]
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
        public IActionResult Create()
        {
            return View(new PersonalNoteCreateVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(PersonalNoteCreateVm vm)
        {
            if (!ModelState.IsValid)
            {
                return View(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var note = new PersonalNote
            {
                Title = vm.Title,
                Content = vm.Content,
                Color = vm.Color,
                IsPinned = vm.IsPinned,
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
            return RedirectToAction(nameof(Index));
        }

        // 📌 ویرایش یادداشت
        [HttpGet("{id}")]
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
                IsPinned = note.IsPinned
            };

            return View(vm);
        }

        [HttpPost("{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(PersonalNoteEditVm vm)
        {
            if (!ModelState.IsValid)
            {
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

            note.Title = vm.Title;
            note.Content = vm.Content;
            note.Color = vm.Color;
            note.IsPinned = vm.IsPinned;
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
            return RedirectToAction(nameof(Index));
        }

        // 📌 حذف یادداشت
        [HttpGet("{id}")]
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

            // حذف فایل‌های پیوست
            foreach (var attachment in note.Attachments)
            {
                _fileUploadService.DeleteFile(attachment.FilePath);
            }

            _context.PersonalNotes.Remove(note);
            await _context.SaveChangesAsync();

            TempData["Success"] = "یادداشت شخصی با موفقیت حذف شد.";
            return RedirectToAction(nameof(Index));
        }

        // 📌 تغییر وضعیت پین یادداشت
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePin(int id)
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
            return RedirectToAction(nameof(Index));
        }
    }
}


