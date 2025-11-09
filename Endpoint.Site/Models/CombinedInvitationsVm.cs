using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Models
{
    public class CombinedInvitationsVm
    {
        public List<ProjectInvitation> ProjectInvitations { get; set; } = new(); // دعوت‌های پروژه‌ای که کاربر دریافت کرده
        public List<ProjectInvitation> UserInvitations { get; set; } = new(); // دعوت‌های سیستمی که کاربر دریافت کرده
        public List<ProjectInvitation> SentProjectInvitations { get; set; } = new(); // دعوت‌های پروژه‌ای ارسال‌شده توسط کاربر
        public List<ProjectInvitation> SentSystemInvitations { get; set; } = new(); // دعوت‌های سیستمی ارسال‌شده توسط کاربر
    }
}
