using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Calculation;

namespace Endpoint.Site.Models
{
    public class CutSpecificCostTestVm
    {
        [Display(Name = "دسته محاسباتی")]
        public CalculationCategory Category { get; set; } = CalculationCategory.Cut;

        [Display(Name = "طول حقیقی PVC")]
        public decimal ActualPvcLength { get; set; }

        [Display(Name = "فی برش")]
        public decimal CutUnitPrice { get; set; }

        [Display(Name = "ضریب چندلایه بودن")]
        public decimal MultiLayerCoefficient { get; set; } = 1;

        public bool HasResult { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }

        public string? CategoryDisplayName { get; set; }
        public bool Applies { get; set; }
        public string? RuleDescription { get; set; }
        public string? Formula { get; set; }
        public string? FormulaWithValues { get; set; }
        public decimal CutCost { get; set; }

        public IReadOnlyList<CalculationCategory> AllCategories { get; set; } =
            Enum.GetValues<CalculationCategory>();
    }
}
