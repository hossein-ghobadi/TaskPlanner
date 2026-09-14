using System.Security.Claims;
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

            // یک round-trip به‌جای چند Count جداگانه
            var sidebarData = await _context.Projects.AsNoTracking()
                .Where(p => p.Id == projectId &&
                    (p.CreatorUserId == userId || p.Members.Any(m => m.UserId == userId)))
                .Select(p => new
                {
                    p.CreatorUserId,
                    TotalTasks = _context.TaskItems.Count(t =>
                        t.ProjectId == projectId
                        && (t.IssueType == IssueType.Story
                            || t.IssueType == IssueType.Task
                            || t.IssueType == IssueType.Bug)),
                    NotesCount = _context.ProjectNotes.Count(n => n.ProjectId == projectId),
                    ImagesCount = _context.ProjectImageGalleries.Count(img => img.ProjectId == projectId),
                    FeaturesCount = _context.ProjectFeatures.Count(f => f.ProjectId == projectId),
                    TicketsCount = _context.ProjectTickets.Count(t => t.ProjectId == projectId),
                    MembersCount = _context.ProjectMembers.Count(m => m.ProjectId == projectId),
                    CreatorInMembers = _context.ProjectMembers.Any(m =>
                        m.ProjectId == projectId && m.UserId == p.CreatorUserId),
                    PendingInvitesCount = _context.ProjectInvitations.Count(i =>
                        i.ProjectId == projectId && i.Status == InvitationStatus.Pending)
                })
                .FirstOrDefaultAsync();

            if (sidebarData == null)
            {
                return Content(string.Empty);
            }

            var isCreator = string.Equals(sidebarData.CreatorUserId, userId, StringComparison.Ordinal);
            if (ViewContext.ViewBag.ProjectSidebarIsCreator is bool flag)
            {
                isCreator = flag;
            }

            var path = ViewContext.HttpContext.Request.Path.Value ?? "";
            var onDetails = path.Contains("/Projects/Details", StringComparison.OrdinalIgnoreCase);
            var memberCount = sidebarData.MembersCount + (sidebarData.CreatorInMembers ? 0 : 1);

            var vm = new ProjectSidebarVm
            {
                ProjectId = projectId,
                IsCreator = isCreator,
                OnProjectDetailsPage = onDetails,
                ActiveNav = DetectActiveNav(path),
                TotalTasks = sidebarData.TotalTasks,
                FeaturesCount = sidebarData.FeaturesCount,
                TicketsCount = sidebarData.TicketsCount,
                NotesCount = sidebarData.NotesCount,
                ImagesCount = sidebarData.ImagesCount,
                PendingInvitesCount = isCreator ? sidebarData.PendingInvitesCount : null,
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
            if (p.Contains("/projects/settings"))
            {
                return "settings";
            }
            if (p.Contains("/projects/edit"))
            {
                return "settings";
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
