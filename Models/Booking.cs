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
        
        // Statuts : Pending, Accepted, Completed, Cancelled
        public string Status { get; set; } = "Pending";
        
        public DateTime CreatedAt { get; set; } = DateTime.Now;

        // Clé étrangère vers le prestataire
        public int ProviderId { get; set; }
    }
}