using Microsoft.AspNetCore.Mvc.Rendering;

namespace Endpoint.Site.Helpers
{
    public static class ProjectSidebarResolver
    {
        public static int? ResolveProjectId(ViewContext viewContext)
        {
            var vb = viewContext.ViewBag;
            if (vb.ProjectId is int pid)
            {
                return pid;
            }

            if (vb.SelectedProjectId is int sel)
            {
                return sel;
            }

            var q = viewContext.HttpContext.Request.Query["projectId"].FirstOrDefault();
            if (int.TryParse(q, out var qid))
            {
                return qid;
            }

            var rd = viewContext.RouteData.Values;
            var controller = rd["controller"]?.ToString();
            var action = rd["action"]?.ToString();
            if (string.Equals(controller, "Projects", StringComparison.OrdinalIgnoreCase)
                && int.TryParse(rd["id"]?.ToString(), out var routeId)
                && (string.Equals(action, "Details", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(action, "Edit", StringComparison.OrdinalIgnoreCase)
                    || string.Equals(action, "Delete", StringComparison.OrdinalIgnoreCase)))
            {
                return routeId;
            }

            return null;
        }
    }
}
