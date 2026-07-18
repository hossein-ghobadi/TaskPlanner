using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Users;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// فیچر پروژه — مشخصات قبل از شروع توسعه (کارکردها، وضعیت‌های صفحه، قرارداد API، قوانین کسب‌وکار)
    /// </summary>
    public class ProjectFeature
    {
        public int Id { get; set; }

        [Required]
        public int ProjectId { get; set; }
        public Project Project { get; set; } = null!;

        [Required(ErrorMessage = "نام فیچر الزامی است")]
        [StringLength(200)]
        public string Name { get; set; } = null!;

        [StringLength(2000)]
        public string? Description { get; set; }

        /// <summary>
        /// مسئول Code Review — می‌تواند امتیاز بازبینی را به اعضای پروژه بدهد
        /// </summary>
        [StringLength(450)]
        public string? CodeReviewerUserId { get; set; }
        public User? CodeReviewerUser { get; set; }

        [StringLength(450)]
        public string? CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<FeatureFunction> Functions { get; set; } = new List<FeatureFunction>();
        public ICollection<FeaturePageState> PageStates { get; set; } = new List<FeaturePageState>();
        public ICollection<FeatureApiContract> ApiContracts { get; set; } = new List<FeatureApiContract>();
        public ICollection<FeatureBusinessRule> BusinessRules { get; set; } = new List<FeatureBusinessRule>();
        public ICollection<FeatureCodeReview> CodeReviews { get; set; } = new List<FeatureCodeReview>();
        public ICollection<TaskItem> Tasks { get; set; } = new List<TaskItem>();
    }
}
