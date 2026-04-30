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
        private readonly IWebHostEnvironment _hostEnvironment;

        public AccountController(
            UserManager<User> userManager, 
            SignInManager<User> signInManager,
            RoleManager<IdentityRole<int>> roleManager,
            IWebHostEnvironment hostEnvironment)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _hostEnvironment = hostEnvironment;
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
                    UserName       = email,
                    Email          = email,
                    FullName       = fullName ?? "",
                    PhoneNumber    = phone ?? "",
                    Phone          = phone ?? "",
                    JobTitle       = "Nouveau Prestataire",
                    City           = "Non spécifiée",
                    CreatedAt      = DateTime.Now,
                    EmailConfirmed = true,
                    // ✅ FIX 15 : Activer le lockout pour permettre la suspension
                    LockoutEnabled = true
                };
            }
            else
            {
                user = new User
                {
                    UserName       = email,
                    Email          = email,
                    FullName       = fullName ?? "",
                    PhoneNumber    = phone ?? "",
                    Phone          = phone ?? "",
                    CreatedAt      = DateTime.Now,
                    EmailConfirmed = (role == "Admin"),
                    // ✅ FIX 15 : Activer le lockout pour permettre la suspension
                    LockoutEnabled = true
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

        // --- PROFIL (Affichage et Modification) ---

        [HttpGet]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login");

            return View(user); // Créez une vue Profile.cshtml pour afficher le formulaire
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string fullName, string phoneNumber, IFormFile? photo)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            user.FullName = fullName;
            user.PhoneNumber = phoneNumber;
            user.Phone = phoneNumber; // Synchronisation avec votre champ personnalisé

            // Gestion de l'upload de la photo
            if (photo != null && photo.Length > 0)
            {
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads/profiles");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + photo.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(fileStream);
                }

                // Supprimer l'ancienne photo si nécessaire ici...
                // user.ProfilePicturePath = "/uploads/profiles/" + uniqueFileName; 
                // Note: Assurez-vous que votre modèle User a une propriété pour le chemin de l'image
                ViewBag.PhotoUrl = "/uploads/profiles/" + uniqueFileName;
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
            {
                TempData["Success"] = "Profil mis à jour avec succès !";
                return RedirectToAction("Profile");
            }

            foreach (var error in result.Errors) ModelState.AddModelError("", error.Description);
            return View("Profile", user);
        }

        [HttpPost]
        [ValidateAntiForgeryToken] // Bonne pratique de sécurité
        public async Task<IActionResult> UpdateProfilePhoto(IFormFile photo)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return NotFound();

            if (photo != null && photo.Length > 0)
            {
                // 1. Créer le dossier s'il n'existe pas
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads/profiles");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                // 2. Générer un nom unique pour éviter les doublons
                string uniqueFileName = Guid.NewGuid().ToString() + "_" + photo.FileName;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                // 3. Sauvegarder le fichier physiquement
                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await photo.CopyToAsync(fileStream);
                }

                // 4. Mettre à jour le chemin dans la base de données
                // Assurez-vous que votre modèle User a une propriété 'ProfilePicturePath'
                user.ImageUrl = "/uploads/profiles/" + uniqueFileName;
                
                var result = await _userManager.UpdateAsync(user);
                if (result.Succeeded)
                {
                    return RedirectToAction("Index", "Customer"); // Ou là où se trouve votre dashboard
                }
            }

            return RedirectToAction("Index", "home");
        }
    }
}