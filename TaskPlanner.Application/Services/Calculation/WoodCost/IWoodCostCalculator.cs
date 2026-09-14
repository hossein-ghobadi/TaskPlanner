using TaskPlanner.Common.CommonDto;

namespace TaskPlanner.Application.Services.Calculation.WoodCost
{
    public interface IWoodCostCalculator
    {
        ResultDto<WoodCostResultDto> Calculate(WoodCostInputDto input);
    }
}
