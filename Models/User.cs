using Microsoft.AspNetCore.Identity;

namespace MadaServices.Models
{
    public class User : IdentityUser<int> 
    {
        public string FullName { get; set; } = default!;
        public string Phone { get; set; } = default!;
        public DateTime CreatedAt { get; set; } = DateTime.Now;
    }
}