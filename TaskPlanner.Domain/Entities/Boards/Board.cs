using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Users;

namespace TaskPlanner.Domain.Entities.Boards
{
    /// <summary>
    /// تخته کاربر - مشابه Trello
    /// </summary>
    public class Board
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام تخته الزامی است")]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        /// <summary>
        /// کاربر ایجادکننده تخته
        /// </summary>
        [Required]
        [StringLength(450)]
        public string CreatorUserId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// اعضای تخته
        /// </summary>
        public ICollection<BoardMember> Members { get; set; } = new List<BoardMember>();

        /// <summary>
        /// کارهای تخته
        /// </summary>
        public ICollection<BoardTask> Tasks { get; set; } = new List<BoardTask>();

        /// <summary>
        /// وضعیت‌های تخته
        /// </summary>
        public ICollection<BoardStatus> Statuses { get; set; } = new List<BoardStatus>();
    }
}

