using System.Globalization;
using TaskPlanner.Common.CommonDto;
using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.GlueCost
{
    /// <summary>
    /// ماژول شفاف محاسبه هزینه چسب.
    /// </summary>
    public class GlueCostCalculator : IGlueCostCalculator
    {
        private static readonly CultureInfo Fa = CultureInfo.GetCultureInfo("fa-IR");

        public ResultDto<GlueCostResultDto> Calculate(GlueCostInputDto input)
        {
            if (input is null)
                return Fail("ورودی محاسبه نامعتبر است.");

            if (!Enum.IsDefined(typeof(CalculationCategory), input.Category))
                return Fail("دسته محاسباتی نامعتبر است.");

            if (!input.Category.CanHaveGlueCost())
            {
                return Ok(new GlueCostResultDto
                {
                    Category = input.Category,
                    CategoryDisplayName = input.Category.GetDisplayName(),
                    Applies = false,
                    RuleDescription = "آهن ساده و استیل ساده هزینه چسب ندارند؛ خروجی صفر است.",
                    GlueCost = 0m
                }, "آهن ساده و استیل ساده هزینه چسب ندارند.");
            }

            if (input.GlueUnitPrice < 0
                || input.Layer1ConsumedArea < 0
                || input.Layer2ConsumedArea < 0
                || input.ActualPvcLength < 0)
            {
                return Fail("مقادیر ورودی نمی‌توانند منفی باشند.");
            }

            var usesLayerAreas = input.Category.UsesLayerAreasForGlueCost();
            decimal glueCost;
            string formula;
            string formulaWithValues;
            string ruleDescription;

            if (usesLayerAreas)
            {
                var totalArea = input.Layer1ConsumedArea + input.Layer2ConsumedArea;
                glueCost = totalArea * input.GlueUnitPrice;
                formula = "(مساحت مصرفی لایه اول + مساحت مصرفی لایه دوم) × فی چسب";
                formulaWithValues =
                    $"({Format(input.Layer1ConsumedArea)} + {Format(input.Layer2ConsumedArea)}) × {Format(input.GlueUnitPrice)} = {Format(glueCost)}";
                ruleDescription = "برش: هزینه چسب از مجموع مساحت مصرفی دو لایه محاسبه می‌شود.";
            }
            else
            {
                glueCost = input.ActualPvcLength * input.GlueUnitPrice;
                formula = "طول حقیقی PVC × فی چسب";
                formulaWithValues =
                    $"{Format(input.ActualPvcLength)} × {Format(input.GlueUnitPrice)} = {Format(glueCost)}";
                ruleDescription = "سایر دسته‌ها: هزینه چسب = طول حقیقی PVC × فی چسب.";
            }

            return Ok(new GlueCostResultDto
            {
                Category = input.Category,
                CategoryDisplayName = input.Category.GetDisplayName(),
                Applies = true,
                UsesLayerAreas = usesLayerAreas,
                RuleDescription = ruleDescription,
                Formula = formula,
                FormulaWithValues = formulaWithValues,
                GlueCost = glueCost
            }, "هزینه چسب محاسبه شد.");
        }

        private static string Format(decimal value) =>
            value.ToString("#,0.####", Fa);

        private static ResultDto<GlueCostResultDto> Fail(string message) => new()
        {
            isSuccess = false,
            message = message,
            data = null!
        };

        private static ResultDto<GlueCostResultDto> Ok(GlueCostResultDto data, string message) => new()
        {
            isSuccess = true,
            message = message,
            data = data
        };
    }
}
