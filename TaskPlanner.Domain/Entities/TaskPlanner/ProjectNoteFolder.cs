using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// پوشه برای سازماندهی یادداشت‌های پروژه
    /// </summary>
    public class ProjectNoteFolder
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام پوشه الزامی است")]
        [MaxLength(200, ErrorMessage = "نام پوشه نمی‌تواند بیشتر از 200 کاراکتر باشد")]
        public string Name { get; set; } = null!;

        // پوشه والد (برای پوشه‌های تودرتو)
        public int? ParentFolderId { get; set; }
        public ProjectNoteFolder? ParentFolder { get; set; }

        // پوشه‌های فرزند
        public ICollection<ProjectNoteFolder> Children { get; set; } = new List<ProjectNoteFolder>();

        // یادداشت‌های موجود در این پوشه
        public ICollection<ProjectNote> Notes { get; set; } = new List<ProjectNote>();

        // ارتباط با پروژه - هر پوشه متعلق به یک پروژه است
        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        // کاربر ایجادکننده پوشه
        [Required]
        public string CreatorUserId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // رنگ پوشه (اختیاری)
        public string? Color { get; set; }

        // ترتیب نمایش
        public int Order { get; set; } = 0;
    }
}

