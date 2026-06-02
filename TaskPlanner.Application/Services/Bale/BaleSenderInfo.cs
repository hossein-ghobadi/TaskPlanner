namespace TaskPlanner.Application.Services.Bale
{
    public sealed class BaleSenderInfo
    {
        public long? BaleUserId { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Username { get; set; }
        public string? Phone { get; set; }
        public string DisplayName { get; set; } = "کاربر بله";
        public bool IsChannelSender { get; set; }
        public string? ChannelTitle { get; set; }
        public bool HasKnownAuthor { get; set; }
    }
}
