namespace TaskPlanner.Domain.Entities.Leaves
{
    public static class LeaveTypeDefaults
    {
        public static IReadOnlyList<LeaveType> CreateSeedTypes() => new List<LeaveType>
        {
            new() { Id = 1, Name = "استحقاقی", Color = "#3b82f6", IsHourlyAllowed = false, IsActive = true, SortOrder = 1 },
            new() { Id = 2, Name = "استعلاجی", Color = "#ef4444", IsHourlyAllowed = false, IsActive = true, SortOrder = 2 },
            new() { Id = 3, Name = "ساعتی", Color = "#8b5cf6", IsHourlyAllowed = true, IsActive = true, SortOrder = 3 },
            new() { Id = 4, Name = "بدون حقوق", Color = "#6b7280", IsHourlyAllowed = false, IsActive = true, SortOrder = 4 },
            new() { Id = 5, Name = "ازدواج", Color = "#ec4899", IsHourlyAllowed = false, IsActive = true, SortOrder = 5 },
            new() { Id = 6, Name = "سایر", Color = "#f59e0b", IsHourlyAllowed = true, IsActive = true, SortOrder = 6 }
        };
    }
}
