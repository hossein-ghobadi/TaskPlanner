using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Calculation;

namespace Endpoint.Site.Models
{
    public class CrystalCostTestVm
    {
        [Display(Name = "دسته محاسباتی")]
        public CalculationCategory Category { get; set; } = CalculationCategory.Chanelium;

        [Display(Name = "مساحت حقیقی PVC")]
        public decimal ActualPvcArea { get; set; }

        [Display(Name = "فی کریستال")]
        public decimal CrystalUnitPrice { get; set; }

        public bool HasResult { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }

        public string? CategoryDisplayName { get; set; }
        public bool Applies { get; set; }
        public string? RuleDescription { get; set; }
        public string? Formula { get; set; }
        public string? FormulaWithValues { get; set; }
        public decimal CrystalCost { get; set; }

        public IReadOnlyList<CalculationCategory> AllCategories { get; set; } =
            Enum.GetValues<CalculationCategory>();
    }
}
