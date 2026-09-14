using TaskPlanner.Common.CommonDto;

namespace TaskPlanner.Application.Services.Calculation.MetalsCost
{
    public interface IMetalsCostCalculator
    {
        ResultDto<MetalsCostResultDto> Calculate(MetalsCostInputDto input);
    }
}
