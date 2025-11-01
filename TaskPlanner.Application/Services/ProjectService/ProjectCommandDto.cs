namespace TaskPlanner.Application.Services.ProjectService
{
    /// <summary>
    /// DTO برای ایجاد پروژه
    /// </summary>
    public class CreateProjectDto
    {
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public List<string> SelectedUserIds { get; set; } = new List<string>();
    }

    /// <summary>
    /// DTO برای ویرایش پروژه
    /// </summary>
    public class UpdateProjectDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = null!;
        public string? Description { get; set; }
        public List<string> SelectedUserIds { get; set; } = new List<string>();
    }
}

