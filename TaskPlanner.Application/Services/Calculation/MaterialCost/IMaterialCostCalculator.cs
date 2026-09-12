using TaskPlanner.Common.CommonDto;

namespace TaskPlanner.Application.Services.Calculation.MaterialCost
{
    public interface IMaterialCostCalculator
    {
        ResultDto<MaterialCostResultDto> Calculate(MaterialCostInputDto input);
    }
}
