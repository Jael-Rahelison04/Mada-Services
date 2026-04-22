using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MadaServices.Models
{
    public class Provider : User
    {
        // --- Propriétés de base ---
        [Required]
        public string JobTitle { get; set; } = string.Empty;

        public string City { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        
        [Column(TypeName = "decimal(18,2)")] 
        public decimal HourlyRate { get; set; }
        
        public bool IsVerified { get; set; }
        public string? PortfolioUrl { get; set; }

        // --- Image de profil (Résout l'erreur CS1061) ---
        public string? ProfileImageUrl { get; set; } 

        // --- Relation avec Category ---
        [Display(Name = "Catégorie")]
        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        // --- Fonctionnalités ---
        public bool IsPaused { get; set; } 
        public string VerificationStatus { get; set; } = "NotSubmitted"; 
        public bool HasSubmittedDocs { get; set; } = false;

        // --- RELATIONS (Indispensables pour le Dashboard) ---
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<PortfolioItem> PortfolioItems { get; set; }
        
        // AJOUTÉ : Pour le suivi des missions
        public virtual ICollection<Booking> Bookings { get; set; }
        
        // AJOUTÉ : Pour la gestion du calendrier
        public virtual ICollection<Availability> Availabilities { get; set; }

        // --- Propriété calculée ---
        [NotMapped] 
        public double AverageRating 
        {
            get {
                return (Reviews != null && Reviews.Any()) ? Reviews.Average(r => (double)r.Rating) : 0.0;
            }
        }

        public Provider() : base() 
        { 
            Reviews = new HashSet<Review>();
            PortfolioItems = new HashSet<PortfolioItem>();
            Bookings = new HashSet<Booking>();
            Availabilities = new HashSet<Availability>();
        }

        public Provider(string name, string job, string city) : this()
        {
            this.FullName = name;
            this.JobTitle = job;
            this.City = city;
        }
    }
}