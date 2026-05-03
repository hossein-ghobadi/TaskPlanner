using System.Security.Claims;
using Endpoint.Site.Helpers;
using Endpoint.Site.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskPlanner.Persistence.Contexts;

namespace Endpoint.Site.ViewComponents
{
    public class ProjectSidebarViewComponent : ViewComponent
    {
        private readonly MVPTestDatabaseContext _context;

        public ProjectSidebarViewComponent(MVPTestDatabaseContext context)
        {
            _context = context;
        }

        public async Task<IViewComponentResult> InvokeAsync(int projectId)
        {
            var userId = UserClaimsPrincipal.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
            {
                return Content(string.Empty);
            }

            var exists = await _context.Projects.AsNoTracking()
                .AnyAsync(p => p.Id == projectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)));
            if (!exists)
            {
                return Content(string.Empty);
            }

            var creatorId = await _context.Projects.AsNoTracking()
                .Where(p => p.Id == projectId)
                .Select(p => p.CreatorUserId)
                .FirstOrDefaultAsync();

            var isCreator = !string.IsNullOrEmpty(creatorId) &&
                string.Equals(creatorId, userId, StringComparison.Ordinal);
            if (ViewContext.ViewBag.ProjectSidebarIsCreator is bool flag)
            {
                isCreator = flag;
            }

            var path = ViewContext.HttpContext.Request.Path.Value ?? "";
            var onDetails = path.Contains("/Projects/Details", StringComparison.OrdinalIgnoreCase);

            var vm = new ProjectSidebarVm
            {
                ProjectId = projectId,
                IsCreator = isCreator,
                OnProjectDetailsPage = onDetails,
                ActiveNav = DetectActiveNav(path),
                TotalTasks = ViewContext.ViewBag.SidebarTotalTasks as int?
                    ?? ViewContext.ViewBag.TotalTasks as int?,
                NotesCount = ViewContext.ViewBag.NotesCount as int?
                    ?? ViewContext.ViewBag.SidebarNotesCount as int?,
                ImagesCount = ViewContext.ViewBag.ImagesCount as int?
                    ?? ViewContext.ViewBag.SidebarImagesCount as int?,
                PendingInvitesCount = ViewContext.ViewBag.SidebarPendingInvites as int?,
                MemberCount = ViewContext.ViewBag.SidebarMemberCount as int?
            };

            return View(vm);
        }

        private static string DetectActiveNav(string path)
        {
            var p = path.ToLowerInvariant();
            if (p.Contains("/projects/details"))
            {
                return "details";
            }
            if (p.Contains("/projects/edit"))
            {
                return "details";
            }
            if (p.Contains("/projects/issuetypes") || p.Contains("/projects/createissuetype") || p.Contains("/projects/editissuetype"))
            {
                return "issue-types";
            }
            if (p.Contains("/sprints/projectboard"))
            {
                return "board";
            }
            if (p.Contains("/sprints/board"))
            {
                return "board";
            }
            if (p.Contains("/sprints/index"))
            {
                return "sprints";
            }
            if (p.Contains("/projectnotes"))
            {
                return "notes";
            }
            if (p.Contains("/projectimagegalleries") || p.Contains("/projectimagegalleryfolders"))
            {
                return "gallery";
            }
            if (p.Contains("/categories"))
            {
                return "categories";
            }
            if (p.Contains("/workflowstatus"))
            {
                return "workflow";
            }
            if (p.Contains("/tasks/"))
            {
                return "tasks";
            }
            return "";
        }
    }
}
