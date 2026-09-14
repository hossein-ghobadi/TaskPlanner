using TaskPlanner.Common.CommonDto;

namespace TaskPlanner.Application.Services.Calculation.PaintCost
{
    public interface IPaintCostCalculator
    {
        ResultDto<PaintCostResultDto> Calculate(PaintCostInputDto input);
    }
}
