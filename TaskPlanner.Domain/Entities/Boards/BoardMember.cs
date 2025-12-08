using System;
using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Users;

namespace TaskPlanner.Domain.Entities.Boards
{
    /// <summary>
    /// عضو تخته - برای دعوت کاربران به تخته
    /// </summary>
    public class BoardMember
    {
        public int Id { get; set; }

        [Required]
        public int BoardId { get; set; }
        public Board Board { get; set; } = null!;

        [Required]
        [StringLength(450)]
        public string UserId { get; set; } = null!;

        /// <summary>
        /// تاریخ دعوت
        /// </summary>
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;
    }
}

