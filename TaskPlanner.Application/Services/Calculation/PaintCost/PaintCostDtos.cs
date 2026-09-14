using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.PaintCost
{
    public class PaintCostInputDto
    {
        public CalculationCategory Category { get; set; }

        /// <summary>مساحت مصرفی لایه</summary>
        public decimal LayerConsumedArea { get; set; }

        /// <summary>طول حقیقی PVC</summary>
        public decimal ActualPvcLength { get; set; }

        /// <summary>سایز لبه</summary>
        public decimal EdgeSize { get; set; }

        /// <summary>فی رنگ</summary>
        public decimal PaintUnitPrice { get; set; }
    }

    public class PaintCostResultDto
    {
        public CalculationCategory Category { get; set; }
        public string CategoryDisplayName { get; set; } = string.Empty;
        public bool Applies { get; set; }
        public string RuleDescription { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public string FormulaWithValues { get; set; } = string.Empty;
        public decimal PaintCost { get; set; }
    }
}
