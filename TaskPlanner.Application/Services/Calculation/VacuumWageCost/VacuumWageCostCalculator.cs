using System.Globalization;
using TaskPlanner.Common.CommonDto;
using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.VacuumWageCost
{
    /// <summary>
    /// ماژول شفاف محاسبه دستمزد وکیوم.
    /// </summary>
    public class VacuumWageCostCalculator : IVacuumWageCostCalculator
    {
        private static readonly CultureInfo Fa = CultureInfo.GetCultureInfo("fa-IR");

        public ResultDto<VacuumWageCostResultDto> Calculate(VacuumWageCostInputDto input)
        {
            if (input is null)
                return Fail("ورودی محاسبه نامعتبر است.");

            if (!Enum.IsDefined(typeof(CalculationCategory), input.Category))
                return Fail("دسته محاسباتی نامعتبر است.");

            if (!input.Category.CanHaveVacuumWageCost())
            {
                return Ok(new VacuumWageCostResultDto
                {
                    Category = input.Category,
                    CategoryDisplayName = input.Category.GetDisplayName(),
                    Applies = false,
                    RuleDescription = "این دسته دستمزد وکیوم ندارد؛ خروجی صفر است.",
                    VacuumWageCost = 0m
                }, "این دسته دستمزد وکیوم ندارد.");
            }

            if (input.EdgeSize < 0 || input.VacuumUnitPrice < 0)
                return Fail("سایز لبه و فی وکیوم نمی‌توانند منفی باشند.");

            var cost = input.EdgeSize * input.VacuumUnitPrice;

            return Ok(new VacuumWageCostResultDto
            {
                Category = input.Category,
                CategoryDisplayName = input.Category.GetDisplayName(),
                Applies = true,
                RuleDescription = "هزینه وکیوم = سایز لبه × فی وکیوم.",
                Formula = "سایز لبه × فی وکیوم",
                FormulaWithValues =
                    $"{Format(input.EdgeSize)} × {Format(input.VacuumUnitPrice)} = {Format(cost)}",
                VacuumWageCost = cost
            }, "دستمزد وکیوم محاسبه شد.");
        }

        private static string Format(decimal value) =>
            value.ToString("#,0.####", Fa);

        private static ResultDto<VacuumWageCostResultDto> Fail(string message) => new()
        {
            isSuccess = false,
            message = message,
            data = null!
        };

        private static ResultDto<VacuumWageCostResultDto> Ok(VacuumWageCostResultDto data, string message) => new()
        {
            isSuccess = true,
            message = message,
            data = data
        };
    }
}
