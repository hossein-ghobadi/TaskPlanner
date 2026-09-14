using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.CrystalCost
{
    public class CrystalCostInputDto
    {
        public CalculationCategory Category { get; set; }

        /// <summary>مساحت حقیقی PVC</summary>
        public decimal ActualPvcArea { get; set; }

        /// <summary>فی کریستال</summary>
        public decimal CrystalUnitPrice { get; set; }
    }

    public class CrystalCostResultDto
    {
        public CalculationCategory Category { get; set; }
        public string CategoryDisplayName { get; set; } = string.Empty;
        public bool Applies { get; set; }
        public string RuleDescription { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public string FormulaWithValues { get; set; } = string.Empty;
        public decimal CrystalCost { get; set; }
    }
}
