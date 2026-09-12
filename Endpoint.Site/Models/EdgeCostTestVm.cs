using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Calculation;

namespace Endpoint.Site.Models
{
    public class EdgeCostTestVm
    {
        [Display(Name = "دسته محاسباتی")]
        public CalculationCategory Category { get; set; } = CalculationCategory.Chanelium;

        [Display(Name = "پانچ لبه")]
        public bool EnablePunch { get; set; }

        [Display(Name = "فی لبه")]
        public decimal EdgeUnitPrice { get; set; }

        [Display(Name = "فی پانچ")]
        public decimal PunchUnitPrice { get; set; }

        [Display(Name = "فی دستمزد")]
        public decimal LaborUnitPrice { get; set; }

        public List<EdgeLetterVm> Letters { get; set; } = new()
        {
            new() { Label = "حرف 1", Length = 0 }
        };

        public List<EdgeDifficultyBandVm> DifficultyBands { get; set; } = new()
        {
            new() { MinLength = 0, MaxLength = 20, Coefficient = 1 },
            new() { MinLength = 20, MaxLength = 40, Coefficient = 1.2m },
            new() { MinLength = 40, MaxLength = 100, Coefficient = 1.5m }
        };

        public bool HasResult { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }

        public string? CategoryDisplayName { get; set; }
        public bool Applies { get; set; }
        public bool IncludesPunch { get; set; }
        public string? RuleDescription { get; set; }
        public decimal ActualPvcLengthResult { get; set; }
        public decimal RelativePvcLength { get; set; }
        public List<EdgeLetterContributionVm> LetterContributions { get; set; } = new();
        public EdgeCostComponentVm? EdgeMaterial { get; set; }
        public EdgeCostComponentVm? EdgePunch { get; set; }
        public EdgeCostComponentVm? EdgeLabor { get; set; }
        public decimal TotalEdgeCost { get; set; }

        public IReadOnlyList<CalculationCategory> AllCategories { get; set; } =
            Enum.GetValues<CalculationCategory>();
    }

    public class EdgeLetterVm
    {
        [Display(Name = "نام حرف")]
        public string? Label { get; set; }

        [Display(Name = "طول")]
        public decimal Length { get; set; }
    }

    public class EdgeDifficultyBandVm
    {
        [Display(Name = "از طول")]
        public decimal MinLength { get; set; }

        [Display(Name = "تا طول")]
        public decimal MaxLength { get; set; }

        [Display(Name = "ضریب سختی")]
        public decimal Coefficient { get; set; }
    }

    public class EdgeLetterContributionVm
    {
        public int Index { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal Length { get; set; }
        public decimal MinLength { get; set; }
        public decimal MaxLength { get; set; }
        public decimal Coefficient { get; set; }
        public decimal Contribution { get; set; }
        public string FormulaWithValues { get; set; } = string.Empty;
    }

    public class EdgeCostComponentVm
    {
        public string Name { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public string FormulaWithValues { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
