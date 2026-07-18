using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// کارکرد فیچر — کاربر در این بخش چه کارهایی می‌تواند انجام دهد
    /// </summary>
    public class FeatureFunction
    {
        public int Id { get; set; }

        public int FeatureId { get; set; }
        public ProjectFeature Feature { get; set; } = null!;

        [Required(ErrorMessage = "عنوان کارکرد الزامی است")]
        [StringLength(500)]
        public string Title { get; set; } = null!;

        public int SortOrder { get; set; }
    }
}
