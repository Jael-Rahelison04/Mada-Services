using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using MadaServices.Data;
using MadaServices.Models;
using Microsoft.AspNetCore.Identity;

namespace MadaServices.Controllers
{
    [Authorize]
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _environment;
        private readonly UserManager<User> _userManager;

        public CustomerController(ApplicationDbContext context, IWebHostEnvironment environment, UserManager<User> userManager)
        {
            _context = context;
            _environment = environment;
            _userManager = userManager;
        }

        // --- DASHBOARD CLIENT ---
        public async Task<IActionResult> Index()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Challenge();

            // Récupère les avis récents du client
            var myReviews = await _context.Reviews
                .Include(r => r.Provider)
                .Where(r => r.ClientId == userIdStr)
                .OrderByDescending(r => r.DatePosted) 
                .Take(5)
                .ToListAsync();

            ViewBag.TotalReviews = await _context.Reviews.CountAsync(r => r.ClientId == userIdStr);
            
            var lastReview = await _context.Reviews
                .Include(r => r.Provider)
                .Where(r => r.ClientId == userIdStr)
                .OrderByDescending(r => r.DatePosted)
                .FirstOrDefaultAsync();
            
            ViewBag.LastProvider = lastReview?.Provider?.FullName ?? "Aucun";

            // Liste des prestataires pour le modal de réservation et d'avis
            ViewBag.ProvidersList = await _context.Providers
                .OrderBy(p => p.FullName)
                .ToListAsync();

            return View(myReviews);
        }

        // --- HISTORIQUE DES DEMANDES (Résout l'erreur 404 /Customer/MyBookings) ---
        public async Task<IActionResult> MyBookings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // On filtre par CustomerName (puisque c'est ce qui est dans ton modèle Booking)
            // Ou mieux, si tu ajoutes ClientId dans Booking, filtre par ID.
            var bookings = await _context.Bookings
                .Where(b => b.CustomerName == user.FullName) 
                .OrderByDescending(b => b.CreatedAt)
                .ToListAsync();

            return View(bookings);
        }

        // --- CRÉER UNE DEMANDE (Action pour le Modal de réservation) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateBooking(Booking booking)
        {
            var user = await _userManager.GetUserAsync(User);
            
            // On s'assure que les données obligatoires sont là
            booking.CreatedAt = DateTime.Now;
            booking.Status = "Pending";
            
            // On force le nom du client depuis l'utilisateur connecté pour la sécurité
            if (user != null) {
                booking.CustomerName = user.FullName ?? user.UserName ?? "Client";
            }

            if (ModelState.IsValid)
            {
                _context.Bookings.Add(booking);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Votre demande a été envoyée avec succès !";
            }
            else
            {
                TempData["Error"] = "Une erreur est survenue lors de l'envoi de la demande.";
            }

            return RedirectToAction(nameof(Index));
        }

        // --- PROFIL PUBLIC ---
        public async Task<IActionResult> PublicProfile(int id)
        {
            if (id <= 0) return NotFound();

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();

            return View(user);
        }

        // --- RÉGLAGES PROFIL ---
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
                string uploadsFolder = Path.Combine(_environment.WebRootPath, "uploads", "profiles");
                if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(model.ProfilePicture.FileName);
                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfilePicture.CopyToAsync(fileStream);
                }
                user.ImageUrl = "/uploads/profiles/" + uniqueFileName;
            }

            var result = await _userManager.UpdateAsync(user);
            if (result.Succeeded) TempData["Success"] = "Profil mis à jour !";
            else TempData["Error"] = "Erreur de mise à jour.";

            return RedirectToAction(nameof(Settings));
        }

        // --- GESTION DES AVIS ---
        public async Task<IActionResult> MyReviews()
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Challenge();
            
            var myReviews = await _context.Reviews
                .Include(r => r.Provider)
                .Where(r => r.ClientId == userIdStr)
                .OrderByDescending(r => r.DatePosted)
                .ToListAsync();

            return View(myReviews);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateReview(int providerId, int rating, string? comment)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrEmpty(userIdStr)) return Challenge();

            var user = await _userManager.FindByIdAsync(userIdStr);
            
            bool alreadyReviewed = await _context.Reviews
                .AnyAsync(r => r.ClientId == userIdStr && r.ProviderId == providerId);

            if (alreadyReviewed)
            {
                TempData["Error"] = "Vous avez déjà déposé un avis sur ce prestataire.";
                return RedirectToAction(nameof(Index));
            }

            var review = new Review
            {
                ProviderId = providerId,
                ClientId = userIdStr,
                CustomerName = user?.FullName ?? "Client",
                Rating = rating,
                Comment = comment ?? "", 
                DatePosted = DateTime.Now 
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            
            TempData["Success"] = "Avis publié !";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var review = await _context.Reviews
                .FirstOrDefaultAsync(r => r.Id == id && r.ClientId == userIdStr);

            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Avis supprimé.";
            }
            return RedirectToAction(nameof(MyReviews));
        }
    }
}