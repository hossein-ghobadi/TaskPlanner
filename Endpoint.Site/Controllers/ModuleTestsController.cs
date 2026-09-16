using Endpoint.Site.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using TaskPlanner.Application.Services.Calculation.CrystalCost;
using TaskPlanner.Application.Services.Calculation.CutSpecificCost;
using TaskPlanner.Application.Services.Calculation.EdgeCost;
using TaskPlanner.Application.Services.Calculation.GlueCost;
using TaskPlanner.Application.Services.Calculation.MaterialCost;
using TaskPlanner.Application.Services.Calculation.MetalsCost;
using TaskPlanner.Application.Services.Calculation.PaintCost;
using TaskPlanner.Application.Services.Calculation.PvcCost;
using TaskPlanner.Application.Services.Calculation.SmdCost;
using TaskPlanner.Application.Services.Calculation.VacuumWageCost;
using TaskPlanner.Application.Services.Calculation.WoodCost;
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
        private readonly ISmdCostCalculator _smdCostCalculator;
        private readonly IMetalsCostCalculator _metalsCostCalculator;
        private readonly IPvcCostCalculator _pvcCostCalculator;
        private readonly IGlueCostCalculator _glueCostCalculator;
        private readonly ICrystalCostCalculator _crystalCostCalculator;
        private readonly IPaintCostCalculator _paintCostCalculator;
        private readonly IWoodCostCalculator _woodCostCalculator;
        private readonly IVacuumWageCostCalculator _vacuumWageCostCalculator;
        private readonly ICutSpecificCostCalculator _cutSpecificCostCalculator;

        public ModuleTestsController(
            IMaterialCostCalculator materialCostCalculator,
            IEdgeCostCalculator edgeCostCalculator,
            ISmdCostCalculator smdCostCalculator,
            IMetalsCostCalculator metalsCostCalculator,
            IPvcCostCalculator pvcCostCalculator,
            IGlueCostCalculator glueCostCalculator,
            ICrystalCostCalculator crystalCostCalculator,
            IPaintCostCalculator paintCostCalculator,
            IWoodCostCalculator woodCostCalculator,
            IVacuumWageCostCalculator vacuumWageCostCalculator,
            ICutSpecificCostCalculator cutSpecificCostCalculator)
        {
            _materialCostCalculator = materialCostCalculator;
            _edgeCostCalculator = edgeCostCalculator;
            _smdCostCalculator = smdCostCalculator;
            _metalsCostCalculator = metalsCostCalculator;
            _pvcCostCalculator = pvcCostCalculator;
            _glueCostCalculator = glueCostCalculator;
            _crystalCostCalculator = crystalCostCalculator;
            _paintCostCalculator = paintCostCalculator;
            _woodCostCalculator = woodCostCalculator;
            _vacuumWageCostCalculator = vacuumWageCostCalculator;
            _cutSpecificCostCalculator = cutSpecificCostCalculator;
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
                EnableLayer1Punch = model.EnableLayer1Punch,
                EnableLayer2Punch = model.EnableLayer2Punch,
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
                model.EdgeMaterial = MapEdgeComponent(data.EdgeMaterial);
                model.EdgePunch = MapEdgeComponent(data.EdgePunch);
                model.EdgeLabor = MapEdgeComponent(data.EdgeLabor);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult SmdCost()
        {
            ViewData["Title"] = "تست ماژول هزینه SMD";
            ViewData["UseFluidLayout"] = true;
            return View(new SmdCostTestVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult SmdCost(SmdCostTestVm model)
        {
            ViewData["Title"] = "تست ماژول هزینه SMD";
            ViewData["UseFluidLayout"] = true;
            model.AllCategories = Enum.GetValues<CalculationCategory>();
            model.Letters ??= new List<SmdLetterVm>();
            model.PenDiameterBands ??= new List<SmdPenDiameterBandVm>();

            var calcResult = _smdCostCalculator.Calculate(new SmdCostInputDto
            {
                Category = model.Category,
                EnableFrontSmd = model.EnableFrontSmd,
                EnableBackSmd = model.EnableBackSmd,
                ActualPvcLength = model.ActualPvcLengthInput,
                FrontGoldenNumber = model.FrontGoldenNumber,
                BackGoldenNumber = model.BackGoldenNumber,
                FrontUnitPrice = model.FrontUnitPrice,
                BackUnitPrice = model.BackUnitPrice,
                Letters = model.Letters.Select(l => new SmdLetterInputDto
                {
                    Label = l.Label,
                    Length = l.Length,
                    Area = l.Area
                }).ToList(),
                PenDiameterBands = model.PenDiameterBands.Select(b => new SmdPenDiameterBandDto
                {
                    MinDiameter = b.MinDiameter,
                    MaxDiameter = b.MaxDiameter,
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
                model.IncludesFrontSmd = data.IncludesFrontSmd;
                model.IncludesBackSmd = data.IncludesBackSmd;
                model.UsesPenDiameterMode = data.UsesPenDiameterMode;
                model.RuleDescription = data.RuleDescription;
                model.ActualPvcLength = data.ActualPvcLength;
                model.TotalSmdCost = data.TotalSmdCost;
                model.LetterContributions = data.LetterContributions.Select(c => new SmdLetterContributionVm
                {
                    Index = c.Index,
                    Label = c.Label,
                    Length = c.Length,
                    Area = c.Area,
                    PenDiameter = c.PenDiameter,
                    MinDiameter = c.MinDiameter,
                    MaxDiameter = c.MaxDiameter,
                    Coefficient = c.Coefficient,
                    Contribution = c.Contribution,
                    PenDiameterFormula = c.PenDiameterFormula,
                    ContributionFormula = c.ContributionFormula
                }).ToList();
                model.FrontSmd = MapSmdSide(data.FrontSmd);
                model.BackSmd = MapSmdSide(data.BackSmd);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult MetalsCost()
        {
            ViewData["Title"] = "تست ماژول هزینه فلزات";
            ViewData["UseFluidLayout"] = true;
            return View(new MetalsCostTestVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult MetalsCost(MetalsCostTestVm model)
        {
            ViewData["Title"] = "تست ماژول هزینه فلزات";
            ViewData["UseFluidLayout"] = true;
            model.MetalsCategories = Enum.GetValues<CalculationCategory>()
                .Where(c => c.CanHaveMetalsCost())
                .ToArray();

            var calcResult = _metalsCostCalculator.Calculate(new MetalsCostInputDto
            {
                Category = model.Category,
                ActualPvcLength = model.ActualPvcLength,
                BaseUnitPrice = model.BaseUnitPrice,
                DoughyConsumedArea = model.DoughyConsumedArea,
                DoughyUnitPrice = model.DoughyUnitPrice,
                DoughyLaborUnitPrice = model.DoughyLaborUnitPrice,
                EdgeSize = model.EdgeSize,
                PlasticEdgeUnitPrice = model.PlasticEdgeUnitPrice,
                AssemblyUnitPrice = model.AssemblyUnitPrice,
                RingEdgeUnitPrice = model.RingEdgeUnitPrice,
                DoubleEdgeRingLaborUnitPrice = model.DoubleEdgeRingLaborUnitPrice
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
                model.IncludesDoubleEdgeRing = data.IncludesDoubleEdgeRing;
                model.RuleDescription = data.RuleDescription;
                model.ActualPvcLengthResult = data.ActualPvcLength;
                model.TotalMetalsCost = data.TotalMetalsCost;
                model.BaseCost = MapMetalsComponent(data.BaseCost);
                model.DoughyCost = MapMetalsComponent(data.DoughyCost);
                model.MetalPlasticCost = MapMetalsComponent(data.MetalPlasticCost);
                model.AssemblyLaborCost = MapMetalsComponent(data.AssemblyLaborCost);
                model.DoubleEdgeRingCost = MapMetalsComponent(data.DoubleEdgeRingCost);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult PvcCost()
        {
            ViewData["Title"] = "تست ماژول هزینه PVC";
            ViewData["UseFluidLayout"] = true;
            return View(new PvcCostTestVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PvcCost(PvcCostTestVm model)
        {
            ViewData["Title"] = "تست ماژول هزینه PVC";
            ViewData["UseFluidLayout"] = true;
            model.AllCategories = Enum.GetValues<CalculationCategory>();

            var calcResult = _pvcCostCalculator.Calculate(new PvcCostInputDto
            {
                Category = model.Category,
                PvcConsumedArea = model.PvcConsumedArea,
                BacklightConsumedArea = model.BacklightConsumedArea,
                PvcUnitPrice = model.PvcUnitPrice
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
                model.RuleDescription = data.RuleDescription;
                model.TotalPvcCost = data.TotalPvcCost;
                model.PvcCost = MapPvcComponent(data.PvcCost);
                model.BacklightPvcCost = MapPvcComponent(data.BacklightPvcCost);
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult GlueCost()
        {
            ViewData["Title"] = "تست ماژول هزینه چسب";
            ViewData["UseFluidLayout"] = true;
            return View(new GlueCostTestVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult GlueCost(GlueCostTestVm model)
        {
            ViewData["Title"] = "تست ماژول هزینه چسب";
            ViewData["UseFluidLayout"] = true;
            model.AllCategories = Enum.GetValues<CalculationCategory>();

            var calcResult = _glueCostCalculator.Calculate(new GlueCostInputDto
            {
                Category = model.Category,
                Layer1ConsumedArea = model.Layer1ConsumedArea,
                Layer2ConsumedArea = model.Layer2ConsumedArea,
                ActualPvcLength = model.ActualPvcLength,
                GlueUnitPrice = model.GlueUnitPrice
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
                model.UsesLayerAreas = data.UsesLayerAreas;
                model.RuleDescription = data.RuleDescription;
                model.Formula = data.Formula;
                model.FormulaWithValues = data.FormulaWithValues;
                model.GlueCost = data.GlueCost;
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult CrystalCost()
        {
            ViewData["Title"] = "تست ماژول هزینه کریستال";
            ViewData["UseFluidLayout"] = true;
            return View(new CrystalCostTestVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CrystalCost(CrystalCostTestVm model)
        {
            ViewData["Title"] = "تست ماژول هزینه کریستال";
            ViewData["UseFluidLayout"] = true;
            model.AllCategories = Enum.GetValues<CalculationCategory>();

            var calcResult = _crystalCostCalculator.Calculate(new CrystalCostInputDto
            {
                Category = model.Category,
                ActualPvcArea = model.ActualPvcArea,
                CrystalUnitPrice = model.CrystalUnitPrice
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
                model.RuleDescription = data.RuleDescription;
                model.Formula = data.Formula;
                model.FormulaWithValues = data.FormulaWithValues;
                model.CrystalCost = data.CrystalCost;
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult PaintCost()
        {
            ViewData["Title"] = "تست ماژول هزینه رنگ";
            ViewData["UseFluidLayout"] = true;
            return View(new PaintCostTestVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult PaintCost(PaintCostTestVm model)
        {
            ViewData["Title"] = "تست ماژول هزینه رنگ";
            ViewData["UseFluidLayout"] = true;
            model.AllCategories = Enum.GetValues<CalculationCategory>();
            model.PaintCategories = Enum.GetValues<CalculationCategory>()
                .Where(c => c.CanHavePaintCost())
                .ToArray();

            var calcResult = _paintCostCalculator.Calculate(new PaintCostInputDto
            {
                Category = model.Category,
                LayerConsumedArea = model.LayerConsumedArea,
                ActualPvcLength = model.ActualPvcLength,
                EdgeSize = model.EdgeSize,
                PaintUnitPrice = model.PaintUnitPrice
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
                model.RuleDescription = data.RuleDescription;
                model.Formula = data.Formula;
                model.FormulaWithValues = data.FormulaWithValues;
                model.PaintCost = data.PaintCost;
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult WoodCost()
        {
            ViewData["Title"] = "تست ماژول هزینه چوب";
            ViewData["UseFluidLayout"] = true;
            return View(new WoodCostTestVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult WoodCost(WoodCostTestVm model)
        {
            ViewData["Title"] = "تست ماژول هزینه چوب";
            ViewData["UseFluidLayout"] = true;
            model.AllCategories = Enum.GetValues<CalculationCategory>();

            var calcResult = _woodCostCalculator.Calculate(new WoodCostInputDto
            {
                Category = model.Category,
                WoodConsumedArea = model.WoodConsumedArea,
                WoodWaste = model.WoodWaste,
                WoodUnitPrice = model.WoodUnitPrice
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
                model.RuleDescription = data.RuleDescription;
                model.Formula = data.Formula;
                model.FormulaWithValues = data.FormulaWithValues;
                model.WoodCost = data.WoodCost;
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult VacuumWageCost()
        {
            ViewData["Title"] = "تست ماژول دستمزد وکیوم";
            ViewData["UseFluidLayout"] = true;
            return View(new VacuumWageCostTestVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult VacuumWageCost(VacuumWageCostTestVm model)
        {
            ViewData["Title"] = "تست ماژول دستمزد وکیوم";
            ViewData["UseFluidLayout"] = true;
            model.AllCategories = Enum.GetValues<CalculationCategory>();

            var calcResult = _vacuumWageCostCalculator.Calculate(new VacuumWageCostInputDto
            {
                Category = model.Category,
                EdgeSize = model.EdgeSize,
                VacuumUnitPrice = model.VacuumUnitPrice
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
                model.RuleDescription = data.RuleDescription;
                model.Formula = data.Formula;
                model.FormulaWithValues = data.FormulaWithValues;
                model.VacuumWageCost = data.VacuumWageCost;
            }

            return View(model);
        }

        [HttpGet]
        public IActionResult CutSpecificCost()
        {
            ViewData["Title"] = "تست ماژول اختصاصی برش";
            ViewData["UseFluidLayout"] = true;
            return View(new CutSpecificCostTestVm());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult CutSpecificCost(CutSpecificCostTestVm model)
        {
            ViewData["Title"] = "تست ماژول اختصاصی برش";
            ViewData["UseFluidLayout"] = true;
            model.AllCategories = Enum.GetValues<CalculationCategory>();

            var calcResult = _cutSpecificCostCalculator.Calculate(new CutSpecificCostInputDto
            {
                Category = model.Category,
                ActualPvcLength = model.ActualPvcLength,
                CutUnitPrice = model.CutUnitPrice,
                MultiLayerCoefficient = model.MultiLayerCoefficient
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
                model.RuleDescription = data.RuleDescription;
                model.Formula = data.Formula;
                model.FormulaWithValues = data.FormulaWithValues;
                model.CutCost = data.CutCost;
            }

            return View(model);
        }

        private static EdgeCostComponentVm? MapEdgeComponent(EdgeCostComponentDto? dto) =>
            dto is null
                ? null
                : new EdgeCostComponentVm
                {
                    Name = dto.Name,
                    Formula = dto.Formula,
                    FormulaWithValues = dto.FormulaWithValues,
                    Amount = dto.Amount
                };

        private static SmdSideResultVm? MapSmdSide(SmdSideResultDto? dto) =>
            dto is null
                ? null
                : new SmdSideResultVm
                {
                    Name = dto.Name,
                    Count = dto.Count,
                    UnitPrice = dto.UnitPrice,
                    Cost = dto.Cost,
                    CountFormula = dto.CountFormula,
                    CountFormulaWithValues = dto.CountFormulaWithValues,
                    CostFormula = dto.CostFormula,
                    CostFormulaWithValues = dto.CostFormulaWithValues
                };

        private static MetalsCostComponentVm? MapMetalsComponent(MetalsCostComponentDto? dto) =>
            dto is null
                ? null
                : new MetalsCostComponentVm
                {
                    Name = dto.Name,
                    Formula = dto.Formula,
                    FormulaWithValues = dto.FormulaWithValues,
                    Amount = dto.Amount
                };

        private static PvcCostComponentVm? MapPvcComponent(PvcCostComponentDto? dto) =>
            dto is null
                ? null
                : new PvcCostComponentVm
                {
                    Name = dto.Name,
                    Formula = dto.Formula,
                    FormulaWithValues = dto.FormulaWithValues,
                    Amount = dto.Amount
                };
    }
}
