using System.ComponentModel.DataAnnotations;

namespace MadaServices.Models
{
    public class Availability
    {
        public int Id { get; set; }
        
        // Exemple : "Lundi", "Mardi", etc.
        [Required]
        public string DayOfWeek { get; set; } = string.Empty;
        
        public bool IsActive { get; set; } = true;

        // Clé étrangère vers le prestataire
        public int ProviderId { get; set; }
    }
}