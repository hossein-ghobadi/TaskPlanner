using System.ComponentModel.DataAnnotations;
using TaskPlanner.Domain.Entities.Calculation;

namespace Endpoint.Site.Models
{
    public class SmdCostTestVm
    {
        [Display(Name = "دسته محاسباتی")]
        public CalculationCategory Category { get; set; } = CalculationCategory.Chanelium;

        [Display(Name = "SMD جلو")]
        public bool EnableFrontSmd { get; set; }

        [Display(Name = "SMD پشت")]
        public bool EnableBackSmd { get; set; } = true;

        [Display(Name = "عدد طلایی SMD جلو")]
        public decimal FrontGoldenNumber { get; set; }

        [Display(Name = "عدد طلایی SMD پشت")]
        public decimal BackGoldenNumber { get; set; }

        [Display(Name = "فی SMD جلو")]
        public decimal FrontUnitPrice { get; set; }

        [Display(Name = "فی SMD پشت")]
        public decimal BackUnitPrice { get; set; }

        [Display(Name = "طول حقیقی PVC")]
        public decimal ActualPvcLengthInput { get; set; }

        public List<SmdLetterVm> Letters { get; set; } = new()
        {
            new() { Label = "حرف 1", Length = 0, Area = 0 }
        };

        public List<SmdPenDiameterBandVm> PenDiameterBands { get; set; } = new()
        {
            new() { MinDiameter = 0, MaxDiameter = 1, Coefficient = 1 },
            new() { MinDiameter = 1, MaxDiameter = 2, Coefficient = 1.2m },
            new() { MinDiameter = 2, MaxDiameter = 10, Coefficient = 1.5m }
        };

        public bool HasResult { get; set; }
        public bool IsSuccess { get; set; }
        public string? Message { get; set; }

        public string? CategoryDisplayName { get; set; }
        public bool Applies { get; set; }
        public bool IncludesFrontSmd { get; set; }
        public bool IncludesBackSmd { get; set; }
        public bool UsesPenDiameterMode { get; set; }
        public string? RuleDescription { get; set; }
        public decimal ActualPvcLength { get; set; }
        public List<SmdLetterContributionVm> LetterContributions { get; set; } = new();
        public SmdSideResultVm? FrontSmd { get; set; }
        public SmdSideResultVm? BackSmd { get; set; }
        public decimal TotalSmdCost { get; set; }

        public IReadOnlyList<CalculationCategory> AllCategories { get; set; } =
            Enum.GetValues<CalculationCategory>();
    }

    public class SmdLetterVm
    {
        public string? Label { get; set; }
        public decimal Length { get; set; }
        public decimal Area { get; set; }
    }

    public class SmdPenDiameterBandVm
    {
        public decimal MinDiameter { get; set; }
        public decimal MaxDiameter { get; set; }
        public decimal Coefficient { get; set; }
    }

    public class SmdLetterContributionVm
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

    public class SmdSideResultVm
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
}
