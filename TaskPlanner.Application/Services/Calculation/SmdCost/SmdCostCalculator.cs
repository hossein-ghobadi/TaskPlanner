using System.Globalization;
using TaskPlanner.Common.CommonDto;
using TaskPlanner.Domain.Entities.Calculation;

namespace TaskPlanner.Application.Services.Calculation.SmdCost
{
    /// <summary>
    /// ماژول شفاف محاسبه هزینه SMD جلو و پشت.
    /// </summary>
    public class SmdCostCalculator : ISmdCostCalculator
    {
        private const decimal PenDiameterDivisor = 45m;
        private static readonly CultureInfo Fa = CultureInfo.GetCultureInfo("fa-IR");

        public ResultDto<SmdCostResultDto> Calculate(SmdCostInputDto input)
        {
            if (input is null)
                return Fail("ورودی محاسبه نامعتبر است.");

            if (!Enum.IsDefined(typeof(CalculationCategory), input.Category))
                return Fail("دسته محاسباتی نامعتبر است.");

            if (!input.Category.CanHaveSmdCost())
            {
                return Ok(new SmdCostResultDto
                {
                    Category = input.Category,
                    CategoryDisplayName = input.Category.GetDisplayName(),
                    Applies = false,
                    RuleDescription = "دسته برش هزینه SMD ندارد؛ خروجی صفر است.",
                    TotalSmdCost = 0m
                }, "دسته برش هزینه SMD ندارد.");
            }

            var enableFront = input.EnableFrontSmd && input.Category.CanHaveFrontSmd();
            var enableBack = input.EnableBackSmd && input.Category.CanHaveBackSmd();

            if (!enableFront && !enableBack)
                return Fail("حداقل یکی از SMD جلو یا SMD پشت باید فعال باشد.");

            if (input.FrontGoldenNumber < 0 || input.BackGoldenNumber < 0
                || input.FrontUnitPrice < 0 || input.BackUnitPrice < 0)
            {
                return Fail("عدد طلایی و فی SMD نمی‌توانند منفی باشند.");
            }

            var usesPenDiameter = input.Category.UsesPenDiameterForBackSmd();
            SmdSideResultDto? front = null;
            SmdSideResultDto? back = null;
            var letterContributions = new List<SmdLetterContributionDto>();
            decimal actualPvcLength;

            if (usesPenDiameter)
            {
                // آهن/استیل ساده: فقط SMD پشت از قطر قلم
                if (enableFront)
                    return Fail("آهن ساده و استیل ساده SMD جلو ندارند.");

                if (!enableBack)
                    return Fail("برای آهن ساده و استیل ساده باید SMD پشت فعال باشد.");

                var letters = (input.Letters ?? new List<SmdLetterInputDto>())
                    .Where(l => l is not null)
                    .ToList();

                if (letters.Count == 0)
                    return Fail("حداقل یک حرف وارد کنید.");

                if (letters.Any(l => l.Length < 0 || l.Area < 0))
                    return Fail("طول و مساحت حروف نمی‌توانند منفی باشند.");

                var bands = (input.PenDiameterBands ?? new List<SmdPenDiameterBandDto>())
                    .Where(b => b is not null)
                    .OrderBy(b => b.MinDiameter)
                    .ToList();

                if (bands.Count == 0)
                    return Fail("حداقل یک محدوده ضریب قطر قلم تعریف کنید.");

                if (bands.Any(b => b.MinDiameter < 0 || b.MaxDiameter < 0 || b.Coefficient < 0))
                    return Fail("مقادیر محدوده و ضریب قطر قلم نمی‌توانند منفی باشند.");

                if (bands.Any(b => b.MaxDiameter < b.MinDiameter))
                    return Fail("در هر محدوده، حد بالا باید بزرگ‌تر یا مساوی حد پایین باشد.");

                if (letters.Any(l => l.Length == 0))
                    return Fail("برای محاسبه قطر قلم، طول حرف نمی‌تواند صفر باشد.");

                actualPvcLength = letters.Sum(l => l.Length);

                decimal backCount = 0m;
                for (var i = 0; i < letters.Count; i++)
                {
                    var letter = letters[i];
                    var penDiameter = letter.Area / letter.Length / PenDiameterDivisor;
                    var band = bands.FirstOrDefault(b =>
                        penDiameter >= b.MinDiameter && penDiameter <= b.MaxDiameter);

                    if (band is null)
                    {
                        return Fail(
                            $"برای قطر قلم «{ResolveLabel(letter.Label, i)}» ({Format(penDiameter)}) محدوده‌ای یافت نشد.");
                    }

                    var contribution = letter.Length * band.Coefficient * input.BackGoldenNumber;
                    backCount += contribution;

                    letterContributions.Add(new SmdLetterContributionDto
                    {
                        Index = i + 1,
                        Label = ResolveLabel(letter.Label, i),
                        Length = letter.Length,
                        Area = letter.Area,
                        PenDiameter = penDiameter,
                        MinDiameter = band.MinDiameter,
                        MaxDiameter = band.MaxDiameter,
                        Coefficient = band.Coefficient,
                        Contribution = contribution,
                        PenDiameterFormula =
                            $"{Format(letter.Area)} ÷ {Format(letter.Length)} ÷ {Format(PenDiameterDivisor)} = {Format(penDiameter)}",
                        ContributionFormula =
                            $"{Format(letter.Length)} × {Format(band.Coefficient)} × {Format(input.BackGoldenNumber)} = {Format(contribution)}"
                    });
                }

                back = BuildSide(
                    name: "SMD پشت",
                    count: backCount,
                    unitPrice: input.BackUnitPrice,
                    countFormula: "Σ(طول حرف × ضریب محدوده قطر قلم × عدد طلایی SMD پشت)",
                    countFormulaWithValues: $"{Format(backCount)}",
                    costFormula: "تعداد SMD پشت × فی SMD پشت");
            }
            else
            {
                if (input.ActualPvcLength < 0)
                    return Fail("طول حقیقی PVC نمی‌تواند منفی باشد.");

                actualPvcLength = input.ActualPvcLength;

                if (enableFront)
                {
                    var frontCount = actualPvcLength * input.FrontGoldenNumber;
                    front = BuildSide(
                        name: "SMD جلو",
                        count: frontCount,
                        unitPrice: input.FrontUnitPrice,
                        countFormula: "طول حقیقی PVC × عدد طلایی SMD جلو",
                        countFormulaWithValues:
                            $"{Format(actualPvcLength)} × {Format(input.FrontGoldenNumber)} = {Format(frontCount)}",
                        costFormula: "تعداد SMD جلو × فی SMD جلو");
                }

                if (enableBack)
                {
                    var backCount = actualPvcLength * input.BackGoldenNumber;
                    back = BuildSide(
                        name: "SMD پشت",
                        count: backCount,
                        unitPrice: input.BackUnitPrice,
                        countFormula: "طول حقیقی PVC × عدد طلایی SMD پشت",
                        countFormulaWithValues:
                            $"{Format(actualPvcLength)} × {Format(input.BackGoldenNumber)} = {Format(backCount)}",
                        costFormula: "تعداد SMD پشت × فی SMD پشت");
                }
            }

            var total = (front?.Cost ?? 0m) + (back?.Cost ?? 0m);
            var rule = BuildRuleDescription(input.Category, enableFront, enableBack, usesPenDiameter);

            return Ok(new SmdCostResultDto
            {
                Category = input.Category,
                CategoryDisplayName = input.Category.GetDisplayName(),
                Applies = true,
                IncludesFrontSmd = enableFront,
                IncludesBackSmd = enableBack,
                UsesPenDiameterMode = usesPenDiameter,
                RuleDescription = rule,
                ActualPvcLength = actualPvcLength,
                LetterContributions = letterContributions,
                FrontSmd = front,
                BackSmd = back,
                TotalSmdCost = total
            }, "هزینه SMD محاسبه شد.");
        }

        private static SmdSideResultDto BuildSide(
            string name,
            decimal count,
            decimal unitPrice,
            string countFormula,
            string countFormulaWithValues,
            string costFormula)
        {
            var cost = count * unitPrice;
            return new SmdSideResultDto
            {
                Name = name,
                Count = count,
                UnitPrice = unitPrice,
                Cost = cost,
                CountFormula = countFormula,
                CountFormulaWithValues = countFormulaWithValues,
                CostFormula = costFormula,
                CostFormulaWithValues = $"{Format(count)} × {Format(unitPrice)} = {Format(cost)}"
            };
        }

        private static string BuildRuleDescription(
            CalculationCategory category,
            bool enableFront,
            bool enableBack,
            bool usesPenDiameter)
        {
            if (usesPenDiameter)
            {
                return "آهن/استیل ساده: قطر قلم هر حرف = مساحت ÷ طول ÷ ۴۵؛ " +
                       "تعداد SMD پشت = Σ(طول حرف × ضریب محدوده قطر قلم × عدد طلایی SMD پشت)؛ " +
                       "هزینه SMD پشت = تعداد SMD پشت × فی SMD پشت. SMD جلو ندارند.";
            }

            var parts = new List<string> { "طول حقیقی PVC به‌صورت ورودی مستقیم استفاده می‌شود." };
            if (enableFront)
                parts.Add("SMD جلو: تعداد = طول حقیقی × عدد طلایی جلو؛ هزینه = تعداد × فی جلو.");
            if (enableBack)
                parts.Add("SMD پشت: تعداد = طول حقیقی × عدد طلایی پشت؛ هزینه = تعداد × فی پشت.");
            parts.Add("جمع SMD = هزینه جلو + هزینه پشت.");
            return string.Join(" ", parts);
        }

        private static string ResolveLabel(string? label, int index) =>
            string.IsNullOrWhiteSpace(label) ? $"حرف {index + 1}" : label.Trim();

        private static string Format(decimal value) =>
            value.ToString("#,0.####", Fa);

        private static ResultDto<SmdCostResultDto> Fail(string message) => new()
        {
            isSuccess = false,
            message = message,
            data = null!
        };

        private static ResultDto<SmdCostResultDto> Ok(SmdCostResultDto data, string message) => new()
        {
            isSuccess = true,
            message = message,
            data = data
        };
    }
}
