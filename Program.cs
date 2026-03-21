using Microsoft.EntityFrameworkCore;
using MadaServices.Data;
using MadaServices.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// 1. Configuration de la connexion MySQL
// Assure-toi que "DefaultConnection" est bien défini dans appsettings.json
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 2. Configuration de l'Identité (Identity)
// Configuration de l'Identité avec types explicites
builder.Services.AddIdentity<User, IdentityRole<int>>(options => {
    options.Password.RequiredLength = 6;
    options.Password.RequireDigit = false;
    options.Password.RequireUppercase = false;
    options.Password.RequireNonAlphanumeric = false;
})
.AddEntityFrameworkStores<ApplicationDbContext>() // <--- L'erreur se trouve ici
.AddDefaultTokenProviders();
// Ajout du support pour les Contrôleurs et les Vues (MVC)
builder.Services.AddControllersWithViews();

var app = builder.Build();

// 3. Initialisation de la base de données (Roles et Users au démarrage)
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try 
    {
        // Appel de la méthode statique du fichier Data/DbInitializer.cs
        await DbInitializer.SeedRolesAndUsers(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Une erreur est survenue lors de l'initialisation des données.");
    }
}

// Configuration du pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// L'ordre est très important ici : Authentication AVANT Authorization
app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();