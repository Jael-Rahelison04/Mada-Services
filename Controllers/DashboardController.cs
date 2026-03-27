using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MadaServices.Models;
using MadaServices.Data;
using System.IO;

namespace MadaServices.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public DashboardController(UserManager<User> userManager, ApplicationDbContext context, IWebHostEnvironment hostEnvironment)
        {
            _userManager = userManager;
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // --- AFFICHAGE DU DASHBOARD ---
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var provider = await _context.Users.OfType<Provider>().FirstOrDefaultAsync(p => p.Id == user.Id);
            if (provider == null) return RedirectToAction("Index", "Home");

            return View(provider);
        }

        // --- MISE À JOUR DU PROFIL ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string FullName, string JobTitle, string City, string Description, decimal HourlyRate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var provider = await _context.Users.OfType<Provider>().FirstOrDefaultAsync(p => p.Id == user.Id);

            if (provider != null)
            {
                provider.FullName = FullName;
                provider.JobTitle = JobTitle;
                provider.City = City;
                provider.Description = Description;
                provider.HourlyRate = HourlyRate;

                _context.Update(provider);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Profil mis à jour !";
            }
            return RedirectToAction(nameof(Index));
        }

        // --- UPLOAD DU PORTFOLIO ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPortfolio(IFormFile photo)
        {
            if (photo == null || photo.Length == 0)
            {
                TempData["Error"] = "Veuillez sélectionner une image.";
                return RedirectToAction(nameof(Index));
            }

            // Extensions autorisées pour les images
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".gif" };
            var extension = Path.GetExtension(photo.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Seuls les fichiers image (jpg, jpeg, png, gif) sont autorisés.";
                return RedirectToAction(nameof(Index));
            }

            // Taille maximale : 5 Mo
            if (photo.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "La taille de l'image ne doit pas dépasser 5 Mo.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var provider = await _context.Users.OfType<Provider>().FirstOrDefaultAsync(p => p.Id == user.Id);
            if (provider == null) return NotFound();

            // Générer un nom de fichier unique
            string uniqueFileName = Guid.NewGuid().ToString() + extension;
            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads/portfolio");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string filePath = Path.Combine(uploadsFolder, uniqueFileName);
            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(fileStream);
            }

            provider.PortfolioImages ??= new List<string>();
            provider.PortfolioImages.Add("/uploads/portfolio/" + uniqueFileName);

            _context.Update(provider);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Image ajoutée au portfolio !";
            return RedirectToAction(nameof(Index));
        }

        // --- UPLOAD VÉRIFICATION (CIN) ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadVerification(IFormFile document)
        {
            if (document == null || document.Length == 0)
            {
                TempData["Error"] = "Veuillez sélectionner un document.";
                return RedirectToAction(nameof(Index));
            }

            // Extensions autorisées : images + PDF
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var extension = Path.GetExtension(document.FileName).ToLowerInvariant();
            if (!allowedExtensions.Contains(extension))
            {
                TempData["Error"] = "Seuls les fichiers PDF, JPG, JPEG et PNG sont autorisés.";
                return RedirectToAction(nameof(Index));
            }

            // Taille maximale : 10 Mo
            if (document.Length > 10 * 1024 * 1024)
            {
                TempData["Error"] = "La taille du document ne doit pas dépasser 10 Mo.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var provider = await _context.Users.OfType<Provider>().FirstOrDefaultAsync(p => p.Id == user.Id);
            if (provider == null) return NotFound();

            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads/verification");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            // Nom de fichier incluant l'ID du user pour éviter les collisions
            string uniqueFileName = $"VERIF_{user.Id}_{Guid.NewGuid()}{extension}";
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await document.CopyToAsync(fileStream);
            }

            provider.HasSubmittedDocs = true;
            _context.Update(provider);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Documents envoyés pour vérification !";
            return RedirectToAction(nameof(Index));
        }
    }
}