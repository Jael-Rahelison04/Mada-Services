using Microsoft.AspNetCore.Identity;
using System;

namespace MadaServices.Models
{
    public class User : IdentityUser<int> 
    {
        [PersonalData]
        public string FullName { get; set; } = string.Empty;

        public string? Phone { get; set; } = string.Empty; 
        
        public string? ImageUrl { get; set; } = "/images/default-avatar.png";
        
        public string? Address { get; set; }

        // Models/User.cs — ajouter cette propriété
        public DateTime? SuspendedAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        public string? JobTitle { get; set; }
        public bool IsVerified { get; set; } = false;
        public ICollection<ClientDocument> Documents { get; set; } = new List<ClientDocument>();

        // Propriété à ajouter sur User.cs
        public int DeletedReviewsCount { get; set; } = 0;
    }
}