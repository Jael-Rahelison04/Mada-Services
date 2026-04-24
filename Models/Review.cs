// Models/Review.cs (VERSION CORRIGÉE)
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MadaServices.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        [Range(1, 5, ErrorMessage = "La note doit être entre 1 et 5.")]
        public int Rating { get; set; }

        [Required]
        public string Comment { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        // Propriété calculée pour compatibilité avec le Dashboard
        public string AuthorName => CustomerName;

        public DateTime DatePosted { get; set; } = DateTime.Now;

        // ✅ CORRECTION C1 : ClientId est maintenant un int (cohérent avec User.Id)
        [Required]
        public int ClientId { get; set; }

        [Required]
        public int ProviderId { get; set; }

        [ForeignKey("ProviderId")]
        public virtual Provider? Provider { get; set; }
    }
}