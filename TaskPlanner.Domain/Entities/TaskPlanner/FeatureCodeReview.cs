using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Users;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// ثبت امتیاز Code Review برای یک عضو پروژه توسط مسئول بازبینی فیچر
    /// </summary>
    public class FeatureCodeReview
    {
        public int Id { get; set; }

        public int FeatureId { get; set; }
        public ProjectFeature Feature { get; set; } = null!;

        /// <summary>
        /// کسی که امتیاز را ثبت کرده (معمولاً مسئول Code Review فیچر)
        /// </summary>
        [Required]
        [StringLength(450)]
        public string ReviewerUserId { get; set; } = null!;
        public User? ReviewerUser { get; set; }

        /// <summary>
        /// عضو پروژه‌ای که امتیاز را دریافت کرده
        /// </summary>
        [Required]
        [StringLength(450)]
        public string RevieweeUserId { get; set; } = null!;
        public User? RevieweeUser { get; set; }

        [Range(0, 100, ErrorMessage = "امتیاز باید بین ۰ تا ۱۰۰ باشد")]
        public int Score { get; set; }

        [StringLength(2000)]
        public string? Comment { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
