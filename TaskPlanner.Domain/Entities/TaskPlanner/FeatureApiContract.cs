using System.ComponentModel.DataAnnotations;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    /// <summary>
    /// قرارداد API بین Frontend و Backend برای یک فیچر
    /// </summary>
    public class FeatureApiContract
    {
        public int Id { get; set; }

        public int FeatureId { get; set; }
        public ProjectFeature Feature { get; set; } = null!;

        [Required(ErrorMessage = "Endpoint الزامی است")]
        [StringLength(500)]
        public string Endpoint { get; set; } = null!;

        [Required(ErrorMessage = "متد HTTP الزامی است")]
        [StringLength(20)]
        public string HttpMethod { get; set; } = "GET";

        [StringLength(4000)]
        public string? RequestDescription { get; set; }

        [StringLength(4000)]
        public string? ResponseDescription { get; set; }

        [StringLength(4000)]
        public string? ErrorResponseDescription { get; set; }

        /// <summary>
        /// کدهای وضعیت مورد انتظار (مثلاً 200, 400, 401, 404)
        /// </summary>
        [StringLength(200)]
        public string? StatusCodes { get; set; }

        [StringLength(1000)]
        public string? Pagination { get; set; }

        [StringLength(1000)]
        public string? Filter { get; set; }

        [StringLength(1000)]
        public string? Sort { get; set; }

        /// <summary>
        /// نام و نوع فیلدها، Nullable یا Required بودن
        /// </summary>
        [StringLength(4000)]
        public string? FieldsDescription { get; set; }

        public int SortOrder { get; set; }
    }
}
