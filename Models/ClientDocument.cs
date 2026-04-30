using System;
using System.ComponentModel.DataAnnotations;
 
namespace MadaServices.Models
{
    public class ClientDocument
    {
        public int Id { get; set; }
 
        public int UserId { get; set; }
        public User User { get; set; } = null!;
 
        [Required]
        [MaxLength(50)]
        public string DocumentType { get; set; } = string.Empty;
        // Valeurs attendues : "CIN" | "CertResidence" | "CertTravail" | "CV"
 
        [Required]
        public string FilePath { get; set; } = string.Empty;
 
        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;
 
        [MaxLength(20)]
        public string Status { get; set; } = "Pending";
        // "Pending" | "Approved" | "Rejected"
    }
}