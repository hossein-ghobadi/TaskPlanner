namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    public static class FeaturePageStateTypeExtensions
    {
        public static string GetDisplayName(this FeaturePageStateType stateType) => stateType switch
        {
            FeaturePageStateType.Loading => "Loading",
            FeaturePageStateType.Empty => "Empty",
            FeaturePageStateType.Error => "Error",
            FeaturePageStateType.Success => "Success",
            FeaturePageStateType.Disabled => "Disabled",
            FeaturePageStateType.Unauthorized => "Unauthorized",
            _ => "نامشخص"
        };
    }
}
