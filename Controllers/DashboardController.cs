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

            var provider = await _context.Providers
                .Include(p => p.PortfolioItems)
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Id == user.Id);

            if (provider == null) return RedirectToAction("Index", "Home");

            return View(provider);
        }

        // --- PAGE DES PARAMÈTRES (SETTINGS) ---
        public async Task<IActionResult> Settings()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // On prépare le ViewModel pour la vue Settings.cshtml
            var model = new SettingsViewModel
            {
                Id = user.Id,
                FullName = user.FullName ?? string.Empty, // Correction CS8601
                Email = user.Email ?? string.Empty,    // Correction CS8601
                PhoneNumber = user.Phone,
                Address = user.Address,
                ImageUrl = user.ImageUrl
            };

            return View(model);
        }

        // --- MISE À JOUR DU PROFIL ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(SettingsViewModel model)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Mise à jour des infos de base de l'IdentityUser
            user.FullName = model.FullName ?? string.Empty;
            user.Phone = model.PhoneNumber ?? string.Empty;
            user.Address = model.Address;

            // Gestion de l'upload de l'image de profil
            if (model.ProfilePicture != null && model.ProfilePicture.Length > 0)
            {
                string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "profiles");
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
            if (result.Succeeded)
            {
                TempData["Success"] = "Profil mis à jour avec succès !";
            }

            return RedirectToAction(nameof(Settings));
        }

        // --- UPLOAD DU PORTFOLIO ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPortfolio(IFormFile? portfolioFile, string? description)
        {
            if (portfolioFile == null || portfolioFile.Length == 0) return RedirectToAction(nameof(Index));

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.Id == user.Id);
            if (provider == null) return NotFound();

            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads", "portfolio");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + Path.GetFileName(portfolioFile.FileName);
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await portfolioFile.CopyToAsync(fileStream);
            }

            var newItem = new PortfolioItem
            {
                ImageUrl = "/uploads/portfolio/" + uniqueFileName,
                Description = description ?? "Sans description",
                ProviderId = provider.Id
            };

            _context.PortfolioItems.Add(newItem);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Réalisation ajoutée !";
            return RedirectToAction(nameof(Index));
        }

        // --- SUPPRESSION PORTFOLIO ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePortfolioItem(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var item = await _context.PortfolioItems.FirstOrDefaultAsync(pi => pi.Id == id && pi.ProviderId == user.Id);
            
            if (item != null && !string.IsNullOrEmpty(item.ImageUrl))
            {
                var fullPath = Path.Combine(_hostEnvironment.WebRootPath, item.ImageUrl.TrimStart('/'));
                if (System.IO.File.Exists(fullPath)) System.IO.File.Delete(fullPath);

                _context.PortfolioItems.Remove(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Image supprimée.";
            }

            return RedirectToAction(nameof(Index));
        }
    }
}