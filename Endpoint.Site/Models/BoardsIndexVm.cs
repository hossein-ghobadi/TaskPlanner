namespace Endpoint.Site.Models
{
    public class BoardsIndexVm
    {
        public string CurrentUserId { get; set; } = string.Empty;
        public List<BoardCardVm> Boards { get; set; } = new();
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int TotalBoardsCount { get; set; }
        public bool HasMoreBoards { get; set; }
    }

    public class BoardCardVm
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string CreatorUserId { get; set; } = string.Empty;
        public DateTime UpdatedAt { get; set; }
        public int TaskCount { get; set; }
    }
}
