using Endpoint.Site.Models.MindMap;
using Endpoint.Site.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Endpoint.Site.Controllers
{
    [Authorize]
    public class MindMapsController : Controller
    {
        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "مایندمپ‌ها";
            ViewData["UseFluidLayout"] = true;
            return View();
        }

        /// <summary>
        /// خروجی Word به‌صورت فهرست چندسطحی بر اساس سلسله‌مراتب نودها.
        /// </summary>
        [HttpPost]
        [IgnoreAntiforgeryToken]
        [Consumes("application/json")]
        public IActionResult ExportWord([FromBody] MindMapExportPayload? payload)
        {
            if (payload?.Nodes == null || payload.Nodes.Count == 0)
            {
                return BadRequest(new { error = "داده‌ای برای خروجی نیست." });
            }

            try
            {
                var bytes = MindMapWordExporter.BuildDocxBytes(payload);
                const string fileName = "mindmap.docx";
                return File(bytes, "application/vnd.openxmlformats-officedocument.wordprocessingml.document", fileName);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { error = ex.Message });
            }
        }
    }
}
