using System.ComponentModel.DataAnnotations;

namespace Endpoint.Site.Models
{
    public class UserListVm
    {
        public string Id { get; set; } = "";
        public string UserName { get; set; } = "";
        public string Email { get; set; } = "";
        public string FullName { get; set; } = "";
        public string Phone { get; set; } = "";
        public bool IsActive { get; set; }
        public DateTime InsertTime { get; set; }
        public bool EmailConfirmed { get; set; }
        public List<string> Roles { get; set; } = new List<string>();
        public int ProjectCount { get; set; }
    }
}

