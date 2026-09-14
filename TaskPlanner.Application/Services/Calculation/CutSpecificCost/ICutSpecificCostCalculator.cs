using TaskPlanner.Common.CommonDto;

namespace TaskPlanner.Application.Services.Calculation.CutSpecificCost
{
    public interface ICutSpecificCostCalculator
    {
        ResultDto<CutSpecificCostResultDto> Calculate(CutSpecificCostInputDto input);
    }
}
