// Controllers/CustomerController.cs (VERSION ENTIÈREMENT CORRIGÉE)
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MadaServices.Data;
using MadaServices.Models;
using Microsoft.AspNetCore.Identity;

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

        public CustomerController(
            ApplicationDbContext context,
            IWebHostEnvironment environment,
            UserManager<User> userManager)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
        }

        // ─────────────────────────────────────────────
        // DASHBOARD CLIENT
        // ─────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

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
        public async Task<IActionResult> CreateReview(int providerId, decimal rating, string? comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // ✅ Validation : 0.5 à 5.0 par pas de 0.5
            if (rating < 0.5m || rating > 5.0m)
            {
                TempData["Error"] = "La note doit être entre 0.5 et 5.";
                return RedirectToAction(nameof(Index));
            }

            // Arrondir au 0.5 le plus proche pour éviter 3.3 ou 4.7
            rating = Math.Round(rating * 2) / 2;

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
                Rating       = rating,
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
        // PARAMÈTRES DU PROFIL
        // ─────────────────────────────────────────────
        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var model = new SettingsViewModel
            {
                Id = user.Id,
                FullName = user.FullName ?? string.Empty,
                Email = user.Email ?? string.Empty,
                PhoneNumber = user.Phone,
                Address = user.Address,
                ImageUrl = user.ImageUrl ?? "/images/default-avatar.png"
            };

            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(SettingsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            user.FullName = model.FullName ?? string.Empty;
            user.Phone = model.PhoneNumber ?? string.Empty;
            user.Address = model.Address;

            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                // Vérification du type de fichier
                var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
                var ext = Path.GetExtension(model.ProfilePicture.FileName).ToLower();
                if (!allowedExtensions.Contains(ext))
                {
                    TempData["Error"] = "Format d'image non supporté (jpg, png, webp uniquement).";
                    return RedirectToAction(nameof(Settings));
                }

                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + ext;
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfilePicture.CopyToAsync(fileStream);
                }
                user.ImageUrl = "/uploads/profiles/" + uniqueFileName;
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded)
                TempData["Success"] = "Profil mis à jour avec succès !";
            else
                TempData["Error"] = "Erreur lors de la mise à jour.";

            return RedirectToAction(nameof(Settings));
        }
    }
}