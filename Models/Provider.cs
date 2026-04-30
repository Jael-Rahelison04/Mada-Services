// Models/Provider.cs (VERSION CORRIGÉE)
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;

namespace MadaServices.Models
{
    public class Provider : User
    {
        // --- Propriétés professionnelles ---

        public string City { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;

        [Column(TypeName = "decimal(18,2)")]
        public decimal HourlyRate { get; set; }

        // ✅ CORRECTION P11 : ProfileImageUrl SUPPRIMÉ.
        // On utilise uniquement ImageUrl hérité de User (déjà dans AspNetUsers).
        // Avant : public string? ProfileImageUrl { get; set; }
        // Après : on utilise this.ImageUrl (hérité)

        // --- Relation avec Category ---
        public int? CategoryId { get; set; }

        [ForeignKey("CategoryId")]
        public virtual Category? Category { get; set; }

        // --- Statuts et vérification ---
        public bool IsPaused { get; set; } = false;

        // Statuts possibles : "NotSubmitted", "Pending", "Approved", "Rejected"
        public string VerificationStatus { get; set; } = "NotSubmitted";
        public bool HasSubmittedDocs { get; set; } = false;

        // ✅ CORRECTION P10 : Champ pour stocker le chemin du document soumis
        public string? CinDocumentPath          { get; set; }  // Obligatoire
        public string? CvDocumentPath           { get; set; }  // Obligatoire
        public string? ResidenceCertPath        { get; set; }  // Obligatoire
        public string? DiplomaDocumentPath      { get; set; }  // Facultatif

        // URL externe optionnelle (lien vers site perso, LinkedIn, etc.)
        public string? PortfolioUrl { get; set; }

        // --- Collections (relations) ---
        public virtual ICollection<Review> Reviews { get; set; }
        public virtual ICollection<PortfolioItem> PortfolioItems { get; set; }
        public virtual ICollection<Booking> Bookings { get; set; }
        public virtual ICollection<Availability> Availabilities { get; set; }

        // --- Propriété calculée (non mappée en base) ---
        [NotMapped]
        public double AverageRating
        {
            get
            {
                return (Reviews != null && Reviews.Any())
                    ? Reviews.Average(r => (double)r.Rating)
                    : 0.0;
            }
        }

        // --- Propriété calculée : nombre de photos portfolio ---
        [NotMapped]
        public int PortfolioCount => PortfolioItems?.Count ?? 0;

        public Provider() : base()
        {
            Reviews = new HashSet<Review>();
            PortfolioItems = new HashSet<PortfolioItem>();
            Bookings = new HashSet<Booking>();
            Availabilities = new HashSet<Availability>();
        }
    }
}