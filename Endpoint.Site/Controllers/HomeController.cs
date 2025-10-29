using Microsoft.AspNetCore.Mvc;

namespace Endpoint.Site.Areas.TaskPlanner.Controllers
{

    [Area("TaskPlanner")]
    [Route("TaskPlanner/[controller]/[action]")]
    public class HomeController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
