using Endpoint.Site.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using System.IO.Compression;
using System.Security.Claims;
using TaskPlanner.Application.Services.FileUpload;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Domain.Entities.Users;
using TaskPlanner.Persistence.Contexts;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class ProjectImageGalleriesController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IFileUploadService _fileUploadService;

        public ProjectImageGalleriesController(
            MVPTestDatabaseContext context, 
            UserManager<User> userManager, 
            IFileUploadService fileUploadService)
        {
            _context = context;
            _userManager = userManager;
            _fileUploadService = fileUploadService;
        }

        /// <summary>
        /// لیست عکس‌های گالری یک پروژه
        /// </summary>
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
                var folder = await _context.ProjectImageGalleryFolders
                    .FirstOrDefaultAsync(f => f.Id == folderId.Value && f.ProjectId == projectId);

                if (folder == null)
                {
                    TempData["Error"] = "پوشه یافت نشد یا متعلق به این پروژه نیست.";
                    return RedirectToAction(nameof(Index), new { projectId });
                }

                ViewBag.CurrentFolder = folder;

                // ساخت مسیر breadcrumb از root تا والد پوشه فعلی (برای برگشت به پوشه‌های بالاتر)
                var breadcrumbPath = new List<ProjectImageGalleryFolder>();
                var parentId = folder.ParentFolderId;
                while (parentId.HasValue)
                {
                    var parentFolder = await _context.ProjectImageGalleryFolders
                        .FirstOrDefaultAsync(f => f.Id == parentId.Value && f.ProjectId == projectId);
                    if (parentFolder == null) break;
                    breadcrumbPath.Insert(0, parentFolder);
                    parentId = parentFolder.ParentFolderId;
                }
                ViewBag.BreadcrumbFolders = breadcrumbPath;
            }

            var project = await _context.Projects.FindAsync(projectId);
            ViewBag.ProjectName = project?.Name;
            ViewBag.ProjectId = projectId;

            // دریافت تمام پوشه‌های پروژه برای محاسبه تعداد عکس‌ها
            var allFolders = await _context.ProjectImageGalleryFolders
                .Where(f => f.ProjectId == projectId)
                .ToListAsync();

            // دریافت پوشه‌های سطح اول
            var folders = allFolders
                .Where(f => f.ParentFolderId == folderId)
                .OrderBy(f => f.Name)
                .ToList();

            // محاسبه تعداد کل عکس‌ها برای هر پوشه (شامل زیرپوشه‌ها)
            var folderImagesCount = new Dictionary<int, int>();
            foreach (var folder in allFolders)
            {
                var folderIdsInTree = GetFolderTreeIds(folder.Id, allFolders);
                var totalImages = await _context.ProjectImageGalleries
                    .Where(img => img.ProjectId == projectId && img.FolderId.HasValue && folderIdsInTree.Contains(img.FolderId.Value))
                    .CountAsync();
                folderImagesCount[folder.Id] = totalImages;
            }

            var images = await _context.ProjectImageGalleries
                .Include(img => img.Folder)
                .Where(img => img.ProjectId == projectId && img.FolderId == folderId)
                .OrderByDescending(img => img.CreatedAt)
                .ToListAsync();

            ViewBag.Folders = folders;
            ViewBag.FolderId = folderId;
            ViewBag.FolderImagesCount = folderImagesCount;

            return View(images);
        }

        /// <summary>
        /// جزئیات یک عکس
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var image = await _context.ProjectImageGalleries
                .Include(img => img.Project)
                .Include(img => img.Folder)
                .FirstOrDefaultAsync(img => img.Id == id);

            if (image == null)
                return NotFound();

            // بررسی دسترسی
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == image.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این عکس دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            return View(image);
        }

        /// <summary>
        /// ایجاد عکس جدید در گالری
        /// </summary>
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
                var folder = await _context.ProjectImageGalleryFolders
                    .FirstOrDefaultAsync(f => f.Id == folderId.Value && f.ProjectId == projectId);

                if (folder == null)
                {
                    TempData["Error"] = "پوشه یافت نشد یا به آن دسترسی ندارید.";
                    return RedirectToAction(nameof(Index), new { projectId = projectId });
                }
            }

            var project = await _context.Projects.FindAsync(projectId);
            ViewBag.ProjectName = project?.Name;

            var vm = new ProjectImageGalleryCreateVm 
            { 
                ProjectId = projectId,
                FolderId = folderId
            };

            // اگر folderId مشخص شده، اطلاعات پوشه را برای نمایش بگیر
            if (folderId.HasValue)
            {
                var currentFolder = await _context.ProjectImageGalleryFolders
                    .FirstOrDefaultAsync(f => f.Id == folderId.Value && f.ProjectId == projectId);
                ViewBag.CurrentFolder = currentFolder;
            }

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(ProjectImageGalleryCreateVm vm)
        {
            // دریافت فایل‌ها از Request.Form
            if (Request.Form.Files != null && Request.Form.Files.Any())
            {
                vm.ImageFiles = Request.Form.Files.Where(f => f.Length > 0).ToList();
            }

            // بررسی اینکه حداقل یک فایل انتخاب شده باشد
            if (vm.ImageFiles == null || !vm.ImageFiles.Any())
            {
                ModelState.AddModelError("ImageFiles", "لطفاً حداقل یک عکس انتخاب کنید.");
            }

            if (!ModelState.IsValid)
            {
                if (IsAjaxRequest())
                {
                    var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
                    return Json(new { success = false, message = "خطا در اعتبارسنجی", errors });
                }
                var project = await _context.Projects.FindAsync(vm.ProjectId);
                ViewBag.ProjectName = project?.Name;
                if (vm.FolderId.HasValue)
                {
                    var currentFolder = await _context.ProjectImageGalleryFolders
                        .FirstOrDefaultAsync(f => f.Id == vm.FolderId.Value && f.ProjectId == vm.ProjectId);
                    ViewBag.CurrentFolder = currentFolder;
                }
                return View(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            // بررسی دسترسی به پروژه
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == vm.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "شما به این پروژه دسترسی ندارید." });
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی اینکه اگر FolderId مشخص شده، متعلق به پروژه باشد
            if (vm.FolderId.HasValue)
            {
                var folder = await _context.ProjectImageGalleryFolders
                    .FirstOrDefaultAsync(f => f.Id == vm.FolderId.Value && f.ProjectId == vm.ProjectId);

                if (folder == null)
                {
                    if (IsAjaxRequest())
                        return Json(new { success = false, message = "پوشه یافت نشد یا به آن دسترسی ندارید." });
                    ModelState.AddModelError("FolderId", "پوشه یافت نشد یا به آن دسترسی ندارید.");
                    var project = await _context.Projects.FindAsync(vm.ProjectId);
                    ViewBag.ProjectName = project?.Name;
                    if (vm.FolderId.HasValue)
                    {
                        var currentFolder = await _context.ProjectImageGalleryFolders
                            .FirstOrDefaultAsync(f => f.Id == vm.FolderId.Value && f.ProjectId == vm.ProjectId);
                        ViewBag.CurrentFolder = currentFolder;
                    }
                    return View(vm);
                }
            }

            if (vm.ImageFiles == null || !vm.ImageFiles.Any())
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "لطفاً حداقل یک عکس انتخاب کنید." });
                ModelState.AddModelError("ImageFiles", "لطفاً حداقل یک عکس انتخاب کنید.");
                var project = await _context.Projects.FindAsync(vm.ProjectId);
                ViewBag.ProjectName = project?.Name;
                if (vm.FolderId.HasValue)
                {
                    var currentFolder = await _context.ProjectImageGalleryFolders
                        .FirstOrDefaultAsync(f => f.Id == vm.FolderId.Value && f.ProjectId == vm.ProjectId);
                    ViewBag.CurrentFolder = currentFolder;
                }
                return View(vm);
            }

            // آپلود دسته‌ای فایل‌های عکس
            var uploadedImages = new List<ProjectImageGallery>();
            var uploadErrors = new List<string>();
            int successCount = 0;

            foreach (var imageFile in vm.ImageFiles)
            {
                if (imageFile == null || imageFile.Length == 0)
                    continue;

                var uploadResult = await _fileUploadService.UploadFileAsync(imageFile, "project-galleries");
                
                if (!uploadResult.Success)
                {
                    uploadErrors.Add($"{imageFile.FileName}: {uploadResult.Error}");
                    continue;
                }

                // استفاده از نام فایل به عنوان عنوان
                var imageTitle = System.IO.Path.GetFileNameWithoutExtension(imageFile.FileName);

                // اگر چند فایل آپلود می‌شود، شماره اضافه کن
                if (vm.ImageFiles.Count > 1)
                {
                    imageTitle = $"{imageTitle} ({successCount + 1})";
                }

                var image = new ProjectImageGallery
                {
                    Title = imageTitle,
                    Description = null, // توضیحات حذف شده
                    ProjectId = vm.ProjectId,
                    FolderId = vm.FolderId,
                    CreatorUserId = userId!,
                    FileName = imageFile.FileName,
                    FilePath = uploadResult.FilePath,
                    FileSize = imageFile.Length,
                    MimeType = imageFile.ContentType,
                    CreatedAt = DateTime.UtcNow
                };

                uploadedImages.Add(image);
                successCount++;
            }

            if (!uploadedImages.Any())
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "خطا در آپلود فایل‌ها", errors = uploadErrors });
                ModelState.AddModelError("ImageFiles", $"خطا در آپلود فایل‌ها:\n{string.Join("\n", uploadErrors)}");
                var project = await _context.Projects.FindAsync(vm.ProjectId);
                ViewBag.ProjectName = project?.Name;
                if (vm.FolderId.HasValue)
                {
                    var currentFolder = await _context.ProjectImageGalleryFolders
                        .FirstOrDefaultAsync(f => f.Id == vm.FolderId.Value && f.ProjectId == vm.ProjectId);
                    ViewBag.CurrentFolder = currentFolder;
                }
                return View(vm);
            }

            // افزودن تمام عکس‌های آپلود شده به دیتابیس
            _context.ProjectImageGalleries.AddRange(uploadedImages);
            await _context.SaveChangesAsync();

            var successMessage = uploadErrors.Any()
                ? $"{successCount} عکس با موفقیت اضافه شد. {uploadErrors.Count} فایل با خطا مواجه شد."
                : $"{successCount} عکس با موفقیت به گالری اضافه شد.";
            if (uploadErrors.Any())
            {
                TempData["Success"] = successMessage;
                TempData["Error"] = string.Join("\n", uploadErrors);
            }
            else
                TempData["Success"] = successMessage;

            if (IsAjaxRequest())
                return Json(new { success = true, message = successMessage, projectId = vm.ProjectId, folderId = vm.FolderId });
            if (vm.FolderId.HasValue)
                return RedirectToAction(nameof(Index), new { projectId = vm.ProjectId, folderId = vm.FolderId.Value });
            return RedirectToAction(nameof(Index), new { projectId = vm.ProjectId });
        }

        /// <summary>
        /// ویرایش عکس
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var image = await _context.ProjectImageGalleries
                .Include(img => img.Project)
                .FirstOrDefaultAsync(img => img.Id == id);

            if (image == null)
            {
                TempData["Error"] = "عکس یافت نشد.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == image.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این عکس دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            var project = await _context.Projects.FindAsync(image.ProjectId);
            ViewBag.ProjectName = project?.Name;

            var vm = new ProjectImageGalleryEditVm
            {
                Id = image.Id,
                Title = image.Title,
                Description = image.Description,
                ProjectId = image.ProjectId,
                FolderId = image.FolderId,
                CurrentFilePath = image.FilePath
            };

            // لیست پوشه‌های موجود برای انتخاب
            ViewBag.Folders = await GetFoldersSelectListAsync(image.ProjectId, userId, image.FolderId);

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(ProjectImageGalleryEditVm vm)
        {
            // بررسی مستقیم Request.Form برای FolderId
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
                var userIdForView = User.FindFirstValue(ClaimTypes.NameIdentifier);
                var project = await _context.Projects.FindAsync(vm.ProjectId);
                ViewBag.ProjectName = project?.Name;
                ViewBag.Folders = await GetFoldersSelectListAsync(vm.ProjectId, userIdForView, vm.FolderId);
                return View(vm);
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var image = await _context.ProjectImageGalleries
                .FirstOrDefaultAsync(img => img.Id == vm.Id && img.ProjectId == vm.ProjectId);

            if (image == null)
            {
                TempData["Error"] = "عکس یافت نشد یا به آن دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == vm.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی اینکه اگر FolderId مشخص شده، متعلق به پروژه باشد
            if (vm.FolderId.HasValue)
            {
                var folder = await _context.ProjectImageGalleryFolders
                    .FirstOrDefaultAsync(f => f.Id == vm.FolderId.Value && f.ProjectId == vm.ProjectId);

                if (folder == null)
                {
                    ModelState.AddModelError("FolderId", "پوشه یافت نشد یا به آن دسترسی ندارید.");
                    var project = await _context.Projects.FindAsync(vm.ProjectId);
                    ViewBag.ProjectName = project?.Name;
                    ViewBag.Folders = await GetFoldersSelectListAsync(vm.ProjectId, userId, vm.FolderId);
                    return View(vm);
                }
            }

            image.Title = vm.Title;
            image.Description = vm.Description;
            image.FolderId = vm.FolderId;
            image.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            TempData["Success"] = "عکس با موفقیت ویرایش شد.";
            return RedirectToAction(nameof(Index), new { projectId = vm.ProjectId, folderId = image.FolderId });
        }

        /// <summary>
        /// حذف عکس
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> Delete(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var image = await _context.ProjectImageGalleries
                .Include(img => img.Project)
                .FirstOrDefaultAsync(img => img.Id == id);

            if (image == null)
            {
                TempData["Error"] = "عکس یافت نشد.";
                return RedirectToAction("Index", "Projects");
            }

            // بررسی دسترسی
            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == image.ProjectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این عکس دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            return View(image);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var image = await _context.ProjectImageGalleries
                .FirstOrDefaultAsync(img => img.Id == id);

            if (image == null)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "عکس یافت نشد." });
                TempData["Error"] = "عکس یافت نشد.";
                return RedirectToAction("Index", "Projects");
            }

            var projectId = image.ProjectId;

            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == projectId && 
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                if (IsAjaxRequest())
                    return Json(new { success = false, message = "شما به این پروژه دسترسی ندارید." });
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            _fileUploadService.DeleteFile(image.FilePath);
            if (!string.IsNullOrEmpty(image.ThumbnailPath))
                _fileUploadService.DeleteFile(image.ThumbnailPath);

            _context.ProjectImageGalleries.Remove(image);
            await _context.SaveChangesAsync();

            if (IsAjaxRequest())
                return Json(new { success = true, message = "عکس با موفقیت حذف شد.", projectId });
            TempData["Success"] = "عکس با موفقیت حذف شد.";
            return RedirectToAction(nameof(Index), new { projectId });
        }

        /// <summary>
        /// فرم آپلود طراحی فیگما (فایل zip اکسپورت فیگما)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> UploadFigmaDesign(int projectId, int? folderId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == projectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            if (folderId.HasValue)
            {
                var folder = await _context.ProjectImageGalleryFolders
                    .FirstOrDefaultAsync(f => f.Id == folderId.Value && f.ProjectId == projectId);
                if (folder == null)
                {
                    TempData["Error"] = "پوشه یافت نشد یا متعلق به این پروژه نیست.";
                    return RedirectToAction(nameof(Index), new { projectId });
                }
            }

            var project = await _context.Projects.FindAsync(projectId);
            ViewBag.ProjectName = project?.Name;
            ViewBag.ProjectId = projectId;
            ViewBag.FolderId = folderId;
            return View();
        }

        /// <summary>
        /// دریافت فایل zip اکسپورت فیگما، استخراج تصاویر و ذخیره در گالری + نمایش طراحی
        /// </summary>
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadFigmaDesign(int projectId, int? folderId, IFormFile figmaZipFile)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == projectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            if (figmaZipFile == null || figmaZipFile.Length == 0)
            {
                TempData["Error"] = "لطفاً یک فایل zip اکسپورت فیگما انتخاب کنید.";
                return RedirectToAction(nameof(UploadFigmaDesign), new { projectId, folderId });
            }

            var ext = Path.GetExtension(figmaZipFile.FileName).ToLowerInvariant();
            if (ext != ".zip")
            {
                TempData["Error"] = "فقط فایل‌های zip مجاز هستند.";
                return RedirectToAction(nameof(UploadFigmaDesign), new { projectId, folderId });
            }

            var allowedImageExtensions = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
                { ".png", ".jpg", ".jpeg", ".gif", ".webp", ".svg" };

            string tempZipPath = null;
            string tempExtractPath = null;

            try
            {
                tempZipPath = Path.Combine(Path.GetTempPath(), $"figma_{Guid.NewGuid():N}.zip");
                tempExtractPath = Path.Combine(Path.GetTempPath(), $"figma_extract_{Guid.NewGuid():N}");

                using (var stream = new FileStream(tempZipPath, FileMode.Create))
                    await figmaZipFile.CopyToAsync(stream);

                ZipFile.ExtractToDirectory(tempZipPath, tempExtractPath, overwriteFiles: true);

                var imageFiles = Directory
                    .GetFiles(tempExtractPath, "*.*", SearchOption.AllDirectories)
                    .Where(f => allowedImageExtensions.Contains(Path.GetExtension(f)))
                    .OrderBy(f => f)
                    .ToList();

                if (!imageFiles.Any())
                {
                    TempData["Error"] = "در این فایل zip هیچ تصویر مجازی (PNG, JPG, SVG و...) یافت نشد.";
                    return RedirectToAction(nameof(UploadFigmaDesign), new { projectId, folderId });
                }

                var folderName = Path.GetFileNameWithoutExtension(figmaZipFile.FileName);
                int? targetFolderId = folderId;

                if (!folderId.HasValue && !string.IsNullOrWhiteSpace(folderName))
                {
                    var newFolder = new ProjectImageGalleryFolder
                    {
                        Name = folderName,
                        ProjectId = projectId,
                        ParentFolderId = null,
                        CreatorUserId = userId!,
                        CreatedAt = DateTime.UtcNow,
                        Color = "#8B5CF6"
                    };
                    _context.ProjectImageGalleryFolders.Add(newFolder);
                    await _context.SaveChangesAsync();
                    targetFolderId = newFolder.Id;
                }

                var uploadedImages = new List<ProjectImageGallery>();
                var uploadErrors = new List<string>();

                foreach (var filePath in imageFiles)
                {
                    var fileName = Path.GetFileName(filePath);
                    var fileLength = new FileInfo(filePath).Length;
                    using (var fileStream = new FileStream(filePath, FileMode.Open, FileAccess.Read, FileShare.Read, 4096, FileOptions.None))
                    {
                        var formFile = new FormFile(fileStream, 0, fileLength, "file", fileName)
                        {
                            Headers = new HeaderDictionary(),
                            ContentType = GetMimeType(fileName)
                        };

                        var uploadResult = await _fileUploadService.UploadFileAsync(formFile, "project-galleries");
                        if (!uploadResult.Success)
                        {
                            uploadErrors.Add($"{fileName}: {uploadResult.Error}");
                            continue;
                        }

                        var title = Path.GetFileNameWithoutExtension(fileName);
                        var image = new ProjectImageGallery
                        {
                            Title = title,
                            ProjectId = projectId,
                            FolderId = targetFolderId,
                            CreatorUserId = userId!,
                            FileName = fileName,
                            FilePath = uploadResult.FilePath,
                            FileSize = fileLength,
                            MimeType = formFile.ContentType,
                            CreatedAt = DateTime.UtcNow
                        };
                        uploadedImages.Add(image);
                    }
                }

                if (!uploadedImages.Any())
                {
                    TempData["Error"] = "هیچ تصویری آپلود نشد. " + (uploadErrors.Any() ? string.Join(" ", uploadErrors.Take(3)) : "");
                    return RedirectToAction(nameof(UploadFigmaDesign), new { projectId, folderId });
                }

                _context.ProjectImageGalleries.AddRange(uploadedImages);
                await _context.SaveChangesAsync();

                TempData["Success"] = $"{uploadedImages.Count} فریم از طراحی فیگما با موفقیت آپلود و در گالری ذخیره شد.";
                return RedirectToAction(nameof(ViewFigmaDesign), new { projectId, folderId = targetFolderId });
            }
            finally
            {
                try
                {
                    if (tempZipPath != null && System.IO.File.Exists(tempZipPath))
                        System.IO.File.Delete(tempZipPath);
                    if (tempExtractPath != null && Directory.Exists(tempExtractPath))
                        Directory.Delete(tempExtractPath, true);
                }
                catch { /* ignore cleanup */ }
            }
        }

        /// <summary>
        /// نمایش طراحی فیگما (فریم‌های استخراج‌شده از zip)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> ViewFigmaDesign(int projectId, int? folderId = null)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);

            var hasAccess = await _context.Projects
                .AnyAsync(p => p.Id == projectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));

            if (!hasAccess)
            {
                TempData["Error"] = "شما به این پروژه دسترسی ندارید.";
                return RedirectToAction("Index", "Projects");
            }

            var query = _context.ProjectImageGalleries
                .Include(img => img.Folder)
                .Where(img => img.ProjectId == projectId);

            if (folderId.HasValue)
                query = query.Where(img => img.FolderId == folderId);

            var images = await query.OrderBy(img => img.CreatedAt).ToListAsync();

            var project = await _context.Projects.FindAsync(projectId);
            var folder = folderId.HasValue
                ? await _context.ProjectImageGalleryFolders.FirstOrDefaultAsync(f => f.Id == folderId && f.ProjectId == projectId)
                : null;

            // مسیر breadcrumb برای پوشه‌های والد (در صورت تودرتو بودن)
            var breadcrumbPath = new List<ProjectImageGalleryFolder>();
            if (folder != null)
            {
                var parentId = folder.ParentFolderId;
                while (parentId.HasValue)
                {
                    var parentFolder = await _context.ProjectImageGalleryFolders
                        .FirstOrDefaultAsync(f => f.Id == parentId.Value && f.ProjectId == projectId);
                    if (parentFolder == null) break;
                    breadcrumbPath.Insert(0, parentFolder);
                    parentId = parentFolder.ParentFolderId;
                }
            }
            ViewBag.BreadcrumbFolders = breadcrumbPath;

            ViewBag.ProjectName = project?.Name;
            ViewBag.ProjectId = projectId;
            ViewBag.FolderId = folderId;
            ViewBag.Folder = folder;
            return View(images);
        }

        private bool IsAjaxRequest() =>
            string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

        private static string GetMimeType(string fileName)
        {
            var ext = Path.GetExtension(fileName).ToLowerInvariant();
            return ext switch
            {
                ".png" => "image/png",
                ".jpg" or ".jpeg" => "image/jpeg",
                ".gif" => "image/gif",
                ".webp" => "image/webp",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };
        }

        // Helper: محاسبه تمام IDهای پوشه‌های موجود در درخت یک پوشه
        private HashSet<int> GetFolderTreeIds(int folderId, List<ProjectImageGalleryFolder> allFolders)
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
        private async Task<List<SelectListItem>> GetFoldersSelectListAsync(int projectId, string userId, int? selectedFolderId = null)
        {
            var folders = await _context.ProjectImageGalleryFolders
                .Where(f => f.ProjectId == projectId)
                .OrderBy(f => f.Name)
                .ToListAsync();

            var selectList = new List<SelectListItem>
            {
                new SelectListItem 
                { 
                    Text = "بدون پوشه", 
                    Value = "", 
                    Selected = !selectedFolderId.HasValue 
                }
            };

            foreach (var folder in folders)
            {
                selectList.Add(new SelectListItem
                {
                    Text = folder.Name,
                    Value = folder.Id.ToString(),
                    Selected = selectedFolderId.HasValue && folder.Id == selectedFolderId.Value
                });
            }

            return selectList;
        }
    }
}

