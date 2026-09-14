using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Calculation;

namespace Endpoint.Site.Models
{
    public class PaintCostTestVm
    {
        [Display(Name = "دسته محاسباتی")]
        public CalculationCategory Category { get; set; } = CalculationCategory.SimpleIron;

        [Display(Name = "مساحت مصرفی لایه")]
        public decimal LayerConsumedArea { get; set; }

        [Display(Name = "طول حقیقی PVC")]
        public decimal ActualPvcLength { get; set; }

        [Display(Name = "سایز لبه")]
        public decimal EdgeSize { get; set; }

        [Display(Name = "فی رنگ")]
        public decimal PaintUnitPrice { get; set; }

        public bool HasResult { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }

        public string? CategoryDisplayName { get; set; }
        public bool Applies { get; set; }
        public string? RuleDescription { get; set; }
        public string? Formula { get; set; }
        public string? FormulaWithValues { get; set; }
        public decimal PaintCost { get; set; }

        public IReadOnlyList<CalculationCategory> PaintCategories { get; set; } =
            Enum.GetValues<CalculationCategory>().Where(c => c.CanHavePaintCost()).ToArray();

        public IReadOnlyList<CalculationCategory> AllCategories { get; set; } =
            Enum.GetValues<CalculationCategory>();
    }
}
