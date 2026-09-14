using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Calculation;

namespace Endpoint.Site.Models
{
    public class GlueCostTestVm
    {
        [Display(Name = "دسته محاسباتی")]
        public CalculationCategory Category { get; set; } = CalculationCategory.Chanelium;

        [Display(Name = "مساحت مصرفی لایه اول")]
        public decimal Layer1ConsumedArea { get; set; }

        [Display(Name = "مساحت مصرفی لایه دوم")]
        public decimal Layer2ConsumedArea { get; set; }

        [Display(Name = "طول حقیقی PVC")]
        public decimal ActualPvcLength { get; set; }

        [Display(Name = "فی چسب")]
        public decimal GlueUnitPrice { get; set; }

        public bool HasResult { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }

        public string? CategoryDisplayName { get; set; }
        public bool Applies { get; set; }
        public bool UsesLayerAreas { get; set; }
        public string? RuleDescription { get; set; }
        public string? Formula { get; set; }
        public string? FormulaWithValues { get; set; }
        public decimal GlueCost { get; set; }

        public IReadOnlyList<CalculationCategory> AllCategories { get; set; } =
            Enum.GetValues<CalculationCategory>();
    }
}
