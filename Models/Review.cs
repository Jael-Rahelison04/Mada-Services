using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MadaServices.Models 
{
    public class Review 
    {
        [Key]
        public int Id { get; set; }
        
        [Range(1, 5)]
        public int Rating { get; set; } 
        
        [Required]
        public string Comment { get; set; } = string.Empty;
        
        public string CustomerName { get; set; } = string.Empty;

        // On ajoute AuthorName pour correspondre au Dashboard
        public string AuthorName => CustomerName; 

        public DateTime DatePosted { get; set; } = DateTime.Now;

        [Required]
        public string ClientId { get; set; } = string.Empty;

        [Required]
        public int ProviderId { get; set; } 

        [ForeignKey("ProviderId")]
        public virtual Provider? Provider { get; set; }
    }
}