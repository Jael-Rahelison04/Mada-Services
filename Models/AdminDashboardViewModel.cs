using System;
using System.Collections.Generic;

namespace MadaServices.Models
{
    public class AdminDashboardViewModel
    {
        public int TotalUsers { get; set; }
        public int TotalProviders { get; set; }
        public int TotalClients { get; set; }
        public int VerifiedProvidersCount { get; set; }
        
        public List<UserWithRole> RecentUsers { get; set; } = new();
        public List<Category> Categories { get; set; } = new();
        public List<City> Cities { get; set; } = new(); // L'erreur venait d'ici
        public List<Review> RecentReviews { get; set; } = new();
    }

    public class UserWithRole
    {
        public User User { get; set; } = default!;
        public string Role { get; set; } = default!;
        public bool IsVerified { get; set; }
        public bool IsSuspended { get; set; }
    }

    // On définit City ici puisqu'elle n'existe pas ailleurs
    public class City
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
    }
}