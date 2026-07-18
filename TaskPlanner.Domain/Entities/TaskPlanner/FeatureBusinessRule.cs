using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// قانون کسب‌وکار فیچر — رفتار سیستم در شرایط مختلف
    /// </summary>
    public class FeatureBusinessRule
    {
        public int Id { get; set; }

        public int FeatureId { get; set; }
        public ProjectFeature Feature { get; set; } = null!;

        [Required(ErrorMessage = "توضیح قانون الزامی است")]
        [StringLength(2000)]
        public string Description { get; set; } = null!;

        public int SortOrder { get; set; }
    }
}
