using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.EdgeCost
{
    public class EdgeCostInputDto
    {
        public CalculationCategory Category { get; set; }

        /// <summary>فقط وقتی true باشد، پانچ لبه در محاسبه لحاظ می‌شود.</summary>
        public bool EnablePunch { get; set; }

        /// <summary>فی لبه</summary>
        public decimal EdgeUnitPrice { get; set; }

        /// <summary>فی پانچ لبه</summary>
        public decimal PunchUnitPrice { get; set; }

        /// <summary>فی دستمزد لبه</summary>
        public decimal LaborUnitPrice { get; set; }

        public List<EdgeLetterInputDto> Letters { get; set; } = new();
        public List<EdgeDifficultyBandDto> DifficultyBands { get; set; } = new();
    }

    public class EdgeLetterInputDto
    {
        public string? Label { get; set; }
        public decimal Length { get; set; }
    }

    public class EdgeDifficultyBandDto
    {
        /// <summary>حد پایین طول (شامل)</summary>
        public decimal MinLength { get; set; }

        /// <summary>حد بالای طول (شامل)</summary>
        public decimal MaxLength { get; set; }

        public decimal Coefficient { get; set; }
    }

    public class EdgeLetterContributionDto
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

    public class EdgeCostComponentDto
    {
        public string Name { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public string FormulaWithValues { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class EdgeCostResultDto
    {
        public CalculationCategory Category { get; set; }
        public string CategoryDisplayName { get; set; } = string.Empty;
        public bool Applies { get; set; }
        public bool IncludesPunch { get; set; }
        public string RuleDescription { get; set; } = string.Empty;

        public decimal ActualPvcLength { get; set; }
        public decimal RelativePvcLength { get; set; }
        public List<EdgeLetterContributionDto> LetterContributions { get; set; } = new();

        public EdgeCostComponentDto EdgeMaterial { get; set; } = new();
        public EdgeCostComponentDto? EdgePunch { get; set; }
        public EdgeCostComponentDto EdgeLabor { get; set; } = new();

        public decimal TotalEdgeCost { get; set; }
    }
}
