using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.GlueCost
{
    public class GlueCostInputDto
    {
        public CalculationCategory Category { get; set; }

        /// <summary>مساحت مصرفی لایه اول (برای برش)</summary>
        public decimal Layer1ConsumedArea { get; set; }

        /// <summary>مساحت مصرفی لایه دوم (برای برش)</summary>
        public decimal Layer2ConsumedArea { get; set; }

        /// <summary>طول حقیقی PVC (برای سایر دسته‌ها به‌جز آهن/استیل ساده)</summary>
        public decimal ActualPvcLength { get; set; }

        /// <summary>فی چسب</summary>
        public decimal GlueUnitPrice { get; set; }
    }

    public class GlueCostResultDto
    {
        public CalculationCategory Category { get; set; }
        public string CategoryDisplayName { get; set; } = string.Empty;
        public bool Applies { get; set; }
        public bool UsesLayerAreas { get; set; }
        public string RuleDescription { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public string FormulaWithValues { get; set; } = string.Empty;
        public decimal GlueCost { get; set; }
    }
}
