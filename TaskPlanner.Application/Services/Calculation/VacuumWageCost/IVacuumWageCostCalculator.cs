using TaskPlanner.Common.CommonDto;

namespace TaskPlanner.Application.Services.Calculation.VacuumWageCost
{
    public interface IVacuumWageCostCalculator
    {
        ResultDto<VacuumWageCostResultDto> Calculate(VacuumWageCostInputDto input);
    }
}
