using Endpoint.Site.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.Application.Services.Calculation.EdgeCost;
using TaskPlanner.Application.Services.Calculation.MaterialCost;
using TaskPlanner.Domain.Entities.Calculation;

namespace Endpoint.Site.Controllers
{
    /// <summary>
    /// محیط تست ماژول‌های سرویس محاسبه.
    /// </summary>
    [Authorize]
    [Route("[controller]/[action]")]
    public class ModuleTestsController : Controller
    {
        private readonly IMaterialCostCalculator _materialCostCalculator;
        private readonly IEdgeCostCalculator _edgeCostCalculator;

        public ModuleTestsController(
            IMaterialCostCalculator materialCostCalculator,
            IEdgeCostCalculator edgeCostCalculator)
        {
            _materialCostCalculator = materialCostCalculator;
            _edgeCostCalculator = edgeCostCalculator;
        }

        [HttpGet]
        public IActionResult Index()
        {
            ViewData["Title"] = "تست ماژول‌های محاسبه";
            ViewData["UseFluidLayout"] = true;
            return View();
        }

        [HttpGet]
        public IActionResult MaterialCost()
        {
            ViewData["Title"] = "تست ماژول هزینه متریال";
            ViewData["UseFluidLayout"] = true;
            return View(new MaterialCostTestVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MaterialCost(MaterialCostTestVm model)
        {
            ViewData["Title"] = "تست ماژول هزینه متریال";
            ViewData["UseFluidLayout"] = true;

            model.AllCategories = Enum.GetValues<CalculationCategory>();

            var calcResult = _materialCostCalculator.Calculate(new MaterialCostInputDto
            {
                Category = model.Category,
                LayerMode = model.LayerMode,
                EnablePunch = model.EnablePunch,
                Layer1ConsumedArea = model.Layer1ConsumedArea,
                Layer1UnitPrice = model.Layer1UnitPrice,
                Layer1Waste = model.Layer1Waste,
                Layer1ActualPvcArea = model.Layer1ActualPvcArea,
                Layer1PunchUnitPrice = model.Layer1PunchUnitPrice,
                Layer2ConsumedArea = model.Layer2ConsumedArea,
                Layer2UnitPrice = model.Layer2UnitPrice,
                Layer2Waste = model.Layer2Waste,
                Layer2ActualPvcArea = model.Layer2ActualPvcArea,
                Layer2PunchUnitPrice = model.Layer2PunchUnitPrice
            });

            model.HasResult = true;
            model.IsSuccess = calcResult.isSuccess;
            model.Message = calcResult.message;

            if (calcResult.isSuccess && calcResult.data is not null)
            {
                var data = calcResult.data;
                model.Category = data.Category;
                model.LayerMode = data.LayerMode;
                model.CategoryDisplayName = data.CategoryDisplayName;
                model.LayerModeDisplayName = data.LayerModeDisplayName;
                model.IncludesWaste = data.IncludesWaste;
                model.IncludesPunch = data.IncludesPunch;
                model.RuleDescription = data.RuleDescription;
                model.TotalSheetMaterialCost = data.TotalSheetMaterialCost;
                model.TotalPunchCost = data.TotalPunchCost;
                model.TotalMaterialCost = data.TotalMaterialCost;
                model.Layers = data.Layers.Select(l => new MaterialCostLayerResultVm
                {
                    LayerNumber = l.LayerNumber,
                    ConsumedArea = l.ConsumedArea,
                    Waste = l.Waste,
                    BillableArea = l.BillableArea,
                    UnitPrice = l.UnitPrice,
                    Cost = l.Cost,
                    Formula = l.Formula,
                    FormulaWithValues = l.FormulaWithValues,
                    HasPunch = l.HasPunch,
                    ActualPvcArea = l.ActualPvcArea,
                    PunchUnitPrice = l.PunchUnitPrice,
                    PunchCost = l.PunchCost,
                    PunchFormula = l.PunchFormula,
                    PunchFormulaWithValues = l.PunchFormulaWithValues,
                    LayerTotalCost = l.LayerTotalCost
                }).ToList();
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult EdgeCost()
        {
            ViewData["Title"] = "تست ماژول هزینه لبه";
            ViewData["UseFluidLayout"] = true;
            return View(new EdgeCostTestVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult EdgeCost(EdgeCostTestVm model)
        {
            ViewData["Title"] = "تست ماژول هزینه لبه";
            ViewData["UseFluidLayout"] = true;

            model.AllCategories = Enum.GetValues<CalculationCategory>();
            model.Letters ??= new List<EdgeLetterVm>();
            model.DifficultyBands ??= new List<EdgeDifficultyBandVm>();

            var calcResult = _edgeCostCalculator.Calculate(new EdgeCostInputDto
            {
                Category = model.Category,
                EnablePunch = model.EnablePunch,
                EdgeUnitPrice = model.EdgeUnitPrice,
                PunchUnitPrice = model.PunchUnitPrice,
                LaborUnitPrice = model.LaborUnitPrice,
                Letters = model.Letters.Select(l => new EdgeLetterInputDto
                {
                    Label = l.Label,
                    Length = l.Length
                }).ToList(),
                DifficultyBands = model.DifficultyBands.Select(b => new EdgeDifficultyBandDto
                {
                    MinLength = b.MinLength,
                    MaxLength = b.MaxLength,
                    Coefficient = b.Coefficient
                }).ToList()
            });

            model.HasResult = true;
            model.IsSuccess = calcResult.isSuccess;
            model.Message = calcResult.message;

            if (calcResult.isSuccess && calcResult.data is not null)
            {
                var data = calcResult.data;
                model.Category = data.Category;
                model.CategoryDisplayName = data.CategoryDisplayName;
                model.Applies = data.Applies;
                model.IncludesPunch = data.IncludesPunch;
                model.RuleDescription = data.RuleDescription;
                model.ActualPvcLengthResult = data.ActualPvcLength;
                model.RelativePvcLength = data.RelativePvcLength;
                model.TotalEdgeCost = data.TotalEdgeCost;
                model.LetterContributions = data.LetterContributions.Select(c => new EdgeLetterContributionVm
                {
                    Index = c.Index,
                    Label = c.Label,
                    Length = c.Length,
                    MinLength = c.MinLength,
                    MaxLength = c.MaxLength,
                    Coefficient = c.Coefficient,
                    Contribution = c.Contribution,
                    FormulaWithValues = c.FormulaWithValues
                }).ToList();
                model.EdgeMaterial = MapComponent(data.EdgeMaterial);
                model.EdgePunch = MapComponent(data.EdgePunch);
                model.EdgeLabor = MapComponent(data.EdgeLabor);
            }

            return View(model);
        }

        private static EdgeCostComponentVm? MapComponent(EdgeCostComponentDto? dto) =>
            dto is null
                ? null
                : new EdgeCostComponentVm
                {
                    Name = dto.Name,
                    Formula = dto.Formula,
                    FormulaWithValues = dto.FormulaWithValues,
                    Amount = dto.Amount
                };
    }
}
