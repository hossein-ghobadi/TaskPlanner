using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class ProjectNoteMoveVm
    {
        [Required]
        public int NoteId { get; set; }

        [Required]
        public int ProjectId { get; set; }

        // پوشه مقصد (null یعنی بدون پوشه)
        public int? TargetFolderId { get; set; }
    }
}

