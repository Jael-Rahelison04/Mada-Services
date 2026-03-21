using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace MadaServices.Models
{
    public class Provider : User
    {
        public string JobTitle { get; set; } = default!;
        public string City { get; set; } = default!;
        public string Description { get; set; } = default!;
        public decimal HourlyRate { get; set; }
        public bool IsVerified { get; set; }

        // --- AJOUTER CETTE LIGNE : Pour corriger l'erreur dans Details.cshtml ---
        public string ImageUrl { get; set; } = "/images/default-avatar.png";

        // --- RELATION AVEC LES AVIS ---
        // Utiliser virtual ICollection est la norme pour Entity Framework
        public virtual ICollection<Review> Reviews { get; set; } = new List<Review>();

        // --- PORTFOLIO ET DOCUMENTS ---
        public List<string> PortfolioImages { get; set; } = new List<string>();
        public bool HasSubmittedDocs { get; set; } = false;

        // --- CONSTRUCTEURS ---

        // Constructeur vide indispensable pour Entity Framework
        public Provider() : base() { }

        // Constructeur spécifique pour ton usage
        public Provider(string name, string job, string city) : base()
        {
            this.FullName = name;
            this.JobTitle = job;
            this.City = city;
            this.Description = ""; 
            this.IsVerified = false; 
        }
    }
}