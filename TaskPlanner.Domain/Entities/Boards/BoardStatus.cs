using System;
using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.Boards
{
    /// <summary>
    /// وضعیت‌های تخته - هر تخته می‌تواند وضعیت‌های سفارشی خودش را داشته باشد
    /// </summary>
    public class BoardStatus
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام وضعیت الزامی است")]
        [StringLength(100)]
        public string Name { get; set; } = null!;

        public string? Description { get; set; }

        /// <summary>
        /// رنگ نمایش وضعیت (برای UI)
        /// </summary>
        [StringLength(20)]
        public string? Color { get; set; }

        /// <summary>
        /// ترتیب نمایش
        /// </summary>
        public int Order { get; set; }

        [Required]
        public int BoardId { get; set; }
        public Board Board { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>
        /// کارهایی که در این وضعیت هستند
        /// </summary>
        public ICollection<BoardTask> Tasks { get; set; } = new List<BoardTask>();
    }
}

