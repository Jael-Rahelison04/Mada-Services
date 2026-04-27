// Models/Review.cs
using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MadaServices.Models
{
    public class Review
    {
        [Key]
        public int Id { get; set; }

        // ✅ CHANGEMENT : int → decimal pour accepter 3.5, 4.5, etc.
        [Range(0.5, 5.0, ErrorMessage = "La note doit être entre 0.5 et 5.")]
        [Column(TypeName = "decimal(3,1)")]  // ex: 3.5, 4.0, 5.0
        public decimal Rating { get; set; }

        [Required]
        public string Comment { get; set; } = string.Empty;

        public string CustomerName { get; set; } = string.Empty;

        public string AuthorName => CustomerName;

        public DateTime DatePosted { get; set; } = DateTime.Now;

        [Required]
        public int ClientId { get; set; }

        [Required]
        public int ProviderId { get; set; }

        [ForeignKey("ProviderId")]
        public virtual Provider? Provider { get; set; }
    }
}