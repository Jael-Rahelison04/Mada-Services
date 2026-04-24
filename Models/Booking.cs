// Models/Booking.cs (VERSION CORRIGÉE)
using System;
using System.ComponentModel.DataAnnotations;

namespace MadaServices.Models
{
    public class Booking
    {
        public int Id { get; set; }

        [Required]
        public string CustomerName { get; set; } = string.Empty;

        [Required]
        public string ServiceName { get; set; } = string.Empty;

        public decimal TotalPrice { get; set; }

        // Statuts possibles : Pending, Accepted, Completed, Cancelled
        public string Status { get; set; } = "Pending";

        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Clé étrangère vers le prestataire
        public int ProviderId { get; set; }

        // ✅ CORRECTION C2 : Ajout du ClientId pour identifier le client de façon fiable
        // Remplace la recherche par nom (qui causait des collisions si deux clients
        // ont le même nom)
        public int ClientId { get; set; }
    }
}