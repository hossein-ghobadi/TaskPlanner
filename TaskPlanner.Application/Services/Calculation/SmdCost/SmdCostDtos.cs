using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.SmdCost
{
    public class SmdCostInputDto
    {
        public CalculationCategory Category { get; set; }
        public bool EnableFrontSmd { get; set; }
        public bool EnableBackSmd { get; set; }

        /// <summary>
        /// برای دسته‌های غیر از آهن/استیل ساده: طول حقیقی PVC به‌صورت مستقیم.
        /// </summary>
        public decimal ActualPvcLength { get; set; }

        public decimal FrontGoldenNumber { get; set; }
        public decimal BackGoldenNumber { get; set; }
        public decimal FrontUnitPrice { get; set; }
        public decimal BackUnitPrice { get; set; }

        public List<SmdLetterInputDto> Letters { get; set; } = new();
        public List<SmdPenDiameterBandDto> PenDiameterBands { get; set; } = new();
    }

    public class SmdLetterInputDto
    {
        public string? Label { get; set; }
        public decimal Length { get; set; }
        public decimal Area { get; set; }
    }

    public class SmdPenDiameterBandDto
    {
        public decimal MinDiameter { get; set; }
        public decimal MaxDiameter { get; set; }
        public decimal Coefficient { get; set; }
    }

    public class SmdLetterContributionDto
    {
        public int Index { get; set; }
        public string Label { get; set; } = string.Empty;
        public decimal Length { get; set; }
        public decimal Area { get; set; }
        public decimal PenDiameter { get; set; }
        public decimal MinDiameter { get; set; }
        public decimal MaxDiameter { get; set; }
        public decimal Coefficient { get; set; }
        public decimal Contribution { get; set; }
        public string PenDiameterFormula { get; set; } = string.Empty;
        public string ContributionFormula { get; set; } = string.Empty;
    }

    public class SmdSideResultDto
    {
        public string Name { get; set; } = string.Empty;
        public decimal Count { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal Cost { get; set; }
        public string CountFormula { get; set; } = string.Empty;
        public string CountFormulaWithValues { get; set; } = string.Empty;
        public string CostFormula { get; set; } = string.Empty;
        public string CostFormulaWithValues { get; set; } = string.Empty;
    }

    public class SmdCostResultDto
    {
        public CalculationCategory Category { get; set; }
        public string CategoryDisplayName { get; set; } = string.Empty;
        public bool Applies { get; set; }
        public bool IncludesFrontSmd { get; set; }
        public bool IncludesBackSmd { get; set; }
        public bool UsesPenDiameterMode { get; set; }
        public string RuleDescription { get; set; } = string.Empty;

        public decimal ActualPvcLength { get; set; }
        public List<SmdLetterContributionDto> LetterContributions { get; set; } = new();

        public SmdSideResultDto? FrontSmd { get; set; }
        public SmdSideResultDto? BackSmd { get; set; }
        public decimal TotalSmdCost { get; set; }
    }
}
