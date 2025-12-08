using TaskPlanner.Domain.Entities.Boards;

namespace Endpoint.Site.Models
{
    public class BoardDetailsVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        /// <summary>
        /// سازنده تخته
        /// </summary>
        public string CreatorUserId { get; set; } = null!;
        public string CreatorUserName { get; set; } = null!;

        /// <summary>
        /// اعضای تخته
        /// </summary>
        public List<BoardMemberInfo> Members { get; set; } = new();

        /// <summary>
        /// کارهای تخته گروه‌بندی شده بر اساس وضعیت
        /// </summary>
        public Dictionary<string, List<BoardTask>> TasksByStatus { get; set; } = new();

        /// <summary>
        /// تمام کارهای تخته
        /// </summary>
        public List<BoardTask> AllTasks { get; set; } = new();

        /// <summary>
        /// آمار تخته
        /// </summary>
        public int TotalTasks { get; set; }
        public int CompletedTasks { get; set; }

        /// <summary>
        /// وضعیت‌های تخته
        /// </summary>
        public List<BoardStatus> Statuses { get; set; } = new();
    }

    /// <summary>
    /// اطلاعات عضو تخته
    /// </summary>
    public class BoardMemberInfo
    {
        public string UserId { get; set; } = null!;
        public string DisplayName { get; set; } = null!;
    }
}

