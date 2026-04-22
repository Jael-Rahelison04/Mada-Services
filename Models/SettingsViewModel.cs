namespace MadaServices.Models
{
    public class SettingsViewModel
    {
        // AJOUTEZ CETTE LIGNE
        public int Id { get; set; }

        public string FullName { get; set; } = default!;
        public string Email { get; set; } = default!;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? ImageUrl { get; set; }
        public IFormFile? ProfilePicture { get; set; }
    }
}