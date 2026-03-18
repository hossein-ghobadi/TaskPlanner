using Microsoft.AspNetCore.Mvc;

namespace Endpoint.Site.Controllers
{
    public class SvgController : Controller
    {
        private readonly IWebHostEnvironment _env;
        private readonly HttpClient _httpClient;

        // در اینجا HttpClient بدون BaseAddress ساخته می‌شود، چون می‌خواهیم از URLهای دلخواه و مطلق استفاده کنیم.
        public SvgController(IWebHostEnvironment env, IHttpClientFactory clientFactory)
        {
            _env = env;
            _httpClient = clientFactory.CreateClient(); // بدون تنظیم BaseAddress
        }

        public async Task<IActionResult> Index(string svgUrl = null, string fileName = "test.svg")
        {
            string svgContent = null;

            if (!string.IsNullOrEmpty(svgUrl))
            {
                // --- این قسمت اضافه شده تا مطمئن شویم URL مطلق است ---
                if (!Uri.IsWellFormedUriString(svgUrl, UriKind.Absolute))
                {
                    // اگر URL ارائه شده مطلق نبود، یک خطای BadRequest برگردان.
                    return BadRequest($"آدرس SVG ارائه شده مطلق نیست. لطفاً یک URL کامل (شامل http:// یا https://) وارد کنید. آدرس وارد شده: {svgUrl}");
                }
                // -------------------------------------------------------

                try
                {
                    // اکنون که از مطلق بودن URL مطمئنیم، آن را با HttpClient دریافت می‌کنیم.
                    HttpResponseMessage response = await _httpClient.GetAsync(svgUrl);
                    response.EnsureSuccessStatusCode(); // بررسی وضعیت پاسخ HTTP
                    svgContent = await response.Content.ReadAsStringAsync();
                }
                catch (HttpRequestException e)
                {
                    // اگر در دریافت محتوا مشکلی بود، خطای مربوطه را برگردان.
                    return BadRequest($"خطا در دریافت SVG از URL '{svgUrl}': {e.Message}");
                }
            }
            else
            {
                // اگر svgUrl ارائه نشده بود، از fileName برای خواندن فایل محلی استفاده کن.
                var filePath = Path.Combine(_env.WebRootPath, "Assets", fileName);
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound($"فایل SVG پیش‌فرض یا با نام '{fileName}' در پوشه Assets یافت نشد.");
                }
                svgContent = System.IO.File.ReadAllText(filePath);
            }

            // محتوای SVG را به View منتقل کن.
            ViewBag.SvgContent = svgContent;
            // View مربوطه را نمایش بده (مثلاً SvgViewer.cshtml).
            return View();
        }
    }
}
