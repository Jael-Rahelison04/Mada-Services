using Microsoft.AspNetCore.Http;
using System.Collections.Generic;

namespace MadaServices.Models.ViewModels
{
    public class SettingsViewModel
    {
        public int Id { get; set; }
        public string FullName { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? PhoneNumber { get; set; }
        public string? Address { get; set; }
        public string? ImageUrl { get; set; }

        // ── Nouveaux champs
        public string? JobTitle { get; set; }
        public bool IsVerified { get; set; }
        public IFormFile? ProfilePicture { get; set; }

        // ── Documents KYC
        public List<ClientDocumentDto>? Documents { get; set; }
    }

    public class ClientDocumentDto
    {
        public int Id { get; set; }
        public string DocumentType { get; set; } = string.Empty;   // "CIN", "CertResidence", "CertTravail", "CV"
        public string FilePath { get; set; } = string.Empty;
        public string Status { get; set; } = "Pending";            // "Pending" | "Approved" | "Rejected"
        public DateTime UploadedAt { get; set; }
    }
}