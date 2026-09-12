using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.Application.Services.MyWorkService;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    [Route("[controller]/[action]")]
    public class MyWorkController : Controller
    {
        private readonly IMyWorkService _myWorkService;

        public MyWorkController(IMyWorkService myWorkService)
        {
            _myWorkService = myWorkService;
        }

        [HttpGet]
        public async Task<IActionResult> Index(
            MyWorkScope scope = MyWorkScope.Assigned,
            int? projectId = null,
            bool includeCompleted = false,
            MyWorkDueFilter due = MyWorkDueFilter.All,
            CancellationToken cancellationToken = default)
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userId))
                return Challenge();

            ViewData["Title"] = "کارهای من";
            ViewData["UseFluidLayout"] = true;

            var model = await _myWorkService.GetMyWorkAsync(
                userId,
                new MyWorkQuery
                {
                    Scope = scope,
                    ProjectId = projectId,
                    IncludeCompleted = includeCompleted,
                    DueFilter = due
                },
                cancellationToken);

            return View(model);
        }
    }
}
