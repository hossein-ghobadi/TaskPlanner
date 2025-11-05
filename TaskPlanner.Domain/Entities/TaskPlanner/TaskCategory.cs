using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    public class TaskCategory
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام دسته‌بندی الزامی است")]
        public string Name { get; set; }

        // ارتباط با پروژه - هر دسته‌بندی متعلق به یک پروژه است
        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; }

        // کاربر ایجادکننده
        [Required]
        public string CreatorUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
