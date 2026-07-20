using System.Security.Claims;
using DNTPersianUtils.Core;
using Endpoint.Site.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using TaskPlanner.Application.Services.FileUpload;
using TaskPlanner.Domain.Entities.TaskPlanner;
using TaskPlanner.Domain.Entities.Users;
using TaskPlanner.Persistence.Contexts;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]/{id?}")]
    public class FeaturesController : Controller
    {
        private readonly MVPTestDatabaseContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IFileUploadService _fileUploadService;

        public FeaturesController(
            MVPTestDatabaseContext context,
            UserManager<User> userManager,
            IFileUploadService fileUploadService)
        {
            _context = context;
            _userManager = userManager;
            _fileUploadService = fileUploadService;
        }

        public async Task<IActionResult> Index(int? projectId, int? id)
        {
            var pid = projectId ?? id;
            if (!pid.HasValue)
                return RedirectToAction("Index", "Projects");

            var projectIdVal = pid.Value;
            if (!await HasProjectAccessAsync(projectIdVal))
                return RedirectToAction("Index", "Projects");

            var project = await _context.Projects.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectIdVal);
            if (project == null)
                return NotFound();

            var features = await _context.ProjectFeatures.AsNoTracking()
                .Where(f => f.ProjectId == projectIdVal)
                .Include(f => f.Functions)
                .Include(f => f.ApiContracts)
                .Include(f => f.BusinessRules)
                .Include(f => f.Tasks)
                .Include(f => f.CodeReviewerUser)
                .AsSplitQuery()
                .OrderByDescending(f => f.UpdatedAt)
                .ToListAsync();

            ViewBag.ProjectId = projectIdVal;
            ViewData["Title"] = "فیچرهای پروژه";

            var vm = new FeatureIndexVm
            {
                ProjectId = projectIdVal,
                ProjectName = project.Name,
                Features = features.Select(f => new FeatureListItemVm
                {
                    Id = f.Id,
                    Name = f.Name,
                    Description = f.Description,
                    TaskCount = f.Tasks?.Count ?? 0,
                    FunctionCount = f.Functions?.Count ?? 0,
                    ApiContractCount = f.ApiContracts?.Count ?? 0,
                    BusinessRuleCount = f.BusinessRules?.Count ?? 0,
                    CodeReviewerName = DisplayName(f.CodeReviewerUser),
                    UpdatedAt = f.UpdatedAt
                }).ToList()
            };

            return View(vm);
        }

        public async Task<IActionResult> Create(int? projectId)
        {
            if (!projectId.HasValue)
                return RedirectToAction("Index", "Projects");

            if (!await HasProjectAccessAsync(projectId.Value))
                return RedirectToAction("Index", "Projects");

            var project = await _context.Projects.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectId.Value);
            if (project == null)
                return NotFound();

            ViewBag.ProjectId = projectId.Value;
            ViewBag.ProjectName = project.Name;
            ViewData["Title"] = "افزودن فیچر";
            return View(new FeatureCreateVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(FeatureCreateVm vm, int projectId)
        {
            if (!await HasProjectAccessAsync(projectId))
                return RedirectToAction("Index", "Projects");

            var project = await _context.Projects.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectId);
            if (project == null)
                return NotFound();

            if (!ModelState.IsValid)
            {
                ViewBag.ProjectId = projectId;
                ViewBag.ProjectName = project.Name;
                return View(vm);
            }

            var userId = CurrentUserId();
            var feature = new ProjectFeature
            {
                ProjectId = projectId,
                Name = vm.Name.Trim(),
                Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim(),
                CreatedByUserId = userId,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            // وضعیت‌های پیش‌فرض صفحه با توضیح خالی تا کاربر تکمیل کند
            foreach (FeaturePageStateType stateType in Enum.GetValues(typeof(FeaturePageStateType)))
            {
                feature.PageStates.Add(new FeaturePageState
                {
                    StateType = stateType,
                    BehaviorDescription = "رفتار این وضعیت هنوز مشخص نشده است.",
                    HasSeparateDesign = false
                });
            }

            _context.ProjectFeatures.Add(feature);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Details), new { id = feature.Id });
        }

        public async Task<IActionResult> Details(int id)
        {
            var feature = await LoadFeatureAsync(id);
            if (feature == null)
                return NotFound();

            if (!await HasProjectAccessAsync(feature.ProjectId))
                return RedirectToAction("Index", "Projects");

            var userId = CurrentUserId();
            var isReviewer = !string.IsNullOrEmpty(feature.CodeReviewerUserId) &&
                             string.Equals(feature.CodeReviewerUserId, userId, StringComparison.Ordinal);
            var isCreator = await IsProjectCreatorAsync(feature.ProjectId);

            ViewBag.ProjectId = feature.ProjectId;
            ViewData["Title"] = feature.Name;

            ViewBag.Categories = await _context.TaskCategories
                .AsNoTracking()
                .Where(c => c.ProjectId == feature.ProjectId)
                .OrderBy(c => c.Name)
                .ToListAsync();

            ViewBag.TaskMembers = await GetProjectMemberOptionsAsync(feature.ProjectId);

            var defaultIssueType = await _context.ProjectIssueTypes.AsNoTracking()
                .Where(p => p.ProjectId == feature.ProjectId && p.Level != IssueTypeLevel.Subtask)
                .OrderBy(p => p.Order)
                .FirstOrDefaultAsync(p => p.BaseType == IssueType.Task)
                ?? await _context.ProjectIssueTypes.AsNoTracking()
                    .Where(p => p.ProjectId == feature.ProjectId && p.Level != IssueTypeLevel.Subtask)
                    .OrderBy(p => p.Order)
                    .FirstOrDefaultAsync();
            ViewBag.DefaultProjectIssueTypeId = defaultIssueType?.Id;
            ViewBag.DefaultIssueTypeBase = defaultIssueType != null ? (int)defaultIssueType.BaseType : (int)IssueType.Task;

            var vm = MapDetails(
                feature,
                currentUserId: userId,
                isCreator: isCreator,
                canManageCodeReview: isReviewer || isCreator,
                canChangeCodeReviewer: isCreator,
                canManageFeatureSpec: isCreator,
                canManagePageStatesAndApi: true);
            return View(vm);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var feature = await _context.ProjectFeatures
                .Include(f => f.Project)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (feature == null)
                return NotFound();

            if (!await HasProjectAccessAsync(feature.ProjectId))
                return RedirectToAction("Index", "Projects");

            ViewBag.ProjectId = feature.ProjectId;
            ViewBag.ProjectName = feature.Project?.Name ?? "پروژه";
            ViewData["Title"] = "ویرایش فیچر";

            return View(new FeatureEditVm
            {
                Id = feature.Id,
                Name = feature.Name,
                Description = feature.Description
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, FeatureEditVm vm)
        {
            if (id != vm.Id)
                return NotFound();

            var feature = await _context.ProjectFeatures
                .Include(f => f.Project)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (feature == null)
                return NotFound();

            if (!await HasProjectAccessAsync(feature.ProjectId))
                return RedirectToAction("Index", "Projects");

            if (!ModelState.IsValid)
            {
                ViewBag.ProjectId = feature.ProjectId;
                ViewBag.ProjectName = feature.Project?.Name ?? "پروژه";
                return View(vm);
            }

            feature.Name = vm.Name.Trim();
            feature.Description = string.IsNullOrWhiteSpace(vm.Description) ? null : vm.Description.Trim();
            feature.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Details), new { id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddFunction(FeatureFunctionItemVm vm)
        {
            var feature = await _context.ProjectFeatures
                .Include(f => f.Functions)
                .FirstOrDefaultAsync(f => f.Id == vm.FeatureId);
            if (feature == null)
                return SpecError("فیچر یافت نشد.", vm.FeatureId, 404);

            if (!await HasProjectAccessAsync(feature.ProjectId))
                return SpecError("دسترسی ندارید.", vm.FeatureId, 403);

            var deny = await DenyUnlessProjectCreatorAsync(feature.ProjectId, feature.Id);
            if (deny != null)
                return deny;

            if (string.IsNullOrWhiteSpace(vm.Title))
                return SpecError("عنوان کارکرد الزامی است.", vm.FeatureId);

            var nextOrder = feature.Functions.Count == 0 ? 0 : feature.Functions.Max(f => f.SortOrder) + 1;
            var entity = new FeatureFunction
            {
                FeatureId = feature.Id,
                Title = vm.Title.Trim(),
                SortOrder = nextOrder
            };
            feature.Functions.Add(entity);
            feature.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return SpecSuccess("کارکرد اضافه شد.", feature.Id, new { id = entity.Id, title = entity.Title });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditFunction(FeatureFunctionItemVm vm)
        {
            var function = await _context.FeatureFunctions
                .Include(f => f.Feature)
                .FirstOrDefaultAsync(f => f.Id == vm.Id);
            if (function == null)
                return SpecError("کارکرد یافت نشد.", vm.FeatureId, 404);

            if (!await HasProjectAccessAsync(function.Feature.ProjectId))
                return SpecError("دسترسی ندارید.", function.FeatureId, 403);

            var deny = await DenyUnlessProjectCreatorAsync(function.Feature.ProjectId, function.FeatureId);
            if (deny != null)
                return deny;

            if (string.IsNullOrWhiteSpace(vm.Title))
                return SpecError("عنوان کارکرد الزامی است.", function.FeatureId);

            function.Title = vm.Title.Trim();
            function.Feature.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return SpecSuccess("کارکرد ویرایش شد.", function.FeatureId, new { id = function.Id, title = function.Title });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteFunction(int id)
        {
            var function = await _context.FeatureFunctions
                .Include(f => f.Feature)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (function == null)
                return SpecError("کارکرد یافت نشد.", 0, 404);

            if (!await HasProjectAccessAsync(function.Feature.ProjectId))
                return SpecError("دسترسی ندارید.", function.FeatureId, 403);

            var deny = await DenyUnlessProjectCreatorAsync(function.Feature.ProjectId, function.FeatureId);
            if (deny != null)
                return deny;

            var featureId = function.FeatureId;
            var deletedId = function.Id;
            _context.FeatureFunctions.Remove(function);
            function.Feature.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return SpecSuccess("کارکرد حذف شد.", featureId, new { id = deletedId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddBusinessRule(FeatureBusinessRuleItemVm vm)
        {
            var feature = await _context.ProjectFeatures
                .Include(f => f.BusinessRules)
                .FirstOrDefaultAsync(f => f.Id == vm.FeatureId);
            if (feature == null)
                return SpecError("فیچر یافت نشد.", vm.FeatureId, 404);

            if (!await HasProjectAccessAsync(feature.ProjectId))
                return SpecError("دسترسی ندارید.", vm.FeatureId, 403);

            var deny = await DenyUnlessProjectCreatorAsync(feature.ProjectId, feature.Id);
            if (deny != null)
                return deny;

            if (string.IsNullOrWhiteSpace(vm.Description))
                return SpecError("توضیح قانون الزامی است.", vm.FeatureId);

            var nextOrder = feature.BusinessRules.Count == 0 ? 0 : feature.BusinessRules.Max(r => r.SortOrder) + 1;
            var entity = new FeatureBusinessRule
            {
                FeatureId = feature.Id,
                Description = vm.Description.Trim(),
                SortOrder = nextOrder
            };
            feature.BusinessRules.Add(entity);
            feature.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return SpecSuccess("قانون کسب‌وکار اضافه شد.", feature.Id, new { id = entity.Id, description = entity.Description });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditBusinessRule(FeatureBusinessRuleItemVm vm)
        {
            var rule = await _context.FeatureBusinessRules
                .Include(r => r.Feature)
                .FirstOrDefaultAsync(r => r.Id == vm.Id);
            if (rule == null)
                return SpecError("قانون یافت نشد.", vm.FeatureId, 404);

            if (!await HasProjectAccessAsync(rule.Feature.ProjectId))
                return SpecError("دسترسی ندارید.", rule.FeatureId, 403);

            var deny = await DenyUnlessProjectCreatorAsync(rule.Feature.ProjectId, rule.FeatureId);
            if (deny != null)
                return deny;

            if (string.IsNullOrWhiteSpace(vm.Description))
                return SpecError("توضیح قانون الزامی است.", rule.FeatureId);

            rule.Description = vm.Description.Trim();
            rule.Feature.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return SpecSuccess("قانون کسب‌وکار ویرایش شد.", rule.FeatureId, new { id = rule.Id, description = rule.Description });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteBusinessRule(int id)
        {
            var rule = await _context.FeatureBusinessRules
                .Include(r => r.Feature)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (rule == null)
                return SpecError("قانون یافت نشد.", 0, 404);

            if (!await HasProjectAccessAsync(rule.Feature.ProjectId))
                return SpecError("دسترسی ندارید.", rule.FeatureId, 403);

            var deny = await DenyUnlessProjectCreatorAsync(rule.Feature.ProjectId, rule.FeatureId);
            if (deny != null)
                return deny;

            var featureId = rule.FeatureId;
            var deletedId = rule.Id;
            _context.FeatureBusinessRules.Remove(rule);
            rule.Feature.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return SpecSuccess("قانون کسب‌وکار حذف شد.", featureId, new { id = deletedId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddImplementationComment(FeatureImplementationCommentCreateVm vm)
        {
            var feature = await _context.ProjectFeatures
                .FirstOrDefaultAsync(f => f.Id == vm.FeatureId);
            if (feature == null)
                return SpecError("فیچر یافت نشد.", vm.FeatureId, 404);

            if (!await HasProjectAccessAsync(feature.ProjectId))
                return SpecError("دسترسی ندارید.", vm.FeatureId, 403);

            var files = vm.Attachments?
                .Where(f => f != null && f.Length > 0)
                .ToList() ?? new List<IFormFile>();

            if (string.IsNullOrWhiteSpace(vm.Body) && files.Count == 0)
                return SpecError("متن یادداشت یا حداقل یک تصویر الزامی است.", vm.FeatureId);

            var userId = CurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return SpecError("کاربر احراز هویت نشده است.", vm.FeatureId, 401);

            var author = await _context.Users.AsNoTracking()
                .FirstOrDefaultAsync(u => u.Id == userId);

            var entity = new FeatureImplementationComment
            {
                FeatureId = feature.Id,
                AuthorUserId = userId,
                Body = string.IsNullOrWhiteSpace(vm.Body) ? "" : vm.Body.Trim(),
                CreatedAt = DateTime.UtcNow
            };
            _context.FeatureImplementationComments.Add(entity);
            feature.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            var uploadedAttachments = new List<FeatureImplementationCommentAttachment>();
            if (files.Count > 0)
            {
                var uploadErrors = new List<string>();
                foreach (var file in files)
                {
                    var fileType = _fileUploadService.GetFileType(file.FileName);
                    if (!string.Equals(fileType, "Image", StringComparison.OrdinalIgnoreCase)
                        && !(file.ContentType?.StartsWith("image/", StringComparison.OrdinalIgnoreCase) ?? false))
                    {
                        uploadErrors.Add($"{file.FileName}: فقط فایل تصویری مجاز است.");
                        continue;
                    }

                    var uploadResult = await _fileUploadService.UploadFileAsync(file, "feature-implementation-comments");
                    if (!uploadResult.Success)
                    {
                        uploadErrors.Add($"{file.FileName}: {uploadResult.Error}");
                        continue;
                    }

                    var attachment = new FeatureImplementationCommentAttachment
                    {
                        CommentId = entity.Id,
                        FileName = file.FileName,
                        FilePath = uploadResult.FilePath,
                        FileType = "Image",
                        FileSize = file.Length,
                        MimeType = file.ContentType,
                        UploadedAt = DateTime.UtcNow
                    };
                    _context.FeatureImplementationCommentAttachments.Add(attachment);
                    uploadedAttachments.Add(attachment);
                }

                if (uploadErrors.Count > 0 && uploadedAttachments.Count == 0 && string.IsNullOrWhiteSpace(entity.Body))
                {
                    _context.FeatureImplementationComments.Remove(entity);
                    await _context.SaveChangesAsync();
                    return SpecError(string.Join("\n", uploadErrors), feature.Id);
                }

                if (uploadedAttachments.Count > 0)
                    await _context.SaveChangesAsync();
            }

            return SpecSuccess("یادداشت ثبت شد.", feature.Id, MapImplementationCommentResponse(entity, author, uploadedAttachments, canDelete: true));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteImplementationComment(int id)
        {
            var comment = await _context.FeatureImplementationComments
                .Include(c => c.Feature)
                .Include(c => c.Attachments)
                .FirstOrDefaultAsync(c => c.Id == id);
            if (comment == null)
                return SpecError("یادداشت یافت نشد.", 0, 404);

            if (!await HasProjectAccessAsync(comment.Feature.ProjectId))
                return SpecError("دسترسی ندارید.", comment.FeatureId, 403);

            var userId = CurrentUserId();
            var isCreator = await IsProjectCreatorAsync(comment.Feature.ProjectId);
            if (!isCreator && !string.Equals(comment.AuthorUserId, userId, StringComparison.Ordinal))
                return SpecError("فقط نویسنده یا سازنده پروژه می‌تواند این یادداشت را حذف کند.", comment.FeatureId, 403);

            var featureId = comment.FeatureId;
            var deletedId = comment.Id;
            _context.FeatureImplementationComments.Remove(comment);
            comment.Feature.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return SpecSuccess("یادداشت حذف شد.", featureId, new { id = deletedId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddPageState(FeaturePageStateItemVm vm)
        {
            var feature = await _context.ProjectFeatures
                .Include(f => f.PageStates)
                .FirstOrDefaultAsync(f => f.Id == vm.FeatureId);
            if (feature == null)
                return SpecError("فیچر یافت نشد.", vm.FeatureId, 404);

            if (!await HasProjectAccessAsync(feature.ProjectId))
                return SpecError("دسترسی ندارید.", vm.FeatureId, 403);

            if (!Enum.IsDefined(typeof(FeaturePageStateType), vm.StateType))
                return SpecError("نوع وضعیت معتبر نیست.", vm.FeatureId);

            if (feature.PageStates.Any(p => p.StateType == vm.StateType))
                return SpecError("این وضعیت قبلاً برای فیچر تعریف شده است.", vm.FeatureId);

            if (string.IsNullOrWhiteSpace(vm.BehaviorDescription))
                return SpecError("توضیح رفتار الزامی است.", vm.FeatureId);

            var entity = new FeaturePageState
            {
                FeatureId = feature.Id,
                StateType = vm.StateType,
                BehaviorDescription = vm.BehaviorDescription.Trim(),
                HasSeparateDesign = vm.HasSeparateDesign
            };
            feature.PageStates.Add(entity);
            feature.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return SpecSuccess("وضعیت صفحه اضافه شد.", feature.Id, new
            {
                id = entity.Id,
                stateType = (int)entity.StateType,
                stateName = entity.StateType.GetDisplayName(),
                behaviorDescription = entity.BehaviorDescription,
                hasSeparateDesign = entity.HasSeparateDesign
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditPageState(FeaturePageStateItemVm vm)
        {
            var pageState = await _context.FeaturePageStates
                .Include(p => p.Feature)
                .FirstOrDefaultAsync(p => p.Id == vm.Id);
            if (pageState == null)
                return SpecError("وضعیت صفحه یافت نشد.", vm.FeatureId, 404);

            if (!await HasProjectAccessAsync(pageState.Feature.ProjectId))
                return SpecError("دسترسی ندارید.", pageState.FeatureId, 403);

            if (string.IsNullOrWhiteSpace(vm.BehaviorDescription))
                return SpecError("توضیح رفتار الزامی است.", pageState.FeatureId);

            pageState.BehaviorDescription = vm.BehaviorDescription.Trim();
            pageState.HasSeparateDesign = vm.HasSeparateDesign;
            pageState.Feature.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return SpecSuccess("وضعیت صفحه ویرایش شد.", pageState.FeatureId, new
            {
                id = pageState.Id,
                stateType = (int)pageState.StateType,
                stateName = pageState.StateType.GetDisplayName(),
                behaviorDescription = pageState.BehaviorDescription,
                hasSeparateDesign = pageState.HasSeparateDesign
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePageState(int id)
        {
            var pageState = await _context.FeaturePageStates
                .Include(p => p.Feature)
                .FirstOrDefaultAsync(p => p.Id == id);
            if (pageState == null)
                return SpecError("وضعیت صفحه یافت نشد.", 0, 404);

            if (!await HasProjectAccessAsync(pageState.Feature.ProjectId))
                return SpecError("دسترسی ندارید.", pageState.FeatureId, 403);

            var featureId = pageState.FeatureId;
            var deleted = new
            {
                id = pageState.Id,
                stateType = (int)pageState.StateType,
                stateName = pageState.StateType.GetDisplayName()
            };
            _context.FeaturePageStates.Remove(pageState);
            pageState.Feature.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return SpecSuccess("وضعیت صفحه حذف شد.", featureId, deleted);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddApiContract(FeatureApiContractItemVm vm)
        {
            var feature = await _context.ProjectFeatures
                .Include(f => f.ApiContracts)
                .FirstOrDefaultAsync(f => f.Id == vm.FeatureId);
            if (feature == null)
                return NotFound();

            if (!await HasProjectAccessAsync(feature.ProjectId))
                return RedirectToAction("Index", "Projects");

            if (string.IsNullOrWhiteSpace(vm.Endpoint))
            {
                TempData["Error"] = "Endpoint الزامی است.";
                return RedirectToAction(nameof(Details), new { id = vm.FeatureId });
            }

            var nextOrder = feature.ApiContracts.Count == 0 ? 0 : feature.ApiContracts.Max(a => a.SortOrder) + 1;
            var entity = new FeatureApiContract { FeatureId = feature.Id };
            ApplyApiContract(entity, vm, nextOrder);
            feature.ApiContracts.Add(entity);
            feature.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "قرارداد API اضافه شد.";
            return RedirectToAction(nameof(Details), new { id = feature.Id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditApiContract(FeatureApiContractItemVm vm)
        {
            var api = await _context.FeatureApiContracts
                .Include(a => a.Feature)
                .FirstOrDefaultAsync(a => a.Id == vm.Id);
            if (api == null)
                return NotFound();

            if (!await HasProjectAccessAsync(api.Feature.ProjectId))
                return RedirectToAction("Index", "Projects");

            if (string.IsNullOrWhiteSpace(vm.Endpoint))
            {
                TempData["Error"] = "Endpoint الزامی است.";
                return RedirectToAction(nameof(Details), new { id = api.FeatureId });
            }

            ApplyApiContract(api, vm, api.SortOrder);
            api.Feature.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "قرارداد API ویرایش شد.";
            return RedirectToAction(nameof(Details), new { id = api.FeatureId });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteApiContract(int id)
        {
            var api = await _context.FeatureApiContracts
                .Include(a => a.Feature)
                .FirstOrDefaultAsync(a => a.Id == id);
            if (api == null)
                return NotFound();

            if (!await HasProjectAccessAsync(api.Feature.ProjectId))
                return RedirectToAction("Index", "Projects");

            var featureId = api.FeatureId;
            _context.FeatureApiContracts.Remove(api);
            api.Feature.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "قرارداد API حذف شد.";
            return RedirectToAction(nameof(Details), new { id = featureId });
        }

        public async Task<IActionResult> Settings(int id)
        {
            var feature = await _context.ProjectFeatures
                .Include(f => f.Project)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (feature == null)
                return NotFound();

            if (!await HasProjectAccessAsync(feature.ProjectId))
                return RedirectToAction("Index", "Projects");

            if (!await IsProjectCreatorAsync(feature.ProjectId))
            {
                TempData["Error"] = "فقط سازنده پروژه می‌تواند مسئول Code Review را تغییر دهد.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var members = await GetProjectMemberOptionsAsync(feature.ProjectId);
            ViewBag.ProjectId = feature.ProjectId;
            ViewData["Title"] = "تنظیمات فیچر";

            return View(new FeatureSettingsVm
            {
                Id = feature.Id,
                ProjectId = feature.ProjectId,
                FeatureName = feature.Name,
                CodeReviewerUserId = feature.CodeReviewerUserId,
                MemberOptions = ToSelectListItems(members, feature.CodeReviewerUserId)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Settings(int id, FeatureSettingsVm vm)
        {
            if (id != vm.Id)
                return NotFound();

            var feature = await _context.ProjectFeatures
                .FirstOrDefaultAsync(f => f.Id == id);
            if (feature == null)
                return NotFound();

            if (!await HasProjectAccessAsync(feature.ProjectId))
                return RedirectToAction("Index", "Projects");

            if (!await IsProjectCreatorAsync(feature.ProjectId))
            {
                TempData["Error"] = "فقط سازنده پروژه می‌تواند مسئول Code Review را تغییر دهد.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!string.IsNullOrWhiteSpace(vm.CodeReviewerUserId))
            {
                var isMember = await IsProjectMemberOrCreatorAsync(feature.ProjectId, vm.CodeReviewerUserId);
                if (!isMember)
                {
                    ModelState.AddModelError(nameof(vm.CodeReviewerUserId), "کاربر انتخاب‌شده عضو این پروژه نیست.");
                }
            }

            if (!ModelState.IsValid)
            {
                var members = await GetProjectMemberOptionsAsync(feature.ProjectId);
                vm.FeatureName = feature.Name;
                vm.ProjectId = feature.ProjectId;
                vm.MemberOptions = ToSelectListItems(members, vm.CodeReviewerUserId);
                ViewBag.ProjectId = feature.ProjectId;
                return View(vm);
            }

            feature.CodeReviewerUserId = string.IsNullOrWhiteSpace(vm.CodeReviewerUserId)
                ? null
                : vm.CodeReviewerUserId;
            feature.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "تنظیمات فیچر ذخیره شد.";
            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> AddCodeReview(int id)
        {
            var feature = await _context.ProjectFeatures
                .AsNoTracking()
                .FirstOrDefaultAsync(f => f.Id == id);
            if (feature == null)
                return NotFound();

            if (!await HasProjectAccessAsync(feature.ProjectId))
                return RedirectToAction("Index", "Projects");

            if (!await CanManageCodeReviewAsync(feature))
            {
                TempData["Error"] = "فقط مسئول Code Review یا سازنده پروژه می‌تواند امتیاز ثبت کند.";
                return RedirectToAction(nameof(Details), new { id });
            }

            var members = await GetProjectMemberOptionsAsync(feature.ProjectId);
            ViewBag.ProjectId = feature.ProjectId;
            ViewBag.FeatureName = feature.Name;
            ViewData["Title"] = "ثبت Code Review";

            return View(new FeatureCodeReviewCreateVm
            {
                FeatureId = id,
                MemberOptions = ToSelectListItems(members, null)
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCodeReview(int id, FeatureCodeReviewCreateVm vm)
        {
            if (id != vm.FeatureId)
                return NotFound();

            var feature = await _context.ProjectFeatures
                .FirstOrDefaultAsync(f => f.Id == id);
            if (feature == null)
                return NotFound();

            if (!await HasProjectAccessAsync(feature.ProjectId))
                return RedirectToAction("Index", "Projects");

            if (!await CanManageCodeReviewAsync(feature))
            {
                TempData["Error"] = "فقط مسئول Code Review یا سازنده پروژه می‌تواند امتیاز ثبت کند.";
                return RedirectToAction(nameof(Details), new { id });
            }

            if (!await IsProjectMemberOrCreatorAsync(feature.ProjectId, vm.RevieweeUserId))
            {
                ModelState.AddModelError(nameof(vm.RevieweeUserId), "فرد انتخاب‌شده عضو این پروژه نیست.");
            }

            if (!ModelState.IsValid)
            {
                var members = await GetProjectMemberOptionsAsync(feature.ProjectId);
                vm.MemberOptions = ToSelectListItems(members, vm.RevieweeUserId);
                ViewBag.ProjectId = feature.ProjectId;
                ViewBag.FeatureName = feature.Name;
                return View(vm);
            }

            var review = new FeatureCodeReview
            {
                FeatureId = feature.Id,
                ReviewerUserId = CurrentUserId()!,
                RevieweeUserId = vm.RevieweeUserId,
                Score = vm.Score,
                Comment = string.IsNullOrWhiteSpace(vm.Comment) ? null : vm.Comment.Trim(),
                CreatedAt = DateTime.UtcNow
            };

            _context.FeatureCodeReviews.Add(review);
            feature.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "امتیاز Code Review ثبت شد.";
            return RedirectToAction(nameof(Details), new { id });
        }

        public async Task<IActionResult> DeleteCodeReview(int id)
        {
            var review = await _context.FeatureCodeReviews
                .AsNoTracking()
                .Include(r => r.Feature)
                .Include(r => r.ReviewerUser)
                .Include(r => r.RevieweeUser)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (review == null)
                return NotFound();

            if (!await HasProjectAccessAsync(review.Feature.ProjectId))
                return RedirectToAction("Index", "Projects");

            if (!await CanManageCodeReviewAsync(review.Feature))
            {
                TempData["Error"] = "فقط مسئول Code Review یا سازنده پروژه می‌تواند امتیاز را حذف کند.";
                return RedirectToAction(nameof(Details), new { id = review.FeatureId });
            }

            ViewBag.ProjectId = review.Feature.ProjectId;
            ViewBag.FeatureName = review.Feature.Name;
            ViewBag.ReviewerName = DisplayName(review.ReviewerUser);
            ViewBag.RevieweeName = DisplayName(review.RevieweeUser);
            ViewData["Title"] = "حذف امتیاز Code Review";
            return View(review);
        }

        [HttpPost, ActionName("DeleteCodeReview")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCodeReviewConfirmed(int id)
        {
            var review = await _context.FeatureCodeReviews
                .Include(r => r.Feature)
                .FirstOrDefaultAsync(r => r.Id == id);
            if (review == null)
                return NotFound();

            if (!await HasProjectAccessAsync(review.Feature.ProjectId))
                return RedirectToAction("Index", "Projects");

            if (!await CanManageCodeReviewAsync(review.Feature))
            {
                TempData["Error"] = "فقط مسئول Code Review یا سازنده پروژه می‌تواند امتیاز را حذف کند.";
                return RedirectToAction(nameof(Details), new { id = review.FeatureId });
            }

            var featureId = review.FeatureId;
            _context.FeatureCodeReviews.Remove(review);
            review.Feature.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            TempData["Success"] = "امتیاز Code Review حذف شد.";
            return RedirectToAction(nameof(Details), new { id = featureId });
        }

        public async Task<IActionResult> Delete(int id)
        {
            var feature = await _context.ProjectFeatures
                .Include(f => f.Project)
                .Include(f => f.Tasks)
                .Include(f => f.Tickets)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (feature == null)
                return NotFound();

            if (!await HasProjectAccessAsync(feature.ProjectId))
                return RedirectToAction("Index", "Projects");

            ViewBag.ProjectId = feature.ProjectId;
            ViewBag.ProjectName = feature.Project?.Name ?? "پروژه";
            ViewBag.TaskCount = feature.Tasks?.Count ?? 0;
            ViewBag.TicketCount = feature.Tickets?.Count ?? 0;
            ViewData["Title"] = "حذف فیچر";
            return View(feature);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var feature = await _context.ProjectFeatures
                .Include(f => f.Tasks)
                .Include(f => f.Tickets)
                .FirstOrDefaultAsync(f => f.Id == id);
            if (feature == null)
                return NotFound();

            if (!await HasProjectAccessAsync(feature.ProjectId))
                return RedirectToAction("Index", "Projects");

            var projectId = feature.ProjectId;

            // جدا کردن تسک‌ها و تیکت‌ها از فیچر قبل از حذف
            foreach (var task in feature.Tasks)
            {
                task.FeatureId = null;
            }

            foreach (var ticket in feature.Tickets)
            {
                ticket.FeatureId = null;
            }

            _context.ProjectFeatures.Remove(feature);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index), new { projectId });
        }

        private async Task<ProjectFeature?> LoadFeatureAsync(int id)
        {
            return await _context.ProjectFeatures.AsNoTracking()
                .Include(f => f.Project)
                .Include(f => f.CodeReviewerUser)
                .Include(f => f.Functions)
                .Include(f => f.PageStates)
                .Include(f => f.ApiContracts)
                .Include(f => f.BusinessRules)
                .Include(f => f.ImplementationComments)
                    .ThenInclude(c => c.AuthorUser)
                .Include(f => f.ImplementationComments)
                    .ThenInclude(c => c.Attachments)
                .Include(f => f.CodeReviews)
                    .ThenInclude(r => r.ReviewerUser)
                .Include(f => f.CodeReviews)
                    .ThenInclude(r => r.RevieweeUser)
                .Include(f => f.Tasks)
                    .ThenInclude(t => t.AssignedUser)
                .Include(f => f.Tasks)
                    .ThenInclude(t => t.SprintTasks)
                        .ThenInclude(st => st.Sprint)
                .AsSplitQuery()
                .FirstOrDefaultAsync(f => f.Id == id);
        }

        private FeatureDetailsVm MapDetails(
            ProjectFeature feature,
            string? currentUserId,
            bool isCreator,
            bool canManageCodeReview,
            bool canChangeCodeReviewer,
            bool canManageFeatureSpec,
            bool canManagePageStatesAndApi)
        {
            return new FeatureDetailsVm
            {
                Id = feature.Id,
                ProjectId = feature.ProjectId,
                ProjectName = feature.Project?.Name ?? "پروژه",
                Name = feature.Name,
                Description = feature.Description,
                CodeReviewerUserId = feature.CodeReviewerUserId,
                CodeReviewerName = DisplayName(feature.CodeReviewerUser),
                CanManageCodeReview = canManageCodeReview,
                CanChangeCodeReviewer = canChangeCodeReviewer,
                CanManageFeatureSpec = canManageFeatureSpec,
                CanManagePageStatesAndApi = canManagePageStatesAndApi,
                CreatedAt = feature.CreatedAt,
                UpdatedAt = feature.UpdatedAt,
                Functions = feature.Functions.OrderBy(x => x.SortOrder).Select(x => new FeatureFunctionItemVm
                {
                    Id = x.Id,
                    FeatureId = feature.Id,
                    Title = x.Title
                }).ToList(),
                PageStates = feature.PageStates.OrderBy(x => x.StateType).Select(x => new FeaturePageStateItemVm
                {
                    Id = x.Id,
                    FeatureId = feature.Id,
                    StateType = x.StateType,
                    BehaviorDescription = x.BehaviorDescription,
                    HasSeparateDesign = x.HasSeparateDesign
                }).ToList(),
                ApiContracts = feature.ApiContracts.OrderBy(x => x.SortOrder).Select(MapApiItem).ToList(),
                BusinessRules = feature.BusinessRules.OrderBy(x => x.SortOrder).Select(x => new FeatureBusinessRuleItemVm
                {
                    Id = x.Id,
                    FeatureId = feature.Id,
                    Description = x.Description
                }).ToList(),
                Tasks = feature.Tasks.Select(t =>
                {
                    var sprintTask = t.SprintTasks?.OrderByDescending(st => st.AddedAt).FirstOrDefault();
                    return new FeatureTaskItemVm
                    {
                        Id = t.Id,
                        IssueKey = t.IssueKey,
                        Title = t.Title,
                        IssueType = t.IssueType,
                        IsCompleted = t.IsCompleted,
                        AssignedUserName = DisplayName(t.AssignedUser),
                        InSprint = sprintTask != null,
                        SprintName = sprintTask?.Sprint?.Name
                    };
                }).OrderBy(t => t.Title).ToList(),
                CodeReviews = feature.CodeReviews.OrderByDescending(r => r.CreatedAt).Select(r => new FeatureCodeReviewItemVm
                {
                    Id = r.Id,
                    ReviewerName = DisplayName(r.ReviewerUser),
                    RevieweeName = DisplayName(r.RevieweeUser),
                    Score = r.Score,
                    Comment = r.Comment,
                    CreatedAt = r.CreatedAt
                }).ToList(),
                ImplementationComments = feature.ImplementationComments
                    .OrderByDescending(c => c.CreatedAt)
                    .Select(c => new FeatureImplementationCommentItemVm
                    {
                        Id = c.Id,
                        FeatureId = feature.Id,
                        AuthorName = DisplayName(c.AuthorUser),
                        Body = c.Body,
                        CreatedAt = c.CreatedAt,
                        CreatedAtDisplay = FormatPersianDateTime(c.CreatedAt),
                        CanDelete = isCreator || string.Equals(c.AuthorUserId, currentUserId, StringComparison.Ordinal),
                        Attachments = c.Attachments
                            .OrderBy(a => a.UploadedAt)
                            .Select(a => MapImplementationAttachment(a))
                            .ToList()
                    }).ToList()
            };
        }

        private object MapImplementationCommentResponse(
            FeatureImplementationComment comment,
            User? author,
            IEnumerable<FeatureImplementationCommentAttachment> attachments,
            bool canDelete)
        {
            return new
            {
                id = comment.Id,
                authorName = DisplayName(author),
                body = comment.Body,
                createdAt = FormatPersianDateTime(comment.CreatedAt),
                canDelete,
                attachments = attachments.Select(a => new
                {
                    id = a.Id,
                    fileName = a.FileName,
                    filePath = _fileUploadService.ToPublicUrl(a.FilePath),
                    fileType = a.FileType,
                    fileSize = a.FileSize,
                    mimeType = a.MimeType
                }).ToList()
            };
        }

        private FeatureImplementationCommentAttachmentVm MapImplementationAttachment(FeatureImplementationCommentAttachment attachment) =>
            new()
            {
                Id = attachment.Id,
                FileName = attachment.FileName,
                FilePath = _fileUploadService.ToPublicUrl(attachment.FilePath),
                FileType = attachment.FileType,
                FileSize = attachment.FileSize,
                MimeType = attachment.MimeType
            };

        private static string FormatPersianDateTime(DateTime utcDateTime) =>
            utcDateTime.ToLocalTime().ToShortPersianDateTimeString().ToPersianNumbers();

        private static FeatureApiContractItemVm MapApiItem(FeatureApiContract x) => new()
        {
            Id = x.Id,
            FeatureId = x.FeatureId,
            Endpoint = x.Endpoint,
            HttpMethod = x.HttpMethod,
            RequestDescription = x.RequestDescription,
            ResponseDescription = x.ResponseDescription,
            ErrorResponseDescription = x.ErrorResponseDescription,
            StatusCodes = x.StatusCodes,
            Pagination = x.Pagination,
            Filter = x.Filter,
            Sort = x.Sort,
            FieldsDescription = x.FieldsDescription
        };

        private static void ApplyApiContract(FeatureApiContract entity, FeatureApiContractItemVm item, int order)
        {
            entity.Endpoint = item.Endpoint.Trim();
            entity.HttpMethod = string.IsNullOrWhiteSpace(item.HttpMethod) ? "GET" : item.HttpMethod.Trim().ToUpperInvariant();
            entity.RequestDescription = NullIfWhiteSpace(item.RequestDescription);
            entity.ResponseDescription = NullIfWhiteSpace(item.ResponseDescription);
            entity.ErrorResponseDescription = NullIfWhiteSpace(item.ErrorResponseDescription);
            entity.StatusCodes = NullIfWhiteSpace(item.StatusCodes);
            entity.Pagination = NullIfWhiteSpace(item.Pagination);
            entity.Filter = NullIfWhiteSpace(item.Filter);
            entity.Sort = NullIfWhiteSpace(item.Sort);
            entity.FieldsDescription = NullIfWhiteSpace(item.FieldsDescription);
            entity.SortOrder = order;
        }

        private static List<SelectListItem> ToSelectListItems(
            IEnumerable<FeatureMemberOptionVm> members,
            string? selectedUserId)
        {
            return members
                .Select(m => new SelectListItem
                {
                    Value = m.UserId,
                    Text = m.DisplayName,
                    Selected = !string.IsNullOrEmpty(selectedUserId) &&
                               string.Equals(m.UserId, selectedUserId, StringComparison.Ordinal)
                })
                .ToList();
        }

        private async Task<List<FeatureMemberOptionVm>> GetProjectMemberOptionsAsync(int projectId)
        {
            var project = await _context.Projects.AsNoTracking()
                .FirstOrDefaultAsync(p => p.Id == projectId);

            var memberIds = await _context.ProjectMembers.AsNoTracking()
                .Where(m => m.ProjectId == projectId && m.UserId != null && m.UserId != "")
                .Select(m => m.UserId)
                .ToListAsync();

            // اعضای پذیرفته‌شده از طریق دعوت (برای سازگاری با داده‌های قدیمی)
            var invitedIds = await _context.ProjectInvitations.AsNoTracking()
                .Where(i => i.ProjectId == projectId
                            && i.Status == InvitationStatus.Accepted
                            && i.InviteeId != null
                            && i.InviteeId != "")
                .Select(i => i.InviteeId!)
                .ToListAsync();

            var allIds = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in memberIds)
                allIds.Add(id);
            foreach (var id in invitedIds)
                allIds.Add(id);

            if (project != null && !string.IsNullOrEmpty(project.CreatorUserId))
                allIds.Add(project.CreatorUserId);

            var result = new List<FeatureMemberOptionVm>();
            foreach (var userId in allIds)
            {
                var user = await _userManager.FindByIdAsync(userId);
                if (user == null)
                    continue;

                var name = DisplayName(user);
                if (project != null &&
                    string.Equals(userId, project.CreatorUserId, StringComparison.Ordinal))
                {
                    name += " — سازنده پروژه";
                }

                result.Add(new FeatureMemberOptionVm
                {
                    UserId = userId,
                    DisplayName = name
                });
            }

            return result.OrderBy(u => u.DisplayName).ToList();
        }

        private async Task<bool> CanManageCodeReviewAsync(ProjectFeature feature)
        {
            var userId = CurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return false;

            if (!string.IsNullOrEmpty(feature.CodeReviewerUserId) &&
                string.Equals(feature.CodeReviewerUserId, userId, StringComparison.Ordinal))
            {
                return true;
            }

            return await _context.Projects.AsNoTracking()
                .AnyAsync(p => p.Id == feature.ProjectId && p.CreatorUserId == userId);
        }

        private async Task<bool> IsProjectCreatorAsync(int projectId)
        {
            var userId = CurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return false;

            return await _context.Projects.AsNoTracking()
                .AnyAsync(p => p.Id == projectId && p.CreatorUserId == userId);
        }

        private bool IsAjaxRequest() =>
            string.Equals(Request.Headers["X-Requested-With"], "XMLHttpRequest", StringComparison.OrdinalIgnoreCase);

        private IActionResult SpecSuccess(string message, int featureId, object? data = null)
        {
            if (IsAjaxRequest())
                return Json(new { success = true, message, data });

            TempData["Success"] = message;
            return RedirectToAction(nameof(Details), new { id = featureId });
        }

        private IActionResult SpecError(string message, int featureId, int statusCode = 400)
        {
            if (IsAjaxRequest())
            {
                Response.StatusCode = statusCode;
                return Json(new { success = false, message });
            }

            TempData["Error"] = message;
            return RedirectToAction(nameof(Details), new { id = featureId });
        }

        private async Task<IActionResult?> DenyUnlessProjectCreatorAsync(int projectId, int featureId)
        {
            if (await IsProjectCreatorAsync(projectId))
                return null;

            return SpecError("فقط سازنده پروژه می‌تواند این عملیات را انجام دهد.", featureId, 403);
        }

        private async Task<bool> IsProjectMemberOrCreatorAsync(int projectId, string userId)
        {
            if (await _context.Projects.AsNoTracking()
                    .AnyAsync(p => p.Id == projectId && p.CreatorUserId == userId))
                return true;

            if (await _context.ProjectMembers.AsNoTracking()
                    .AnyAsync(pm => pm.ProjectId == projectId && pm.UserId == userId))
                return true;

            return await _context.ProjectInvitations.AsNoTracking()
                .AnyAsync(i => i.ProjectId == projectId
                               && i.InviteeId == userId
                               && i.Status == InvitationStatus.Accepted);
        }

        private async Task<bool> HasProjectAccessAsync(int projectId)
        {
            var userId = CurrentUserId();
            if (string.IsNullOrEmpty(userId))
                return false;

            return await IsProjectMemberOrCreatorAsync(projectId, userId);
        }

        private string? CurrentUserId() => User.FindFirstValue(ClaimTypes.NameIdentifier);

        private static string DisplayName(User? user)
        {
            if (user == null)
                return "—";

            if (!string.IsNullOrWhiteSpace(user.FullName))
                return user.FullName;

            return user.UserName ?? user.Email ?? "کاربر";
        }

        private static string? NullIfWhiteSpace(string? value) =>
            string.IsNullOrWhiteSpace(value) ? null : value.Trim();
    }
}
