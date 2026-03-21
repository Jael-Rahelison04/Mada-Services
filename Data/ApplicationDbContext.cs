using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MadaServices.Models;

namespace MadaServices.Data
{
    // IdentityDbContext configuré pour utiliser des ID de type <int>
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        // Utilisation de = default! pour éviter les warnings CS8618
        public DbSet<Provider> Providers { get; set; } = default!;
        public DbSet<Review> Reviews { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Optionnel : Configuration précise de la relation Review -> Provider
            builder.Entity<Review>()
                .HasOne(r => r.Provider)
                .WithMany() // Ou .WithMany(p => p.Reviews) si tu ajoutes une liste dans Provider
                .HasForeignKey(r => r.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}