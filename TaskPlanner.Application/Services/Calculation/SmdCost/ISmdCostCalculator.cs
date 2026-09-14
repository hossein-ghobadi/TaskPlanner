using TaskPlanner.Common.CommonDto;

namespace TaskPlanner.Application.Services.Calculation.SmdCost
{
    public interface ISmdCostCalculator
    {
        ResultDto<SmdCostResultDto> Calculate(SmdCostInputDto input);
    }
}
