using Microsoft.AspNetCore.Identity;
using System;

namespace MadaServices.Models
{
    public class User : IdentityUser<int> 
    {
        [PersonalData]
        public string FullName { get; set; } = string.Empty;

        // On le rend nullable (?) pour que MySQL accepte l'absence de valeur
        // et on l'initialise à string.Empty par sécurité.
        public string? Phone { get; set; } = string.Empty; 
        
        public string? ImageUrl { get; set; } = "/images/default-avatar.png";
        
        public string? Address { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}