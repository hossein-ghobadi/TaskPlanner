using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.WoodCost
{
    public class WoodCostInputDto
    {
        public CalculationCategory Category { get; set; }

        /// <summary>مساحت مصرفی چوب</summary>
        public decimal WoodConsumedArea { get; set; }

        /// <summary>پرت چوب</summary>
        public decimal WoodWaste { get; set; }

        /// <summary>فی چوب</summary>
        public decimal WoodUnitPrice { get; set; }
    }

    public class WoodCostResultDto
    {
        public CalculationCategory Category { get; set; }
        public string CategoryDisplayName { get; set; } = string.Empty;
        public bool Applies { get; set; }
        public string RuleDescription { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public string FormulaWithValues { get; set; } = string.Empty;
        public decimal WoodCost { get; set; }
    }
}
