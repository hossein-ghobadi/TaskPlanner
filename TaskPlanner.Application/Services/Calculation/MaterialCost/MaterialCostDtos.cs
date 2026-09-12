using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.MaterialCost
{
    public class MaterialCostInputDto
    {
        public CalculationCategory Category { get; set; }
        public LayerMode LayerMode { get; set; } = LayerMode.Single;

        /// <summary>
        /// فقط وقتی true باشد و دسته برش نباشد، پانچ در محاسبه لحاظ می‌شود.
        /// </summary>
        public bool EnablePunch { get; set; }

        public decimal Layer1ConsumedArea { get; set; }
        public decimal Layer1UnitPrice { get; set; }
        public decimal Layer1Waste { get; set; }
        public decimal Layer1ActualPvcArea { get; set; }
        public decimal Layer1PunchUnitPrice { get; set; }

        public decimal Layer2ConsumedArea { get; set; }
        public decimal Layer2UnitPrice { get; set; }
        public decimal Layer2Waste { get; set; }
        public decimal Layer2ActualPvcArea { get; set; }
        public decimal Layer2PunchUnitPrice { get; set; }
    }

    public class MaterialCostLayerResultDto
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

    public class MaterialCostResultDto
    {
        public CalculationCategory Category { get; set; }
        public string CategoryDisplayName { get; set; } = string.Empty;
        public LayerMode LayerMode { get; set; }
        public string LayerModeDisplayName { get; set; } = string.Empty;
        public bool IncludesWaste { get; set; }
        public bool IncludesPunch { get; set; }
        public string RuleDescription { get; set; } = string.Empty;
        public List<MaterialCostLayerResultDto> Layers { get; set; } = new();
        public decimal TotalSheetMaterialCost { get; set; }
        public decimal TotalPunchCost { get; set; }
        public decimal TotalMaterialCost { get; set; }
    }
}
