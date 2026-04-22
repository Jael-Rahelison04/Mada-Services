using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MadaServices.Models;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace MadaServices.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;
        private readonly RoleManager<IdentityRole<int>> _roleManager;

        public AccountController(
            UserManager<User> userManager, 
            SignInManager<User> signInManager,
            RoleManager<IdentityRole<int>> roleManager)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
        }

        // --- CONNEXION ---

        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(string email, string password, bool rememberMe, string? returnUrl = null)
        {
            if (ModelState.IsValid)
            {
                // Identity utilise le UserName. On passe l'email ici car UserName = Email dans notre config
                var result = await _signInManager.PasswordSignInAsync(email, password, rememberMe, lockoutOnFailure: false);
                
                if (result.Succeeded)
                {
                    // Redirection intelligente : Si Admin -> Dashboard, sinon Home ou ReturnUrl
                    var user = await _userManager.FindByEmailAsync(email);
                    if (user != null && await _userManager.IsInRoleAsync(user, "Admin"))
                    {
                        return RedirectToAction("Index", "Admin");
                    }

                    if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
                    {
                        return Redirect(returnUrl);
                    }
                    return RedirectToAction("Index", "Home");
                }
                
                ModelState.AddModelError(string.Empty, "Email ou mot de passe incorrect.");
            }
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        // --- INSCRIPTION ---

        [HttpGet]
        public async Task<IActionResult> Register()
        {
            // Vérifie si un Admin existe déjà pour masquer l'option dans la vue
            var admins = await _userManager.GetUsersInRoleAsync("Admin");
            ViewBag.HasAdmin = admins.Any();
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(string email, string password, string fullName, string phone, string role)
        {
            // 1. Sécurité : Vérifier si l'inscription Admin est autorisée
            if (role == "Admin")
            {
                var admins = await _userManager.GetUsersInRoleAsync("Admin");
                if (admins.Any())
                {
                    ModelState.AddModelError(string.Empty, "Un compte administrateur existe déjà sur ce système.");
                    ViewBag.HasAdmin = true;
                    return View();
                }
            }

            // 2. Initialisation de l'utilisateur (Gestion de l'héritage Provider/User)
            User user;
            if (role == "Provider")
            {
                user = new Provider 
                { 
                    UserName = email, 
                    Email = email, 
                    FullName = fullName ?? "", 
                    PhoneNumber = phone ?? "", // Champ standard Identity
                    Phone = phone ?? "",       // Votre champ personnalisé (Évite l'erreur MySQL NULL)
                    JobTitle = "Nouveau Prestataire", 
                    City = "Non spécifiée",
                    CreatedAt = DateTime.Now,
                    EmailConfirmed = true // Pour simplifier vos tests
                };
            }
            else
            {
                user = new User 
                { 
                    UserName = email, 
                    Email = email, 
                    FullName = fullName ?? "", 
                    PhoneNumber = phone ?? "", 
                    Phone = phone ?? "",
                    CreatedAt = DateTime.Now,
                    EmailConfirmed = (role == "Admin") // L'admin est auto-confirmé
                };
            }

            // 3. Création dans la base de données
            var result = await _userManager.CreateAsync(user, password);

            if (result.Succeeded)
            {
                // Vérifier et créer le rôle s'il n'existe pas encore
                if (!await _roleManager.RoleExistsAsync(role))
                {
                    await _roleManager.CreateAsync(new IdentityRole<int>(role));
                }

                // Assigner le rôle à l'utilisateur
                await _userManager.AddToRoleAsync(user, role);

                // Connexion automatique après inscription
                await _signInManager.SignInAsync(user, isPersistent: false);
                
                // Redirection selon le rôle
                if (role == "Admin") return RedirectToAction("Index", "Admin");
                if (role == "Provider") return RedirectToAction("Dashboard", "Provider");

                return RedirectToAction("Index", "Home");
            }

            // Gestion des erreurs Identity (ex: mot de passe trop simple)
            foreach (var error in result.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }

            // Re-calculer l'état de l'admin pour la vue en cas d'erreur
            var currentAdmins = await _userManager.GetUsersInRoleAsync("Admin");
            ViewBag.HasAdmin = currentAdmins.Any();
            
            return View();
        }

        // --- DÉCONNEXION ---

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction("Index", "Home");
        }

        // --- ACCÈS REFUSÉ ---
        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}