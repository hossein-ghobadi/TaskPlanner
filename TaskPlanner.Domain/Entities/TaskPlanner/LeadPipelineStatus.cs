namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// مراحل لید در CRM سبک داخلی.
    /// </summary>
    public enum LeadPipelineStatus
    {
        New = 0,
        Contacted = 1,
        /// <summary>قطعیت / آماده تبدیل به پروژه</summary>
        Qualified = 2,
        Converted = 3,
        Lost = 4,
        /// <summary>اعلام قیمت به مشتری</summary>
        QuoteAnnounced = 5
    }
}
