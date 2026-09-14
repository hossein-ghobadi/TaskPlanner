using System.Globalization;
using TaskPlanner.Common.CommonDto;
using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.WoodCost
{
    /// <summary>
    /// ماژول شفاف محاسبه هزینه چوب.
    /// </summary>
    public class WoodCostCalculator : IWoodCostCalculator
    {
        private static readonly CultureInfo Fa = CultureInfo.GetCultureInfo("fa-IR");

        public ResultDto<WoodCostResultDto> Calculate(WoodCostInputDto input)
        {
            if (input is null)
                return Fail("ورودی محاسبه نامعتبر است.");

            if (!Enum.IsDefined(typeof(CalculationCategory), input.Category))
                return Fail("دسته محاسباتی نامعتبر است.");

            if (!input.Category.CanHaveWoodCost())
            {
                return Ok(new WoodCostResultDto
                {
                    Category = input.Category,
                    CategoryDisplayName = input.Category.GetDisplayName(),
                    Applies = false,
                    RuleDescription = "این دسته هزینه چوب ندارد؛ خروجی صفر است.",
                    WoodCost = 0m
                }, "این دسته هزینه چوب ندارد.");
            }

            if (input.WoodConsumedArea < 0 || input.WoodWaste < 0 || input.WoodUnitPrice < 0)
                return Fail("مساحت مصرفی، پرت و فی چوب نمی‌توانند منفی باشند.");

            var billableArea = input.WoodConsumedArea + input.WoodWaste;
            var cost = billableArea * input.WoodUnitPrice;

            return Ok(new WoodCostResultDto
            {
                Category = input.Category,
                CategoryDisplayName = input.Category.GetDisplayName(),
                Applies = true,
                RuleDescription = "هزینه چوب = (مساحت مصرفی چوب + پرت چوب) × فی چوب.",
                Formula = "(مساحت مصرفی چوب + پرت چوب) × فی چوب",
                FormulaWithValues =
                    $"({Format(input.WoodConsumedArea)} + {Format(input.WoodWaste)}) × {Format(input.WoodUnitPrice)} = {Format(cost)}",
                WoodCost = cost
            }, "هزینه چوب محاسبه شد.");
        }

        private static string Format(decimal value) =>
            value.ToString("#,0.####", Fa);

        private static ResultDto<WoodCostResultDto> Fail(string message) => new()
        {
            isSuccess = false,
            message = message,
            data = null!
        };

        private static ResultDto<WoodCostResultDto> Ok(WoodCostResultDto data, string message) => new()
        {
            isSuccess = true,
            message = message,
            data = data
        };
    }
}
