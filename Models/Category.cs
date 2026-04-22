using System.ComponentModel.DataAnnotations;

namespace MadaServices.Models
{
    public class Category
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        // Pour stocker le nom de l'icône Bootstrap (ex: "bi-tools")
        public string Icon { get; set; } = "bi-briefcase";

        // Relation optionnelle : une catégorie peut avoir plusieurs prestataires
        public virtual ICollection<Provider> Providers { get; set; } = new List<Provider>();
    }
}