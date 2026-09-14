using TaskPlanner.Common.CommonDto;

namespace TaskPlanner.Application.Services.Calculation.CrystalCost
{
    public interface ICrystalCostCalculator
    {
        ResultDto<CrystalCostResultDto> Calculate(CrystalCostInputDto input);
    }
}
