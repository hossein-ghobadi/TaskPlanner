using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    public class DesignsController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "صفحه طراحی";
            ViewData["UseFluidLayout"] = true;
            return View();
        }
    }
}
