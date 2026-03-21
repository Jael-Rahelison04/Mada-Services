using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace MadaServices.Models 
{
    public class Review 
    {
        public int Id { get; set; }
        
        [Range(1, 5)]
        public int Rating { get; set; } 
        
        public string Comment { get; set; } = string.Empty;
        
        // --- INDISPENSABLE : Pour savoir quel client a posté l'avis ---
        // C'est ce champ qui est utilisé dans CustomerController
        public string CustomerName { get; set; } = string.Empty;

        // --- AJOUT DE LA DATE : Pour corriger l'erreur CS0117 ---
        public DateTime DatePosted { get; set; } = DateTime.Now;

        // 1. Clé étrangère
        public int ProviderId { get; set; } 

        // 2. Navigation vers le prestataire
        [ForeignKey("ProviderId")]
        public virtual Provider Provider { get; set; } = default!;
    }
}