using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.VacuumWageCost
{
    public class VacuumWageCostInputDto
    {
        public CalculationCategory Category { get; set; }

        /// <summary>سایز لبه</summary>
        public decimal EdgeSize { get; set; }

        /// <summary>فی وکیوم</summary>
        public decimal VacuumUnitPrice { get; set; }
    }

    public class VacuumWageCostResultDto
    {
        public CalculationCategory Category { get; set; }
        public string CategoryDisplayName { get; set; } = string.Empty;
        public bool Applies { get; set; }
        public string RuleDescription { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public string FormulaWithValues { get; set; } = string.Empty;
        public decimal VacuumWageCost { get; set; }
    }
}
