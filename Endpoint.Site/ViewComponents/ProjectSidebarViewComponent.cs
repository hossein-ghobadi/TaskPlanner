using System.Security.Claims;
using Endpoint.Site.Helpers;
using Endpoint.Site.Models;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using TaskPlanner.Domain.Entities.TaskPlanner;
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

            var totalTasks = await _context.TaskItems.AsNoTracking()
                .CountAsync(t => t.ProjectId == projectId
                    && (t.IssueType == IssueType.Story || t.IssueType == IssueType.Task || t.IssueType == IssueType.Bug));
            var notesCount = await _context.ProjectNotes.AsNoTracking()
                .CountAsync(n => n.ProjectId == projectId);
            var imagesCount = await _context.ProjectImageGalleries.AsNoTracking()
                .CountAsync(img => img.ProjectId == projectId);
            var featuresCount = await _context.ProjectFeatures.AsNoTracking()
                .CountAsync(f => f.ProjectId == projectId);
            var ticketsCount = await _context.ProjectTickets.AsNoTracking()
                .CountAsync(t => t.ProjectId == projectId);
            var membersCount = await _context.ProjectMembers.AsNoTracking()
                .CountAsync(m => m.ProjectId == projectId);
            var creatorInMembers = await _context.ProjectMembers.AsNoTracking()
                .AnyAsync(m => m.ProjectId == projectId && m.UserId == creatorId);
            var memberCount = membersCount + (creatorInMembers ? 0 : 1);
            int? pendingInvitesCount = null;
            if (isCreator)
            {
                pendingInvitesCount = await _context.ProjectInvitations.AsNoTracking()
                    .CountAsync(i => i.ProjectId == projectId && i.Status == InvitationStatus.Pending);
            }

            var vm = new ProjectSidebarVm
            {
                ProjectId = projectId,
                IsCreator = isCreator,
                OnProjectDetailsPage = onDetails,
                ActiveNav = DetectActiveNav(path),
                TotalTasks = totalTasks,
                FeaturesCount = featuresCount,
                TicketsCount = ticketsCount,
                NotesCount = notesCount,
                ImagesCount = imagesCount,
                PendingInvitesCount = pendingInvitesCount,
                MemberCount = memberCount
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
            if (p.Contains("/features"))
            {
                return "features";
            }
            if (p.Contains("/tickets"))
            {
                return "tickets";
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
