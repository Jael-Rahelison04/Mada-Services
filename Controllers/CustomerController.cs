// Controllers/CustomerController.cs (VERSION ENTIÈREMENT CORRIGÉE)
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MadaServices.Data;
using MadaServices.Models;
using Microsoft.AspNetCore.Identity;
using MadaServices.Models.ViewModels;

namespace MadaServices.Controllers
{
    // ✅ CORRECTION C3 : Ajout de Roles = "Client" pour empêcher
    // les prestataires et admins d'accéder à cet espace
    [Authorize(Roles = "Client")]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<User> _userManager;
        private readonly SignInManager<User> _signInManager;

        public CustomerController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            UserManager<User> userManager,
            SignInManager<User> signInManager)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
            _signInManager = signInManager;
        }

        // ─────────────────────────────────────────────
        // DASHBOARD CLIENT
        // ─────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // ✅ FORCE L'AFFICHAGE DU NOM ET DE LA PHOTO
            // On envoie le FullName au ViewBag. S'il est vide, on prend la partie avant le '@' de l'email
            ViewBag.FullName = !string.IsNullOrEmpty(user.FullName) 
                ? user.FullName 
                : user.Email?.Split('@')[0];

            // On envoie le chemin de la photo. Si null, on utilise l'avatar par défaut
            ViewBag.PhotoUrl = !string.IsNullOrEmpty(user.ImageUrl) 
                ? user.ImageUrl 
                : "/images/default-avatar.png";

            // ✅ CORRECTION C1 : Utilisation de user.Id (int) au lieu de userIdStr (string)
            var myReviews = await _context.Reviews
                .Include(r => r.Provider)
                .Where(r => r.ClientId == user.Id)   // ← int == int (correct)
                .OrderByDescending(r => r.DatePosted)
                .Take(5)
                .ToListAsync();

            ViewBag.TotalReviews = await _context.Reviews
                .CountAsync(r => r.ClientId == user.Id);  // ← int

            var lastReview = await _context.Reviews
                .Include(r => r.Provider)
                .Where(r => r.ClientId == user.Id)
                .OrderByDescending(r => r.DatePosted)
                .FirstOrDefaultAsync();

            ViewBag.LastProvider = lastReview?.Provider?.FullName ?? "Aucun";

            ViewBag.ProvidersList = await _context.Providers
                .Where(p => !p.IsPaused)
                .OrderBy(p => p.FullName)
                .ToListAsync();

            return View(myReviews);
        }

        // ─────────────────────────────────────────────
        // HISTORIQUE DES DEMANDES
        // ─────────────────────────────────────────────
        public async Task<IActionResult> MyBookings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // ✅ CORRECTION C2 : Recherche par ClientId (int) au lieu du nom
            // Plus de collision si deux clients ont le même prénom/nom
            var bookings = await _context.Bookings
                .Where(b => b.ClientId == user.Id)       // ← int == int (correct)
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // ─────────────────────────────────────────────
        // CRÉER UNE RÉSERVATION
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBooking(Booking booking)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            booking.CreatedAt = DateTime.Now;
            booking.Status = "Pending";
            booking.CustomerName = user.FullName ?? user.UserName ?? "Client";

            // ✅ CORRECTION C2 : Stocker le ClientId (int) pour identifier
            // le client de façon unique et fiable
            booking.ClientId = user.Id;

            // Validation basique
            if (booking.ProviderId <= 0 || string.IsNullOrWhiteSpace(booking.ServiceName))
            {
                TempData["Error"] = "Informations de réservation incomplètes.";
                return RedirectToAction(nameof(Index));
            }

            // Vérifier que le prestataire existe et n'est pas en pause
            var providerExists = await _context.Providers
                .AnyAsync(p => p.Id == booking.ProviderId && !p.IsPaused);

            if (!providerExists)
            {
                TempData["Error"] = "Ce prestataire n'est pas disponible actuellement.";
                return RedirectToAction(nameof(Index));
            }

            _context.Bookings.Add(booking);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Votre demande a été envoyée avec succès !";
            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────────────────────────
        // DÉPOSER UN AVIS
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        // ✅ APRÈS — reçu en string puis parsé en InvariantCulture
        public async Task<IActionResult> CreateReview(int providerId, string? rating, string? comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // ── Parser manuellement avec InvariantCulture ──────────────
            decimal ratingDecimal = 0;
            if (!decimal.TryParse(
                    rating?.Replace(",", "."),          // sécurité si virgule
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out ratingDecimal))
            {
                TempData["Error"] = "Note invalide.";
                return RedirectToAction(nameof(Index));
            }

            // ── Validation plage ───────────────────────────────────────
            if (ratingDecimal < 0.5m || ratingDecimal > 5.0m)
            {
                TempData["Error"] = "La note doit être entre 0.5 et 5.";
                return RedirectToAction(nameof(Index));
            }

            // ── Arrondir au 0.5 le plus proche ────────────────────────
            ratingDecimal = Math.Round(ratingDecimal * 2) / 2;

            if (user.Id == providerId)
            {
                TempData["Error"] = "Vous ne pouvez pas noter votre propre profil.";
                return RedirectToAction(nameof(Index));
            }

            bool alreadyReviewed = await _context.Reviews
                .AnyAsync(r => r.ClientId == user.Id && r.ProviderId == providerId);

            if (alreadyReviewed)
            {
                TempData["Error"] = "Vous avez déjà déposé un avis sur ce prestataire.";
                return RedirectToAction(nameof(Index));
            }

            var review = new Review
            {
                ProviderId   = providerId,
                ClientId     = user.Id,
                CustomerName = user.FullName ?? user.UserName ?? "Client",
                Rating       = ratingDecimal,
                Comment      = comment ?? "",
                DatePosted   = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Votre avis a été publié avec succès !";
            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────────────────────────
        // SUPPRIMER UN AVIS
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // ✅ CORRECTION C1 : Vérification avec int == int
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == id && r.ClientId == user.Id);

            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Avis supprimé avec succès.";
            }
            else
            {
                TempData["Error"] = "Avis introuvable ou non autorisé.";
            }

            return RedirectToAction(nameof(MyReviews));
        }

        // ─────────────────────────────────────────────
        // MES AVIS
        // ─────────────────────────────────────────────
        public async Task<IActionResult> MyReviews()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // ✅ CORRECTION C1 : Filtrage par int
            var myReviews = await _context.Reviews
                .Include(r => r.Provider)
                .Where(r => r.ClientId == user.Id)
                .OrderByDescending(r => r.DatePosted)
                .ToListAsync();

            return View(myReviews);
        }

        // ─────────────────────────────────────────────
        // PROFIL PUBLIC D'UN UTILISATEUR
        // ─────────────────────────────────────────────
        public async Task<IActionResult> PublicProfile(int id)
        {
            if (id <= 0) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            return View(user);
        }

        // ─────────────────────────────────────────────
        // SETTINGS — Affichage (remplacement de l'action existante)
        // ─────────────────────────────────────────────
        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
 
            var docs = await _context.ClientDocuments
                .Where(d => d.UserId == user.Id)
                .ToListAsync();
 
            var model = new SettingsViewModel
            {
                Id          = user.Id,
                FullName    = user.FullName ?? string.Empty,
                Email       = user.Email ?? string.Empty,
                PhoneNumber = user.Phone,
                Address     = user.Address,
                JobTitle    = user.JobTitle,
                IsVerified  = user.IsVerified,
                ImageUrl    = user.ImageUrl ?? "/images/default-avatar.png",
                Documents   = docs.Select(d => new ClientDocumentDto
                {
                    Id           = d.Id,
                    DocumentType = d.DocumentType,
                    FilePath     = d.FilePath,
                    Status       = d.Status,
                    UploadedAt   = d.UploadedAt
                }).ToList()
            };
 
            return View(model);
        }

        // ─────────────────────────────────────────────
        // SETTINGS — UpdateProfile (remplacement)
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(SettingsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
 
            user.FullName = model.FullName ?? string.Empty;
            user.Phone    = model.PhoneNumber ?? string.Empty;
            user.Address  = model.Address;
            user.JobTitle = model.JobTitle;
 
            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                var allowed = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(model.ProfilePicture.FileName).ToLowerInvariant();
                if (!allowed.Contains(ext))
                {
                    TempData["Error"] = "Format d'image non supporté.";
                    TempData["ActiveTab"] = "profile";
                    return RedirectToAction(nameof(Settings));
                }
 
                if (model.ProfilePicture.Length > 5 * 1024 * 1024)
                {
                    TempData["Error"] = "Image trop volumineuse (max 5 Mo).";
                    TempData["ActiveTab"] = "profile";
                    return RedirectToAction(nameof(Settings));
                }
 
                string folder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
 
                string fileName = Guid.NewGuid().ToString() + ext;
                string filePath = Path.Combine(folder, fileName);
                using (var stream = new FileStream(filePath, FileMode.Create))
                    await model.ProfilePicture.CopyToAsync(stream);
 
                user.ImageUrl = "/uploads/profiles/" + fileName;
            }
 
            var result = await _userManager.UpdateAsync(user);
            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Profil mis à jour avec succès !"
                : "Erreur lors de la mise à jour.";
 
            TempData["ActiveTab"] = "profile";
            return RedirectToAction(nameof(Settings));
        }

                // ─────────────────────────────────────────────
        // SETTINGS — UpdateEmail (nouvelle action)
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateEmail(string NewEmail, string PasswordConfirm)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
 
            // Vérifier le mot de passe courant
            var pwdOk = await _userManager.CheckPasswordAsync(user, PasswordConfirm);
            if (!pwdOk)
            {
                TempData["Error"] = "Mot de passe incorrect.";
                TempData["ActiveTab"] = "security";
                return RedirectToAction(nameof(Settings));
            }
 
            // Recharger avant SetEmailAsync pour éviter le conflit de ConcurrencyStamp
            user = await _userManager.FindByIdAsync(user.Id.ToString());
            var emailResult = await _userManager.SetEmailAsync(user!, NewEmail);
            if (!emailResult.Succeeded)
            {
                TempData["Error"] = emailResult.Errors.FirstOrDefault()?.Description ?? "Erreur lors du changement d'e-mail.";
                TempData["ActiveTab"] = "security";
                return RedirectToAction(nameof(Settings));
            }
 
            user = await _userManager.FindByIdAsync(user!.Id.ToString());
            await _userManager.SetUserNameAsync(user!, NewEmail);
 
            TempData["Success"] = "E-mail mis à jour. Reconnectez-vous si nécessaire.";
            TempData["ActiveTab"] = "security";
            return RedirectToAction(nameof(Settings));
        }
 
        // ─────────────────────────────────────────────
        // SETTINGS — UpdatePassword (nouvelle action)
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdatePassword(string CurrentPassword, string NewPassword, string ConfirmPassword)
        {
            if (NewPassword != ConfirmPassword)
            {
                TempData["Error"] = "Les mots de passe ne correspondent pas.";
                TempData["ActiveTab"] = "security";
                return RedirectToAction(nameof(Settings));
            }
 
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
 
            var result = await _userManager.ChangePasswordAsync(user, CurrentPassword, NewPassword);
            if (result.Succeeded)
            {
                TempData["Success"] = "Mot de passe changé avec succès.";
            }
            else
            {
                TempData["Error"] = result.Errors.FirstOrDefault()?.Description ?? "Erreur lors du changement de mot de passe.";
            }
 
            TempData["ActiveTab"] = "security";
            return RedirectToAction(nameof(Settings));
        }
 
        // ─────────────────────────────────────────────
        // SETTINGS — UploadDocument (nouvelle action)
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadDocument(string documentType, IFormFile file)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");
 
            var validTypes = new[] { "CIN", "CertResidence", "CertTravail", "CV" };
            if (!validTypes.Contains(documentType))
            {
                TempData["Error"] = "Type de document invalide.";
                TempData["ActiveTab"] = "documents";
                return RedirectToAction(nameof(Settings));
            }
 
            if (file == null || file.Length == 0)
            {
                TempData["Error"] = "Aucun fichier sélectionné.";
                TempData["ActiveTab"] = "documents";
                return RedirectToAction(nameof(Settings));
            }
 
            var allowed = new[] { ".pdf", ".jpg", ".jpeg", ".png" };
            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (!allowed.Contains(ext))
            {
                TempData["Error"] = "Format non supporté. Utilisez PDF, JPG ou PNG.";
                TempData["ActiveTab"] = "documents";
                return RedirectToAction(nameof(Settings));
            }
 
            if (file.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "Fichier trop volumineux (max 5 Mo).";
                TempData["ActiveTab"] = "documents";
                return RedirectToAction(nameof(Settings));
            }
 
            // Sauvegarder le fichier
            string folder = Path.Combine(_environment.WebRootPath, "uploads", "documents", user.Id.ToString());
            if (!Directory.Exists(folder)) Directory.CreateDirectory(folder);
 
            string fileName = documentType + "_" + Guid.NewGuid().ToString() + ext;
            string filePath = Path.Combine(folder, fileName);
            using (var stream = new FileStream(filePath, FileMode.Create))
                await file.CopyToAsync(stream);
 
            // Mettre à jour ou créer l'entrée en base
            var existing = await _context.ClientDocuments
                .FirstOrDefaultAsync(d => d.UserId == user.Id && d.DocumentType == documentType);
 
            if (existing != null)
            {
                // Remplacer l'ancien fichier physiquement
                var oldPath = Path.Combine(_environment.WebRootPath, existing.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));
                if (System.IO.File.Exists(oldPath)) System.IO.File.Delete(oldPath);
 
                existing.FilePath   = "/uploads/documents/" + user.Id + "/" + fileName;
                existing.UploadedAt = DateTime.UtcNow;
                existing.Status     = "Pending"; // Repasse en attente après re-soumission
            }
            else
            {
                _context.ClientDocuments.Add(new ClientDocument
                {
                    UserId       = user.Id,
                    DocumentType = documentType,
                    FilePath     = "/uploads/documents/" + user.Id + "/" + fileName,
                    UploadedAt   = DateTime.UtcNow,
                    Status       = "Pending"
                });
            }
 
            await _context.SaveChangesAsync();
 
            TempData["Success"] = "Document soumis avec succès. En attente de vérification.";
            TempData["ActiveTab"] = "documents";
            return RedirectToAction(nameof(Settings));
        }
 
        // ─────────────────────────────────────────────
        // DeleteAccount (nouvelle action)
        // ─────────────────────────────────────────────
        [HttpGet]
        public async Task<IActionResult> DeleteAccount()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // ✅ Déconnexion avant suppression
            await _signInManager.SignOutAsync();
            
            // Suppression du compte
            var result = await _userManager.DeleteAsync(user);
            
            if (!result.Succeeded)
            {
                // Gérer les erreurs éventuelles
                TempData["Error"] = "Erreur lors de la suppression du compte.";
                return RedirectToAction(nameof(Settings));
            }

            TempData["Success"] = "Votre compte a été supprimé avec succès.";
            return RedirectToAction("Index", "Home");
        }
    }
}