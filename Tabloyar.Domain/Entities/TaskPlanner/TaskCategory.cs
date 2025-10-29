using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    public class TaskCategory
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام دسته‌بندی الزامی است")]
        public string Name { get; set; }

        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
