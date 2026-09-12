using System.Globalization;
using TaskPlanner.Common.CommonDto;
using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.EdgeCost
{
    /// <summary>
    /// ماژول شفاف محاسبه هزینه لبه.
    /// </summary>
    public class EdgeCostCalculator : IEdgeCostCalculator
    {
        private static readonly CultureInfo Fa = CultureInfo.GetCultureInfo("fa-IR");

        public ResultDto<EdgeCostResultDto> Calculate(EdgeCostInputDto input)
        {
            if (input is null)
            {
                return Fail("ورودی محاسبه نامعتبر است.");
            }

            if (!Enum.IsDefined(typeof(CalculationCategory), input.Category))
            {
                return Fail("دسته محاسباتی نامعتبر است.");
            }

            if (!input.Category.CanHaveEdgeCost())
            {
                return new ResultDto<EdgeCostResultDto>
                {
                    isSuccess = true,
                    message = "دسته برش هزینه لبه ندارد.",
                    data = new EdgeCostResultDto
                    {
                        Category = input.Category,
                        CategoryDisplayName = input.Category.GetDisplayName(),
                        Applies = false,
                        RuleDescription = "دسته برش هزینه لبه ندارد؛ خروجی صفر است.",
                        TotalEdgeCost = 0m
                    }
                };
            }

            if (input.EdgeUnitPrice < 0 || input.PunchUnitPrice < 0 || input.LaborUnitPrice < 0)
            {
                return Fail("فی‌ها نمی‌توانند منفی باشند.");
            }

            var letters = (input.Letters ?? new List<EdgeLetterInputDto>())
                .Where(l => l is not null)
                .ToList();

            if (letters.Count == 0)
            {
                return Fail("برای محاسبه حداقل یک حرف با طول وارد کنید.");
            }

            if (letters.Any(l => l.Length < 0))
            {
                return Fail("طول حروف نمی‌تواند منفی باشد.");
            }

            var bands = (input.DifficultyBands ?? new List<EdgeDifficultyBandDto>())
                .Where(b => b is not null)
                .OrderBy(b => b.MinLength)
                .ToList();

            if (bands.Count == 0)
            {
                return Fail("حداقل یک محدوده ضریب سختی تعریف کنید.");
            }

            if (bands.Any(b => b.MinLength < 0 || b.MaxLength < 0 || b.Coefficient < 0))
            {
                return Fail("مقادیر محدوده و ضریب سختی نمی‌توانند منفی باشند.");
            }

            if (bands.Any(b => b.MaxLength < b.MinLength))
            {
                return Fail("در هر محدوده، حد بالا باید بزرگ‌تر یا مساوی حد پایین باشد.");
            }

            var actualPvcLength = letters.Sum(l => l.Length);

            var contributions = new List<EdgeLetterContributionDto>();
            for (var i = 0; i < letters.Count; i++)
            {
                var letter = letters[i];
                var band = bands.FirstOrDefault(b => letter.Length >= b.MinLength && letter.Length <= b.MaxLength);
                if (band is null)
                {
                    return Fail(
                        $"برای طول حرف «{ResolveLabel(letter.Label, i)}» ({Format(letter.Length)}) محدوده‌ای یافت نشد.");
                }

                var contribution = letter.Length * band.Coefficient;
                contributions.Add(new EdgeLetterContributionDto
                {
                    Index = i + 1,
                    Label = ResolveLabel(letter.Label, i),
                    Length = letter.Length,
                    MinLength = band.MinLength,
                    MaxLength = band.MaxLength,
                    Coefficient = band.Coefficient,
                    Contribution = contribution,
                    FormulaWithValues =
                        $"{Format(letter.Length)} × {Format(band.Coefficient)} = {Format(contribution)}"
                });
            }

            var relativeLength = contributions.Sum(c => c.Contribution);
            var includesPunch = input.EnablePunch;
            var edgeMaterial = actualPvcLength * input.EdgeUnitPrice;
            var edgePunch = includesPunch ? actualPvcLength * input.PunchUnitPrice : 0m;
            var edgeLabor = relativeLength * input.LaborUnitPrice;

            var punchRule = includesPunch
                ? "پانچ لبه فعال است: طول حقیقی × فی پانچ؛ "
                : "پانچ لبه غیرفعال است؛ ";

            var result = new EdgeCostResultDto
            {
                Category = input.Category,
                CategoryDisplayName = input.Category.GetDisplayName(),
                Applies = true,
                IncludesPunch = includesPunch,
                RuleDescription =
                    "طول حقیقی PVC = مجموع طول همه حروف؛ " +
                    "هزینه متریال لبه = طول حقیقی × فی لبه؛ " +
                    punchRule +
                    "هزینه دستمزد لبه = طول نسبی × فی دستمزد؛ " +
                    "طول نسبی = Σ(طول هر حرف × ضریب سختی محدوده).",
                ActualPvcLength = actualPvcLength,
                RelativePvcLength = relativeLength,
                LetterContributions = contributions,
                EdgeMaterial = new EdgeCostComponentDto
                {
                    Name = "هزینه متریال لبه",
                    Formula = "طول حقیقی PVC × فی لبه",
                    FormulaWithValues =
                        $"{Format(actualPvcLength)} × {Format(input.EdgeUnitPrice)} = {Format(edgeMaterial)}",
                    Amount = edgeMaterial
                },
                EdgePunch = includesPunch
                    ? new EdgeCostComponentDto
                    {
                        Name = "هزینه پانچ لبه",
                        Formula = "طول حقیقی PVC × فی پانچ",
                        FormulaWithValues =
                            $"{Format(actualPvcLength)} × {Format(input.PunchUnitPrice)} = {Format(edgePunch)}",
                        Amount = edgePunch
                    }
                    : null,
                EdgeLabor = new EdgeCostComponentDto
                {
                    Name = "هزینه دستمزد لبه",
                    Formula = "طول نسبی PVC × فی دستمزد",
                    FormulaWithValues =
                        $"{Format(relativeLength)} × {Format(input.LaborUnitPrice)} = {Format(edgeLabor)}",
                    Amount = edgeLabor
                },
                TotalEdgeCost = edgeMaterial + edgePunch + edgeLabor
            };

            return new ResultDto<EdgeCostResultDto>
            {
                isSuccess = true,
                message = "هزینه لبه محاسبه شد.",
                data = result
            };
        }

        private static string ResolveLabel(string? label, int index) =>
            string.IsNullOrWhiteSpace(label) ? $"حرف {index + 1}" : label.Trim();

        private static string Format(decimal value) =>
            value.ToString("#,0.####", Fa);

        private static ResultDto<EdgeCostResultDto> Fail(string message) => new()
        {
            isSuccess = false,
            message = message,
            data = null!
        };
    }
}
