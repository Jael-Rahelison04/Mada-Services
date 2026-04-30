using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MadaServices.Data;
using MadaServices.Models;

var builder = WebApplication.CreateBuilder(args);

// 1. Connexion MySQL (Assurez-vous que la chaîne de caractères est dans appsettings.json)
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection") 
    ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");

builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseMySql(connectionString, ServerVersion.AutoDetect(connectionString)));

// 2. Configuration de l'Identité
// Dans Program.cs, remplacer la config Identity par celle-ci :
builder.Services.AddIdentity<User, IdentityRole<int>>(options =>
{
    options.Password.RequiredLength         = 6;
    options.Password.RequireDigit           = false;
    options.Password.RequireUppercase       = false;
    options.Password.RequireLowercase       = false;
    options.Password.RequireNonAlphanumeric = false;

    options.User.RequireUniqueEmail = true;

    // ✅ FIX 15 : Activer le Lockout par défaut pour TOUS les nouveaux comptes
    // Sans ça, Identity ignorait SetLockoutEndDateAsync
    options.Lockout.AllowedForNewUsers      = true;
    options.Lockout.DefaultLockoutTimeSpan  = TimeSpan.FromDays(36500); // ~100 ans
    options.Lockout.MaxFailedAccessAttempts = 5;
})
.AddEntityFrameworkStores<ApplicationDbContext>()
.AddDefaultTokenProviders();

// 3. Configuration des Cookies (Gestion des accès et redirections)
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied"; 
    options.Cookie.HttpOnly = true;
    options.Cookie.Name = "MadaServices.Auth";
    options.ExpireTimeSpan = TimeSpan.FromDays(30);
    options.SlidingExpiration = true;

    options.Events.OnValidatePrincipal = SecurityStampValidator.ValidatePrincipalAsync;
});

builder.Services.Configure<SecurityStampValidatorOptions>(options =>
{
    options.ValidationInterval = TimeSpan.FromMinutes(30);
});

builder.Services.AddControllersWithViews();

builder.Services.AddHostedService<MadaServices.Services.AccountCleanupService>();

var app = builder.Build();

// 4. Initialisation des données (Seed de l'Admin et des Rôles)
// Program.cs — bloc seed après builder.Build()
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    try
    {
        // ✅ Un seul point d'entrée pour le seed
        await DbInitializer.SeedRolesAndUsers(services);
    }
    catch (Exception ex)
    {
        var logger = services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Erreur lors de l'initialisation de la base de données.");
    }
}


// 5. Pipeline HTTP
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();

app.UseRouting();

// L'ordre est vital : Authentification AVANT Autorisation
app.UseAuthentication(); 
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}");

app.Run();

// --- MÉTHODE DE SEED INTERNE (Plus fiable que DbInitializer si absent) ---
async Task SeedAdminData(UserManager<User> userManager, RoleManager<IdentityRole<int>> roleManager)
{
    string[] roles = { "Admin", "Provider", "Client" };
    foreach (var role in roles)
    {
        if (!await roleManager.RoleExistsAsync(role))
        {
            await roleManager.CreateAsync(new IdentityRole<int>(role));
        }
    }

    string adminEmail = "admin@madaservices.mg";
    if (await userManager.FindByEmailAsync(adminEmail) == null)
    {
        var admin = new User
        {
            UserName = adminEmail,
            Email = adminEmail,
            FullName = "Administrateur MadaServices",
            EmailConfirmed = true,
            CreatedAt = DateTime.Now
        };

        var result = await userManager.CreateAsync(admin, "AdminMada2026!");
        if (result.Succeeded)
        {
            await userManager.AddToRoleAsync(admin, "Admin");
        }
    }
}