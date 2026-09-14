using System.Globalization;
using TaskPlanner.Common.CommonDto;
using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.MetalsCost
{
    /// <summary>
    /// ماژول شفاف محاسبه هزینه فلزات.
    /// </summary>
    public class MetalsCostCalculator : IMetalsCostCalculator
    {
        private static readonly CultureInfo Fa = CultureInfo.GetCultureInfo("fa-IR");

        public ResultDto<MetalsCostResultDto> Calculate(MetalsCostInputDto input)
        {
            if (input is null)
                return Fail("ورودی محاسبه نامعتبر است.");

            if (!Enum.IsDefined(typeof(CalculationCategory), input.Category))
                return Fail("دسته محاسباتی نامعتبر است.");

            if (!input.Category.CanHaveMetalsCost())
            {
                return Ok(new MetalsCostResultDto
                {
                    Category = input.Category,
                    CategoryDisplayName = input.Category.GetDisplayName(),
                    Applies = false,
                    RuleDescription = "این دسته هزینه فلزات ندارد؛ خروجی صفر است.",
                    TotalMetalsCost = 0m
                }, "این دسته هزینه فلزات ندارد.");
            }

            if (input.ActualPvcLength < 0
                || input.BaseUnitPrice < 0
                || input.DoughyConsumedArea < 0
                || input.DoughyUnitPrice < 0
                || input.DoughyLaborUnitPrice < 0
                || input.EdgeSize < 0
                || input.PlasticEdgeUnitPrice < 0
                || input.AssemblyUnitPrice < 0
                || input.RingEdgeUnitPrice < 0
                || input.DoubleEdgeRingLaborUnitPrice < 0)
            {
                return Fail("مقادیر ورودی نمی‌توانند منفی باشند.");
            }

            var includesDoubleEdge = input.Category.CanHaveDoubleEdgeRingCost();
            var length = input.ActualPvcLength;

            var baseAmount = length * input.BaseUnitPrice;
            var doughyAmount =
                (input.DoughyConsumedArea * input.DoughyUnitPrice)
                + (length * input.DoughyLaborUnitPrice);
            var metalPlasticAmount = length * input.EdgeSize * input.PlasticEdgeUnitPrice;
            var assemblyAmount = length * input.AssemblyUnitPrice;

            MetalsCostComponentDto? doubleEdge = null;
            var doubleEdgeAmount = 0m;
            if (includesDoubleEdge)
            {
                doubleEdgeAmount = length * (input.RingEdgeUnitPrice + input.DoubleEdgeRingLaborUnitPrice);
                doubleEdge = new MetalsCostComponentDto
                {
                    Name = "هزینه دولبه رینگ",
                    Formula = "طول حقیقی PVC × (فی لبه + فی دستمزد دو لبه رینگ)",
                    FormulaWithValues =
                        $"{Format(length)} × ({Format(input.RingEdgeUnitPrice)} + {Format(input.DoubleEdgeRingLaborUnitPrice)}) = {Format(doubleEdgeAmount)}",
                    Amount = doubleEdgeAmount
                };
            }

            var total = baseAmount + doughyAmount + metalPlasticAmount + assemblyAmount + doubleEdgeAmount;

            return Ok(new MetalsCostResultDto
            {
                Category = input.Category,
                CategoryDisplayName = input.Category.GetDisplayName(),
                Applies = true,
                IncludesDoubleEdgeRing = includesDoubleEdge,
                RuleDescription = includesDoubleEdge
                    ? "هزینه پایه، دوغی، فلز پلاست، دستمزد مونتاژ و دولبه رینگ محاسبه می‌شود."
                    : "هزینه پایه، دوغی، فلز پلاست و دستمزد مونتاژ محاسبه می‌شود (بدون دولبه رینگ).",
                ActualPvcLength = length,
                BaseCost = new MetalsCostComponentDto
                {
                    Name = "هزینه پایه",
                    Formula = "طول حقیقی PVC × فی پایه",
                    FormulaWithValues =
                        $"{Format(length)} × {Format(input.BaseUnitPrice)} = {Format(baseAmount)}",
                    Amount = baseAmount
                },
                DoughyCost = new MetalsCostComponentDto
                {
                    Name = "هزینه دوغی",
                    Formula = "(مساحت مصرفی دوغی × فی دوغی) + (طول حقیقی PVC × دستمزد دوغی)",
                    FormulaWithValues =
                        $"({Format(input.DoughyConsumedArea)} × {Format(input.DoughyUnitPrice)}) + ({Format(length)} × {Format(input.DoughyLaborUnitPrice)}) = {Format(doughyAmount)}",
                    Amount = doughyAmount
                },
                MetalPlasticCost = new MetalsCostComponentDto
                {
                    Name = "هزینه فلز پلاست",
                    Formula = "طول حقیقی PVC × سایز لبه × فی لبه پلاستیک",
                    FormulaWithValues =
                        $"{Format(length)} × {Format(input.EdgeSize)} × {Format(input.PlasticEdgeUnitPrice)} = {Format(metalPlasticAmount)}",
                    Amount = metalPlasticAmount
                },
                AssemblyLaborCost = new MetalsCostComponentDto
                {
                    Name = "دستمزد مونتاژ",
                    Formula = "طول حقیقی PVC × فی مونتاژ",
                    FormulaWithValues =
                        $"{Format(length)} × {Format(input.AssemblyUnitPrice)} = {Format(assemblyAmount)}",
                    Amount = assemblyAmount
                },
                DoubleEdgeRingCost = doubleEdge,
                TotalMetalsCost = total
            }, "هزینه فلزات محاسبه شد.");
        }

        private static string Format(decimal value) =>
            value.ToString("#,0.####", Fa);

        private static ResultDto<MetalsCostResultDto> Fail(string message) => new()
        {
            isSuccess = false,
            message = message,
            data = null!
        };

        private static ResultDto<MetalsCostResultDto> Ok(MetalsCostResultDto data, string message) => new()
        {
            isSuccess = true,
            message = message,
            data = data
        };
    }
}
