using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Calculation;

namespace Endpoint.Site.Models
{
    public class MetalsCostTestVm
    {
        [Display(Name = "دسته محاسباتی")]
        public CalculationCategory Category { get; set; } = CalculationCategory.SimpleIron;

        [Display(Name = "طول حقیقی PVC")]
        public decimal ActualPvcLength { get; set; }

        [Display(Name = "فی پایه")]
        public decimal BaseUnitPrice { get; set; }

        [Display(Name = "مساحت مصرفی دوغی")]
        public decimal DoughyConsumedArea { get; set; }

        [Display(Name = "فی دوغی")]
        public decimal DoughyUnitPrice { get; set; }

        [Display(Name = "دستمزد دوغی")]
        public decimal DoughyLaborUnitPrice { get; set; }

        [Display(Name = "سایز لبه")]
        public decimal EdgeSize { get; set; }

        [Display(Name = "فی لبه پلاستیک")]
        public decimal PlasticEdgeUnitPrice { get; set; }

        [Display(Name = "فی مونتاژ")]
        public decimal AssemblyUnitPrice { get; set; }

        [Display(Name = "فی لبه")]
        public decimal RingEdgeUnitPrice { get; set; }

        [Display(Name = "فی دستمزد دو لبه رینگ")]
        public decimal DoubleEdgeRingLaborUnitPrice { get; set; }

        public bool HasResult { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }

        public string? CategoryDisplayName { get; set; }
        public bool Applies { get; set; }
        public bool IncludesDoubleEdgeRing { get; set; }
        public string? RuleDescription { get; set; }
        public decimal ActualPvcLengthResult { get; set; }

        public MetalsCostComponentVm? BaseCost { get; set; }
        public MetalsCostComponentVm? DoughyCost { get; set; }
        public MetalsCostComponentVm? MetalPlasticCost { get; set; }
        public MetalsCostComponentVm? AssemblyLaborCost { get; set; }
        public MetalsCostComponentVm? DoubleEdgeRingCost { get; set; }
        public decimal TotalMetalsCost { get; set; }

        public IReadOnlyList<CalculationCategory> MetalsCategories { get; set; } =
            Enum.GetValues<CalculationCategory>().Where(c => c.CanHaveMetalsCost()).ToArray();
    }

    public class MetalsCostComponentVm
    {
        public string Name { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public string FormulaWithValues { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
