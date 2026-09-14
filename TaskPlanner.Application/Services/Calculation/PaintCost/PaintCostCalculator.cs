using System.Globalization;
using TaskPlanner.Common.CommonDto;
using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.PaintCost
{
    /// <summary>
    /// ماژول شفاف محاسبه هزینه رنگ.
    /// </summary>
    public class PaintCostCalculator : IPaintCostCalculator
    {
        private static readonly CultureInfo Fa = CultureInfo.GetCultureInfo("fa-IR");

        public ResultDto<PaintCostResultDto> Calculate(PaintCostInputDto input)
        {
            if (input is null)
                return Fail("ورودی محاسبه نامعتبر است.");

            if (!Enum.IsDefined(typeof(CalculationCategory), input.Category))
                return Fail("دسته محاسباتی نامعتبر است.");

            if (!input.Category.CanHavePaintCost())
            {
                return Ok(new PaintCostResultDto
                {
                    Category = input.Category,
                    CategoryDisplayName = input.Category.GetDisplayName(),
                    Applies = false,
                    RuleDescription = "این دسته هزینه رنگ ندارد؛ خروجی صفر است.",
                    PaintCost = 0m
                }, "این دسته هزینه رنگ ندارد.");
            }

            if (input.LayerConsumedArea < 0
                || input.ActualPvcLength < 0
                || input.EdgeSize < 0
                || input.PaintUnitPrice < 0)
            {
                return Fail("مقادیر ورودی نمی‌توانند منفی باشند.");
            }

            var billableArea = input.LayerConsumedArea + (input.ActualPvcLength * input.EdgeSize);
            var cost = billableArea * input.PaintUnitPrice;

            return Ok(new PaintCostResultDto
            {
                Category = input.Category,
                CategoryDisplayName = input.Category.GetDisplayName(),
                Applies = true,
                RuleDescription =
                    "هزینه رنگ = (مساحت مصرفی لایه + (طول حقیقی PVC × سایز لبه)) × فی رنگ.",
                Formula = "(مساحت مصرفی لایه + (طول حقیقی PVC × سایز لبه)) × فی رنگ",
                FormulaWithValues =
                    $"({Format(input.LayerConsumedArea)} + ({Format(input.ActualPvcLength)} × {Format(input.EdgeSize)})) × {Format(input.PaintUnitPrice)} = {Format(cost)}",
                PaintCost = cost
            }, "هزینه رنگ محاسبه شد.");
        }

        private static string Format(decimal value) =>
            value.ToString("#,0.####", Fa);

        private static ResultDto<PaintCostResultDto> Fail(string message) => new()
        {
            isSuccess = false,
            message = message,
            data = null!
        };

        private static ResultDto<PaintCostResultDto> Ok(PaintCostResultDto data, string message) => new()
        {
            isSuccess = true,
            message = message,
            data = data
        };
    }
}
