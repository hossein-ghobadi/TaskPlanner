using Microsoft.AspNetCore.Identity;
using System;
using System.Collections.Generic;
using TaskPlanner.Domain.Entities.Notifications;

namespace TaskPlanner.Domain.Entities.Users
{
    public class User : IdentityUser
    {
        public string FullName { get; set; }
        public string? Gender { get; set; }
        public int? Country { get; set; }
        public int? Province { get; set; }//Province
        public int? City { get; set; }
        public DateTime? Age { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
        public bool IsRemove { get; set; } = false;
        public bool IsActive { get; set; } = true;
        public double? Latitude { get; set; }
        public double? Longitude { get; set; }
        public string? Documents { get; set; }
        public DateTime InsertTime { get; set; }
        public bool IsVarify { get; set; } = false;
        public bool IsCentralOffice { get; set; } = false;

        public ICollection<Notification> Notifications { get; set; } = new List<Notification>();
    }
}
