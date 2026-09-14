using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.PvcCost
{
    public class PvcCostInputDto
    {
        public CalculationCategory Category { get; set; }

        /// <summary>مساحت مصرفی PVC</summary>
        public decimal PvcConsumedArea { get; set; }

        /// <summary>مساحت مصرفی بک‌لایت</summary>
        public decimal BacklightConsumedArea { get; set; }

        /// <summary>فی PVC</summary>
        public decimal PvcUnitPrice { get; set; }
    }

    public class PvcCostComponentDto
    {
        public string Name { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public string FormulaWithValues { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class PvcCostResultDto
    {
        public CalculationCategory Category { get; set; }
        public string CategoryDisplayName { get; set; } = string.Empty;
        public bool Applies { get; set; }
        public string RuleDescription { get; set; } = string.Empty;

        public PvcCostComponentDto PvcCost { get; set; } = new();
        public PvcCostComponentDto BacklightPvcCost { get; set; } = new();
        public decimal TotalPvcCost { get; set; }
    }
}
