using TaskPlanner.Common.CommonDto;

namespace TaskPlanner.Application.Services.Calculation.GlueCost
{
    public interface IGlueCostCalculator
    {
        ResultDto<GlueCostResultDto> Calculate(GlueCostInputDto input);
    }
}
