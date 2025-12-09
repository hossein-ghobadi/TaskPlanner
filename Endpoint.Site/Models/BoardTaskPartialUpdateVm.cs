using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    /// <summary>
    /// ViewModel برای به‌روزرسانی جزئی کارها (مثلا فقط Status)
    /// استفاده در API های AJAX
    /// </summary>
    public class BoardTaskPartialUpdateVm
    {
        [Required]
        public int Id { get; set; }

        [StringLength(500)]
        public string? Title { get; set; }

        public string? Description { get; set; }

        [StringLength(50)]
        public string? Status { get; set; }

        public string? AssignedUserId { get; set; }

        public DateTime? DueDate { get; set; }

        public int? Order { get; set; }
    }
}

