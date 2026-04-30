// Data/DbInitializer.cs
using Microsoft.AspNetCore.Identity;
using MadaServices.Models;

namespace MadaServices.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndUsers(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

            // ── 1. Création des rôles ────────────────────────────────
            string[] roles = { "Admin", "Provider", "Client" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole<int>(role));
            }

            // ── 2. Admin : vérifier par RÔLE, pas par email ──────────
            // Ainsi, même si l'admin change son email, aucun doublon ne sera créé
            var existingAdmins = await userManager.GetUsersInRoleAsync("Admin");
            if (existingAdmins.Any())
                return; // Un admin existe déjà → rien à faire

            // Aucun admin en base → créer le compte par défaut
            string adminEmail = "admin@madaservices.mg";
            var admin = new User
            {
                UserName       = adminEmail,
                Email          = adminEmail,
                FullName       = "Administrateur MadaServices",
                EmailConfirmed = true,
                CreatedAt      = DateTime.Now
            };

            var result = await userManager.CreateAsync(admin, "AdminMada2026!");
            if (result.Succeeded)
                await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}