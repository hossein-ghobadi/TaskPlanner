using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.CutSpecificCost
{
    public class CutSpecificCostInputDto
    {
        public CalculationCategory Category { get; set; }

        /// <summary>طول حقیقی PVC</summary>
        public decimal ActualPvcLength { get; set; }

        /// <summary>فی برش</summary>
        public decimal CutUnitPrice { get; set; }

        /// <summary>ضریب چندلایه بودن</summary>
        public decimal MultiLayerCoefficient { get; set; }
    }

    public class CutSpecificCostResultDto
    {
        public CalculationCategory Category { get; set; }
        public string CategoryDisplayName { get; set; } = string.Empty;
        public bool Applies { get; set; }
        public string RuleDescription { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public string FormulaWithValues { get; set; } = string.Empty;
        public decimal CutCost { get; set; }
    }
}
