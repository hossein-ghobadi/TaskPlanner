using System.Globalization;
using TaskPlanner.Common.CommonDto;
using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.CutSpecificCost
{
    /// <summary>
    /// ماژول شفاف محاسبه هزینه اختصاصی برش.
    /// </summary>
    public class CutSpecificCostCalculator : ICutSpecificCostCalculator
    {
        private static readonly CultureInfo Fa = CultureInfo.GetCultureInfo("fa-IR");

        public ResultDto<CutSpecificCostResultDto> Calculate(CutSpecificCostInputDto input)
        {
            if (input is null)
                return Fail("ورودی محاسبه نامعتبر است.");

            if (!Enum.IsDefined(typeof(CalculationCategory), input.Category))
                return Fail("دسته محاسباتی نامعتبر است.");

            if (!input.Category.CanHaveCutSpecificCost())
            {
                return Ok(new CutSpecificCostResultDto
                {
                    Category = input.Category,
                    CategoryDisplayName = input.Category.GetDisplayName(),
                    Applies = false,
                    RuleDescription = "ماژول اختصاصی برش فقط برای دسته برش است؛ خروجی صفر است.",
                    CutCost = 0m
                }, "این دسته هزینه اختصاصی برش ندارد.");
            }

            if (input.ActualPvcLength < 0 || input.CutUnitPrice < 0 || input.MultiLayerCoefficient < 0)
                return Fail("طول حقیقی PVC، فی برش و ضریب چندلایه نمی‌توانند منفی باشند.");

            var cost = input.ActualPvcLength * input.CutUnitPrice * input.MultiLayerCoefficient;

            return Ok(new CutSpecificCostResultDto
            {
                Category = input.Category,
                CategoryDisplayName = input.Category.GetDisplayName(),
                Applies = true,
                RuleDescription = "هزینه برش = طول حقیقی PVC × فی برش × ضریب چندلایه بودن.",
                Formula = "طول حقیقی PVC × فی برش × ضریب چندلایه بودن",
                FormulaWithValues =
                    $"{Format(input.ActualPvcLength)} × {Format(input.CutUnitPrice)} × {Format(input.MultiLayerCoefficient)} = {Format(cost)}",
                CutCost = cost
            }, "هزینه برش محاسبه شد.");
        }

        private static string Format(decimal value) =>
            value.ToString("#,0.####", Fa);

        private static ResultDto<CutSpecificCostResultDto> Fail(string message) => new()
        {
            isSuccess = false,
            message = message,
            data = null!
        };

        private static ResultDto<CutSpecificCostResultDto> Ok(CutSpecificCostResultDto data, string message) => new()
        {
            isSuccess = true,
            message = message,
            data = data
        };
    }
}
