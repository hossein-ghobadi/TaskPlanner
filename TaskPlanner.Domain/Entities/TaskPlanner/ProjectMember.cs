using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using TaskPlanner.Domain.Entities.Users;

namespace TaskPlanner.Domain.Entities.TaskPlanner
{
    public class ProjectMember
    {
        public int Id { get; set; }

        public int? ProjectId { get; set; }
        public Project? Project { get; set; } = null!;

        public string UserId { get; set; } = null!;
    }

}
