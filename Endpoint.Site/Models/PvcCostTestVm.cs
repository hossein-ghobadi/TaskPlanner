using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Calculation;

namespace Endpoint.Site.Models
{
    public class PvcCostTestVm
    {
        [Display(Name = "دسته محاسباتی")]
        public CalculationCategory Category { get; set; } = CalculationCategory.Chanelium;

        [Display(Name = "مساحت مصرفی PVC")]
        public decimal PvcConsumedArea { get; set; }

        [Display(Name = "مساحت مصرفی بک‌لایت")]
        public decimal BacklightConsumedArea { get; set; }

        [Display(Name = "فی PVC")]
        public decimal PvcUnitPrice { get; set; }

        public bool HasResult { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }

        public string? CategoryDisplayName { get; set; }
        public bool Applies { get; set; }
        public string? RuleDescription { get; set; }
        public PvcCostComponentVm? PvcCost { get; set; }
        public PvcCostComponentVm? BacklightPvcCost { get; set; }
        public decimal TotalPvcCost { get; set; }

        public IReadOnlyList<CalculationCategory> AllCategories { get; set; } =
            Enum.GetValues<CalculationCategory>();
    }

    public class PvcCostComponentVm
    {
        public string Name { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public string FormulaWithValues { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
