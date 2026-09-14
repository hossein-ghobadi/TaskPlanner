using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Calculation;

namespace Endpoint.Site.Models
{
    public class WoodCostTestVm
    {
        [Display(Name = "دسته محاسباتی")]
        public CalculationCategory Category { get; set; } = CalculationCategory.VacuumIronRing;

        [Display(Name = "مساحت مصرفی چوب")]
        public decimal WoodConsumedArea { get; set; }

        [Display(Name = "پرت چوب")]
        public decimal WoodWaste { get; set; }

        [Display(Name = "فی چوب")]
        public decimal WoodUnitPrice { get; set; }

        public bool HasResult { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }

        public string? CategoryDisplayName { get; set; }
        public bool Applies { get; set; }
        public string? RuleDescription { get; set; }
        public string? Formula { get; set; }
        public string? FormulaWithValues { get; set; }
        public decimal WoodCost { get; set; }

        public IReadOnlyList<CalculationCategory> AllCategories { get; set; } =
            Enum.GetValues<CalculationCategory>();
    }
}
