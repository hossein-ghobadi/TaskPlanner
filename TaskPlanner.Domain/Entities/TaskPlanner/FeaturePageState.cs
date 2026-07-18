using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// وضعیت صفحه فیچر و رفتار مورد انتظار در آن حالت
    /// </summary>
    public class FeaturePageState
    {
        public int Id { get; set; }

        public int FeatureId { get; set; }
        public ProjectFeature Feature { get; set; } = null!;

        public FeaturePageStateType StateType { get; set; }

        /// <summary>
        /// رفتار سیستم در این وضعیت (طراحی جداگانه الزامی نیست)
        /// </summary>
        [Required(ErrorMessage = "توضیح رفتار الزامی است")]
        [StringLength(2000)]
        public string BehaviorDescription { get; set; } = null!;

        public bool HasSeparateDesign { get; set; }
    }

    public enum FeaturePageStateType
    {
        Loading = 1,
        Empty = 2,
        Error = 3,
        Success = 4,
        Disabled = 5,
        Unauthorized = 6
    }
}
