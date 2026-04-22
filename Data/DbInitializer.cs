using Microsoft.AspNetCore.Identity;
using MadaServices.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MadaServices.Data
{
    public static class DbInitializer
    {
        public static async Task SeedRolesAndUsers(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

            // 1. Création des rôles indispensables
            string[] roles = { "Admin", "Provider", "Client" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    await roleManager.CreateAsync(new IdentityRole<int> { Name = role });
                }
            }

            // 2. Création forcée de l'Administrateur par défaut
            string adminEmail = "admin@madaservices.mg";
            var adminUser = await userManager.FindByEmailAsync(adminEmail);

            if (adminUser == null)
            {
                var newAdmin = new User
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    FullName = "Administrateur Système",
                    EmailConfirmed = true,
                    CreatedAt = DateTime.Now
                };

                // Création avec le mot de passe AdminMada2026!
                var result = await userManager.CreateAsync(newAdmin, "AdminMada2026!");

                if (result.Succeeded)
                {
                    // Attribution du rôle Admin
                    await userManager.AddToRoleAsync(newAdmin, "Admin");
                }
            }
        }
    }
}