using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    public class ProjectInvitation
    {
        public int Id { get; set; }

        public int? ProjectId { get; set; }
        public Project? Project { get; set; } = null!;

        // کاربر دعوت‌کننده
        public string InviterId { get; set; } = null!;

        // کاربر دعوت‌شده
        public string InviteeId { get; set; } = null!;
        public string InviteePhone { get; set; } = null!; // شماره‌ای که وارد شده

        // وضعیت دعوت (در انتظار، پذیرفته، رد شده)
        public InvitationStatus Status { get; set; } = InvitationStatus.Pending;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? RespondedAt { get; set; }
    }

    public enum InvitationStatus
    {
        Pending,
        Accepted,
        Rejected
    }


}
