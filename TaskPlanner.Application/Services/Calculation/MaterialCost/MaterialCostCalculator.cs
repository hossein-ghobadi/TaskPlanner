using System.Globalization;
using TaskPlanner.Common.CommonDto;
using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.MaterialCost
{
    /// <summary>
    /// ماژول شفاف محاسبه هزینه متریال بر اساس دسته و تعداد لایه.
    /// </summary>
    public class MaterialCostCalculator : IMaterialCostCalculator
    {
        private static readonly CultureInfo Fa = CultureInfo.GetCultureInfo("fa-IR");

        public ResultDto<MaterialCostResultDto> Calculate(MaterialCostInputDto input)
        {
            if (input is null)
            {
                return Fail("ورودی محاسبه نامعتبر است.");
            }

            if (!Enum.IsDefined(typeof(CalculationCategory), input.Category))
            {
                return Fail("دسته محاسباتی نامعتبر است.");
            }

            var layerMode = input.Category.IsAlwaysSingleLayer()
                ? LayerMode.Single
                : input.LayerMode;

            if (!input.Category.SupportsLayerMode(layerMode))
            {
                return Fail($"{input.Category.GetDisplayName()} فقط به‌صورت تک‌لایه قابل محاسبه است.");
            }

            if (input.Layer1ConsumedArea < 0 || input.Layer1UnitPrice < 0 || input.Layer1Waste < 0
                || input.Layer1ActualPvcArea < 0 || input.Layer1PunchUnitPrice < 0
                || input.Layer2ConsumedArea < 0 || input.Layer2UnitPrice < 0 || input.Layer2Waste < 0
                || input.Layer2ActualPvcArea < 0 || input.Layer2PunchUnitPrice < 0)
            {
                return Fail("مقادیر مساحت، پرتی، فی و پانچ نمی‌توانند منفی باشند.");
            }

            var includesWaste = input.Category.IncludesWasteInMaterialCost();
            var includesPunch = input.EnablePunch && input.Category.CanHavePunchMaterial();

            var layers = new List<MaterialCostLayerResultDto>
            {
                BuildLayer(
                    layerNumber: 1,
                    consumedArea: input.Layer1ConsumedArea,
                    waste: includesWaste ? input.Layer1Waste : 0m,
                    unitPrice: input.Layer1UnitPrice,
                    includeWaste: includesWaste,
                    includePunch: includesPunch,
                    actualPvcArea: input.Layer1ActualPvcArea,
                    punchUnitPrice: input.Layer1PunchUnitPrice)
            };

            if (layerMode == LayerMode.Double)
            {
                layers.Add(BuildLayer(
                    layerNumber: 2,
                    consumedArea: input.Layer2ConsumedArea,
                    waste: includesWaste ? input.Layer2Waste : 0m,
                    unitPrice: input.Layer2UnitPrice,
                    includeWaste: includesWaste,
                    includePunch: includesPunch,
                    actualPvcArea: input.Layer2ActualPvcArea,
                    punchUnitPrice: input.Layer2PunchUnitPrice));
            }

            var totalSheet = layers.Sum(l => l.Cost);
            var totalPunch = layers.Sum(l => l.PunchCost);

            var result = new MaterialCostResultDto
            {
                Category = input.Category,
                CategoryDisplayName = input.Category.GetDisplayName(),
                LayerMode = layerMode,
                LayerModeDisplayName = layerMode.GetDisplayName(),
                IncludesWaste = includesWaste,
                IncludesPunch = includesPunch,
                RuleDescription = BuildRuleDescription(input.Category, layerMode, includesWaste, includesPunch),
                Layers = layers,
                TotalSheetMaterialCost = totalSheet,
                TotalPunchCost = totalPunch,
                TotalMaterialCost = totalSheet + totalPunch
            };

            return new ResultDto<MaterialCostResultDto>
            {
                isSuccess = true,
                message = "هزینه متریال محاسبه شد.",
                data = result
            };
        }

        private static MaterialCostLayerResultDto BuildLayer(
            int layerNumber,
            decimal consumedArea,
            decimal waste,
            decimal unitPrice,
            bool includeWaste,
            bool includePunch,
            decimal actualPvcArea,
            decimal punchUnitPrice)
        {
            var billableArea = includeWaste ? consumedArea + waste : consumedArea;
            var cost = billableArea * unitPrice;

            var formula = includeWaste
                ? $"(مساحت مصرفی لایه {layerNumber} + پرتی لایه {layerNumber}) × فی لایه {layerNumber}"
                : $"مساحت مصرفی لایه {layerNumber} × فی لایه {layerNumber}";

            var formulaWithValues = includeWaste
                ? $"({Format(consumedArea)} + {Format(waste)}) × {Format(unitPrice)} = {Format(cost)}"
                : $"{Format(consumedArea)} × {Format(unitPrice)} = {Format(cost)}";

            var punchCost = 0m;
            var punchFormula = string.Empty;
            var punchFormulaWithValues = string.Empty;

            if (includePunch)
            {
                punchCost = actualPvcArea * punchUnitPrice;
                punchFormula = $"مساحت حقیقی PVC لایه {layerNumber} × فی پانچ لایه {layerNumber}";
                punchFormulaWithValues = $"{Format(actualPvcArea)} × {Format(punchUnitPrice)} = {Format(punchCost)}";
            }

            return new MaterialCostLayerResultDto
            {
                LayerNumber = layerNumber,
                ConsumedArea = consumedArea,
                Waste = waste,
                BillableArea = billableArea,
                UnitPrice = unitPrice,
                Cost = cost,
                Formula = formula,
                FormulaWithValues = formulaWithValues,
                HasPunch = includePunch,
                ActualPvcArea = includePunch ? actualPvcArea : 0m,
                PunchUnitPrice = includePunch ? punchUnitPrice : 0m,
                PunchCost = punchCost,
                PunchFormula = punchFormula,
                PunchFormulaWithValues = punchFormulaWithValues,
                LayerTotalCost = cost + punchCost
            };
        }

        private static string BuildRuleDescription(
            CalculationCategory category,
            LayerMode layerMode,
            bool includesWaste,
            bool includesPunch)
        {
            string sheetRule;
            if (category.IsAlwaysSingleLayer())
            {
                sheetRule = "آهن ساده و استیل ساده همیشه تک‌لایه‌اند: هزینه ورق = مساحت مصرفی لایه اول × فی لایه اول.";
            }
            else if (includesWaste)
            {
                sheetRule = layerMode == LayerMode.Double
                    ? "وکیوم: هزینه ورق هر لایه = (مساحت مصرفی + پرتی) × فی همان لایه."
                    : "وکیوم تک‌لایه: هزینه ورق لایه اول = (مساحت مصرفی لایه اول + پرتی لایه اول) × فی لایه اول.";
            }
            else
            {
                sheetRule = layerMode == LayerMode.Double
                    ? "سایر دسته‌ها: هزینه ورق هر لایه = مساحت مصرفی همان لایه × فی همان لایه (بدون پرتی)."
                    : "سایر دسته‌ها (تک‌لایه): هزینه ورق = مساحت مصرفی لایه اول × فی لایه اول.";
            }

            var punchRule = includesPunch
                ? " پانچ متریال فعال است: هر لایه = مساحت حقیقی PVC × فی پانچ."
                : category.CanHavePunchMaterial()
                    ? " پانچ متریال غیرفعال است و در محاسبه لحاظ نمی‌شود."
                    : " دسته برش پانچ متریال ندارد.";

            return sheetRule + punchRule;
        }

        private static string Format(decimal value) =>
            value.ToString("#,0.####", Fa);

        private static ResultDto<MaterialCostResultDto> Fail(string message) => new()
        {
            isSuccess = false,
            message = message,
            data = null!
        };
    }
}
