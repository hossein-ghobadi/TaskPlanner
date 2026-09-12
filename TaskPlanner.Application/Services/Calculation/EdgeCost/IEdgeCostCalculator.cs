using TaskPlanner.Common.CommonDto;

namespace TaskPlanner.Application.Services.Calculation.EdgeCost
{
    public interface IEdgeCostCalculator
    {
        ResultDto<EdgeCostResultDto> Calculate(EdgeCostInputDto input);
    }
}
