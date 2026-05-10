using System.Security.Claims;
using System.Text.Json;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.Application.Services.DashboardService;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly IDashboardService _dashboardService;

        public DashboardController(IDashboardService dashboardService)
        {
            _dashboardService = dashboardService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken cancellationToken)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Challenge();

            var model = await _dashboardService.GetUserDashboardAsync(userId, cancellationToken);

            var charts = new
            {
                priorities = model.TaskPrioritySlices,
                perProject = model.ProjectTaskDistributionSlices,
                leads = model.LeadPipelineSlices,
                dues = model.DueTasksNext7Days
            };
            ViewBag.ChartsJson = JsonSerializer.Serialize(charts, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });

            return View(model);
        }
    }
}
