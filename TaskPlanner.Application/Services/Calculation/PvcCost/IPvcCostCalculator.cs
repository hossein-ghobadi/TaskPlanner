using TaskPlanner.Common.CommonDto;

namespace TaskPlanner.Application.Services.Calculation.PvcCost
{
    public interface IPvcCostCalculator
    {
        ResultDto<PvcCostResultDto> Calculate(PvcCostInputDto input);
    }
}
