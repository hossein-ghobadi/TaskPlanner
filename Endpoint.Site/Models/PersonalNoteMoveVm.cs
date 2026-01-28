using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class PersonalNoteMoveVm
    {
        [Required]
        public int NoteId { get; set; }

        // پوشه مقصد (null یعنی بدون پوشه)
        public int? TargetFolderId { get; set; }
    }
}

