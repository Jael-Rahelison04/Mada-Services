using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MadaServices.Models;

namespace MadaServices.Data
{
    public class ApplicationDbContext : IdentityDbContext<User, IdentityRole<int>, int>
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<Review> Reviews { get; set; } = default!;

        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            // Configuration de l'entité User (notre classe)
            builder.Entity<User>(entity =>
            {
                entity.Property(u => u.UserName).HasMaxLength(100);
                entity.Property(u => u.NormalizedUserName).HasMaxLength(100);
                entity.Property(u => u.Email).HasMaxLength(100);
                entity.Property(u => u.NormalizedEmail).HasMaxLength(100);
            });

            // Configuration des rôles
            builder.Entity<IdentityRole<int>>(entity =>
            {
                entity.Property(r => r.Name).HasMaxLength(100);
                entity.Property(r => r.NormalizedName).HasMaxLength(100);
            });

            // Tables de logins et tokens (déjà à 100)
            builder.Entity<IdentityUserLogin<int>>(entity =>
            {
                entity.Property(l => l.LoginProvider).HasMaxLength(100);
                entity.Property(l => l.ProviderKey).HasMaxLength(100);
            });

            builder.Entity<IdentityUserToken<int>>(entity =>
            {
                entity.Property(t => t.LoginProvider).HasMaxLength(100);
                entity.Property(t => t.Name).HasMaxLength(100);
            });

            // Relation Review -> Provider
            builder.Entity<Review>()
                .HasOne(r => r.Provider)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            builder.Entity<Provider>()
                .Property(p => p.HourlyRate)
                .HasPrecision(18, 2); // total 18 chiffres, 2 après la virgule
        }
    }
}