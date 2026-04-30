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

        public DbSet<Category> Categories { get; set; }
        public DbSet<City> Cities { get; set; } 
        public DbSet<Provider> Providers { get; set; } = default!;
        public DbSet<Review> Reviews { get; set; } = default!;
        public DbSet<PortfolioItem> PortfolioItems { get; set; } = default!;
        
        // --- NOUVEAUX DBSET POUR LE DYNAMISME ---
        public DbSet<Booking> Bookings { get; set; } = default!;
        public DbSet<Availability> Availabilities { get; set; } = default!;
        public DbSet<ClientDocument> ClientDocuments { get; set; }

        // Data/ApplicationDbContext.cs (SECTION OnModelCreating mise à jour)
        protected override void OnModelCreating(ModelBuilder builder)
        {
            base.OnModelCreating(builder);

            builder.Entity<IdentityUserLogin<int>>(entity =>
            {
                entity.Property(e => e.LoginProvider).HasMaxLength(100);
                entity.Property(e => e.ProviderKey).HasMaxLength(100);
            });

            // RELATION : Provider → Category
            builder.Entity<Provider>()
                .HasOne(p => p.Category)
                .WithMany(c => c.Providers)
                .HasForeignKey(p => p.CategoryId)
                .OnDelete(DeleteBehavior.SetNull);

            // RELATION : Review → Provider (ClientId est maintenant int)
            builder.Entity<Review>()
                .HasOne(r => r.Provider)
                .WithMany(p => p.Reviews)
                .HasForeignKey(r => r.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            // RELATION : PortfolioItem → Provider
            builder.Entity<PortfolioItem>()
                .HasOne(pi => pi.Provider)
                .WithMany(p => p.PortfolioItems)
                .HasForeignKey(pi => pi.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            // RELATION : Booking → Provider
            builder.Entity<Booking>()
                .HasOne<Provider>()
                .WithMany(p => p.Bookings)
                .HasForeignKey(b => b.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);

            // RELATION : Availability → Provider
            builder.Entity<Availability>()
                .HasOne<Provider>()
                .WithMany(p => p.Availabilities)
                .HasForeignKey(a => a.ProviderId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}