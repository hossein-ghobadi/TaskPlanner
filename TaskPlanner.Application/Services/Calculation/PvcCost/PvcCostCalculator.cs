using System.Globalization;
using TaskPlanner.Common.CommonDto;
using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.PvcCost
{
    /// <summary>
    /// ماژول شفاف محاسبه هزینه PVC و PVC بک‌لایت.
    /// </summary>
    public class PvcCostCalculator : IPvcCostCalculator
    {
        private static readonly CultureInfo Fa = CultureInfo.GetCultureInfo("fa-IR");

        public ResultDto<PvcCostResultDto> Calculate(PvcCostInputDto input)
        {
            if (input is null)
                return Fail("ورودی محاسبه نامعتبر است.");

            if (!Enum.IsDefined(typeof(CalculationCategory), input.Category))
                return Fail("دسته محاسباتی نامعتبر است.");

            if (!input.Category.CanHavePvcCost())
            {
                return Ok(new PvcCostResultDto
                {
                    Category = input.Category,
                    CategoryDisplayName = input.Category.GetDisplayName(),
                    Applies = false,
                    RuleDescription = "دسته برش هزینه PVC ندارد؛ خروجی صفر است.",
                    TotalPvcCost = 0m
                }, "دسته برش هزینه PVC ندارد.");
            }

            if (input.PvcConsumedArea < 0 || input.BacklightConsumedArea < 0 || input.PvcUnitPrice < 0)
                return Fail("مساحت‌ها و فی PVC نمی‌توانند منفی باشند.");

            var pvcAmount = input.PvcConsumedArea * input.PvcUnitPrice;
            var backlightAmount = input.BacklightConsumedArea * input.PvcUnitPrice;

            return Ok(new PvcCostResultDto
            {
                Category = input.Category,
                CategoryDisplayName = input.Category.GetDisplayName(),
                Applies = true,
                RuleDescription =
                    "هزینه PVC = مساحت مصرفی PVC × فی PVC؛ " +
                    "هزینه PVC بک‌لایت = مساحت مصرفی بک‌لایت × فی PVC.",
                PvcCost = new PvcCostComponentDto
                {
                    Name = "هزینه PVC",
                    Formula = "مساحت مصرفی PVC × فی PVC",
                    FormulaWithValues =
                        $"{Format(input.PvcConsumedArea)} × {Format(input.PvcUnitPrice)} = {Format(pvcAmount)}",
                    Amount = pvcAmount
                },
                BacklightPvcCost = new PvcCostComponentDto
                {
                    Name = "هزینه PVC بک‌لایت",
                    Formula = "مساحت مصرفی بک‌لایت × فی PVC",
                    FormulaWithValues =
                        $"{Format(input.BacklightConsumedArea)} × {Format(input.PvcUnitPrice)} = {Format(backlightAmount)}",
                    Amount = backlightAmount
                },
                TotalPvcCost = pvcAmount + backlightAmount
            }, "هزینه PVC محاسبه شد.");
        }

        private static string Format(decimal value) =>
            value.ToString("#,0.####", Fa);

        private static ResultDto<PvcCostResultDto> Fail(string message) => new()
        {
            isSuccess = false,
            message = message,
            data = null!
        };

        private static ResultDto<PvcCostResultDto> Ok(PvcCostResultDto data, string message) => new()
        {
            isSuccess = true,
            message = message,
            data = data
        };
    }
}
