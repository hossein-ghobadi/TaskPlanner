using System.Globalization;
using TaskPlanner.Common.CommonDto;
using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.CrystalCost
{
    /// <summary>
    /// ماژول شفاف محاسبه هزینه کریستال.
    /// </summary>
    public class CrystalCostCalculator : ICrystalCostCalculator
    {
        private static readonly CultureInfo Fa = CultureInfo.GetCultureInfo("fa-IR");

        public ResultDto<CrystalCostResultDto> Calculate(CrystalCostInputDto input)
        {
            if (input is null)
                return Fail("ورودی محاسبه نامعتبر است.");

            if (!Enum.IsDefined(typeof(CalculationCategory), input.Category))
                return Fail("دسته محاسباتی نامعتبر است.");

            if (!input.Category.CanHaveCrystalCost())
            {
                return Ok(new CrystalCostResultDto
                {
                    Category = input.Category,
                    CategoryDisplayName = input.Category.GetDisplayName(),
                    Applies = false,
                    RuleDescription = "برش، آهن ساده و استیل ساده هزینه کریستال ندارند؛ خروجی صفر است.",
                    CrystalCost = 0m
                }, "این دسته هزینه کریستال ندارد.");
            }

            if (input.ActualPvcArea < 0 || input.CrystalUnitPrice < 0)
                return Fail("مساحت حقیقی PVC و فی کریستال نمی‌توانند منفی باشند.");

            var cost = input.ActualPvcArea * input.CrystalUnitPrice;

            return Ok(new CrystalCostResultDto
            {
                Category = input.Category,
                CategoryDisplayName = input.Category.GetDisplayName(),
                Applies = true,
                RuleDescription = "هزینه کریستال = مساحت حقیقی PVC × فی کریستال.",
                Formula = "مساحت حقیقی PVC × فی کریستال",
                FormulaWithValues =
                    $"{Format(input.ActualPvcArea)} × {Format(input.CrystalUnitPrice)} = {Format(cost)}",
                CrystalCost = cost
            }, "هزینه کریستال محاسبه شد.");
        }

        private static string Format(decimal value) =>
            value.ToString("#,0.####", Fa);

        private static ResultDto<CrystalCostResultDto> Fail(string message) => new()
        {
            isSuccess = false,
            message = message,
            data = null!
        };

        private static ResultDto<CrystalCostResultDto> Ok(CrystalCostResultDto data, string message) => new()
        {
            isSuccess = true,
            message = message,
            data = data
        };
    }
}
