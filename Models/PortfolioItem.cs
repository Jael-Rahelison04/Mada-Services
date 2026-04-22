using System.ComponentModel.DataAnnotations;

namespace MadaServices.Models
{
    public class PortfolioItem
    {
        public int Id { get; set; }
        public string? ImageUrl { get; set; }
        public string? Description { get; set; }
        
        // Clé étrangère vers le Provider
        public int ProviderId { get; set; }
        public virtual Provider? Provider { get; set; }
    }
}