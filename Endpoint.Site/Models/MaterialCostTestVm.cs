using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Calculation;

namespace Endpoint.Site.Models
{
    public class MaterialCostTestVm
    {
        [Display(Name = "دسته محاسباتی")]
        public CalculationCategory Category { get; set; } = CalculationCategory.Chanelium;

        [Display(Name = "تعداد لایه")]
        public LayerMode LayerMode { get; set; } = LayerMode.Single;

        [Display(Name = "پانچ متریال")]
        public bool EnablePunch { get; set; }

        [Display(Name = "مساحت مصرفی لایه اول")]
        public decimal Layer1ConsumedArea { get; set; }

        [Display(Name = "فی لایه اول")]
        public decimal Layer1UnitPrice { get; set; }

        [Display(Name = "پرتی لایه اول")]
        public decimal Layer1Waste { get; set; }

        [Display(Name = "مساحت حقیقی PVC لایه اول")]
        public decimal Layer1ActualPvcArea { get; set; }

        [Display(Name = "فی پانچ لایه اول")]
        public decimal Layer1PunchUnitPrice { get; set; }

        [Display(Name = "مساحت مصرفی لایه دوم")]
        public decimal Layer2ConsumedArea { get; set; }

        [Display(Name = "فی لایه دوم")]
        public decimal Layer2UnitPrice { get; set; }

        [Display(Name = "پرتی لایه دوم")]
        public decimal Layer2Waste { get; set; }

        [Display(Name = "مساحت حقیقی PVC لایه دوم")]
        public decimal Layer2ActualPvcArea { get; set; }

        [Display(Name = "فی پانچ لایه دوم")]
        public decimal Layer2PunchUnitPrice { get; set; }

        public bool HasResult { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }

        public string? CategoryDisplayName { get; set; }
        public string? LayerModeDisplayName { get; set; }
        public bool IncludesWaste { get; set; }
        public bool IncludesPunch { get; set; }
        public string? RuleDescription { get; set; }
        public List<MaterialCostLayerResultVm> Layers { get; set; } = new();
        public decimal TotalSheetMaterialCost { get; set; }
        public decimal TotalPunchCost { get; set; }
        public decimal TotalMaterialCost { get; set; }

        public IReadOnlyList<CalculationCategory> AllCategories { get; set; } =
            Enum.GetValues<CalculationCategory>();
    }

    public class MaterialCostLayerResultVm
    {
        public int LayerNumber { get; set; }
        public decimal ConsumedArea { get; set; }
        public decimal Waste { get; set; }
        public decimal BillableArea { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Cost { get; set; }
        public string Formula { get; set; } = string.Empty;
        public string FormulaWithValues { get; set; } = string.Empty;

        public bool HasPunch { get; set; }
        public decimal ActualPvcArea { get; set; }
        public decimal PunchUnitPrice { get; set; }
        public decimal PunchCost { get; set; }
        public string PunchFormula { get; set; } = string.Empty;
        public string PunchFormulaWithValues { get; set; } = string.Empty;
        public decimal LayerTotalCost { get; set; }
    }
}
