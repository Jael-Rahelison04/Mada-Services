using Microsoft.AspNetCore.Identity;
using MadaServices.Models;
using Microsoft.Extensions.DependencyInjection;

namespace MadaServices.Data // <--- Vérifie bien cette ligne
{
    public static class DbInitializer // <--- Doit être static
    {
        public static async Task SeedRolesAndUsers(IServiceProvider serviceProvider)
        {
            var roleManager = serviceProvider.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var userManager = serviceProvider.GetRequiredService<UserManager<User>>();

            // Ton code de création de rôles et users ici...
            string[] roles = { "Admin", "Provider", "Client" };
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                    await roleManager.CreateAsync(new IdentityRole<int>(role));
            }
        }
    }
}