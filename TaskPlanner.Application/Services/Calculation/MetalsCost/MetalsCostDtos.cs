using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.MetalsCost
{
    public class MetalsCostInputDto
    {
        public CalculationCategory Category { get; set; }

        /// <summary>طول حقیقی PVC</summary>
        public decimal ActualPvcLength { get; set; }

        /// <summary>فی پایه</summary>
        public decimal BaseUnitPrice { get; set; }

        /// <summary>مساحت مصرفی دوغی</summary>
        public decimal DoughyConsumedArea { get; set; }

        /// <summary>فی دوغی</summary>
        public decimal DoughyUnitPrice { get; set; }

        /// <summary>دستمزد دوغی</summary>
        public decimal DoughyLaborUnitPrice { get; set; }

        /// <summary>سایز لبه</summary>
        public decimal EdgeSize { get; set; }

        /// <summary>فی لبه پلاستیک</summary>
        public decimal PlasticEdgeUnitPrice { get; set; }

        /// <summary>فی مونتاژ</summary>
        public decimal AssemblyUnitPrice { get; set; }

        /// <summary>فی لبه (برای دولبه رینگ)</summary>
        public decimal RingEdgeUnitPrice { get; set; }

        /// <summary>فی دستمزد دو لبه رینگ</summary>
        public decimal DoubleEdgeRingLaborUnitPrice { get; set; }
    }

    public class MetalsCostComponentDto
    {
        public string Name { get; set; } = string.Empty;
        public string Formula { get; set; } = string.Empty;
        public string FormulaWithValues { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }

    public class MetalsCostResultDto
    {
        public CalculationCategory Category { get; set; }
        public string CategoryDisplayName { get; set; } = string.Empty;
        public bool Applies { get; set; }
        public bool IncludesDoubleEdgeRing { get; set; }
        public string RuleDescription { get; set; } = string.Empty;
        public decimal ActualPvcLength { get; set; }

        public MetalsCostComponentDto BaseCost { get; set; } = new();
        public MetalsCostComponentDto DoughyCost { get; set; } = new();
        public MetalsCostComponentDto MetalPlasticCost { get; set; } = new();
        public MetalsCostComponentDto AssemblyLaborCost { get; set; } = new();
        public MetalsCostComponentDto? DoubleEdgeRingCost { get; set; }

        public decimal TotalMetalsCost { get; set; }
    }
}
