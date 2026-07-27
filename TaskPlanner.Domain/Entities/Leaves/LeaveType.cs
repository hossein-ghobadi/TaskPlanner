namespace TaskPlanner.Domain.Entities.Leaves
{
    public class LeaveType
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string Color { get; set; } = "#6b7280";
        public bool IsHourlyAllowed { get; set; }
        public bool IsActive { get; set; } = true;
        public int SortOrder { get; set; }

        public ICollection<LeaveRecord> LeaveRecords { get; set; } = new List<LeaveRecord>();
    }
}
