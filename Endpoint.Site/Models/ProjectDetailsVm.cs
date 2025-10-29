using TaskPlanner.Domain.Entities.TaskPlanner;

namespace Endpoint.Site.Areas.TaskPlanner.Models
{
    public class ProjectDetailsVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }

        // سازنده پروژه
        public string CreatorUserName { get; set; } = null!;

        // اعضای پروژه
        public List<string> MemberUserNames { get; set; } = new();

        // تسک‌ها
        public List<TaskItem> Tasks { get; set; } = new();
        public List<ProjectInvitationVm> Invitations { get; set; } = new();

        // اسپرینت فعال
        public int? ActiveSprintId { get; set; }
        public string? ActiveSprintName { get; set; }

    }

}
