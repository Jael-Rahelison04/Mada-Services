using Microsoft.AspNetCore.Http; // Nécessaire pour IFormFile

namespace MadaServices.Models
{
    public class EditProfileViewModel
    {
        public string FullName { get; set; } = default!;
        public string JobTitle { get; set; } = default!;
        public string City { get; set; } = default!;
        public string Description { get; set; } = default!;
        public decimal HourlyRate { get; set; }

        // CORRECTION 1 : Renommer Phone en PhoneNumber pour la vue
        public string? PhoneNumber { get; set; } 

        // CORRECTION 2 : Ajouter ExistingPhotoPath (réclamé par l'erreur CS1061)
        public string? ExistingPhotoPath { get; set; }

        // CORRECTION 3 : Ajouter PhotoFile pour permettre l'upload
        public IFormFile? PhotoFile { get; set; }
    }
}