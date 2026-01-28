using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// پوشه برای سازماندهی یادداشت‌های شخصی
    /// </summary>
    public class PersonalNoteFolder
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "نام پوشه الزامی است")]
        [MaxLength(200, ErrorMessage = "نام پوشه نمی‌تواند بیشتر از 200 کاراکتر باشد")]
        public string Name { get; set; } = null!;

        // پوشه والد (برای پوشه‌های تودرتو)
        public int? ParentFolderId { get; set; }
        public PersonalNoteFolder? ParentFolder { get; set; }

        // پوشه‌های فرزند
        public ICollection<PersonalNoteFolder> Children { get; set; } = new List<PersonalNoteFolder>();

        // یادداشت‌های موجود در این پوشه
        public ICollection<PersonalNote> Notes { get; set; } = new List<PersonalNote>();

        // کاربر مالک پوشه
        [Required]
        public string UserId { get; set; } = null!;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // رنگ پوشه (اختیاری)
        public string? Color { get; set; }

        // ترتیب نمایش
        public int Order { get; set; } = 0;
    }
}

