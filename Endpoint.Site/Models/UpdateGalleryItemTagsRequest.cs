namespace Endpoint.Site.Models
{
    /// <summary>
    /// درخواست به‌روزرسانی تگ‌های آیتم گالری (تگ‌ها با کاما جدا)
    /// </summary>
    public class UpdateGalleryItemTagsRequest
    {
        public string? Tags { get; set; }
    }
}
