using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Calculation;

namespace Endpoint.Site.Models
{
    public class VacuumWageCostTestVm
    {
        [Display(Name = "دسته محاسباتی")]
        public CalculationCategory Category { get; set; } = CalculationCategory.VacuumIronRing;

        [Display(Name = "سایز لبه")]
        public decimal EdgeSize { get; set; }

        [Display(Name = "فی وکیوم")]
        public decimal VacuumUnitPrice { get; set; }

        public bool HasResult { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }

        public string? CategoryDisplayName { get; set; }
        public bool Applies { get; set; }
        public string? RuleDescription { get; set; }
        public string? Formula { get; set; }
        public string? FormulaWithValues { get; set; }
        public decimal VacuumWageCost { get; set; }

        public IReadOnlyList<CalculationCategory> AllCategories { get; set; } =
            Enum.GetValues<CalculationCategory>();
    }
}
