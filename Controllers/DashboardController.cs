// Controllers/DashboardController.cs (VERSION CORRIGÉE)
// ✅ CORRECTION P8 : L'action UploadPortfolio est SUPPRIMÉE de ce controller.
// Elle existe uniquement dans ProviderController.cs désormais.
// Les formulaires dans les vues doivent pointer vers Provider/UploadPortfolio.

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MadaServices.Models;
using MadaServices.Data;

namespace MadaServices.Controllers
{
    [Authorize]
    public class DashboardController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public DashboardController(
            UserManager<User> userManager,
            ApplicationDbContext context,
            IWebHostEnvironment hostEnvironment)
        {
            _userManager = userManager;
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // Affichage du dashboard → redirige vers ProviderController.Dashboard
        // pour éviter la duplication de logique
        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            // Redirige proprement vers le vrai dashboard du prestataire
            return RedirectToAction("Dashboard", "Provider");
        }

        // ─────────────────────────────────────────────
        // PAGE DES PARAMÈTRES
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
                // ✅ CORRECTION P11 : Utiliser ImageUrl (hérité de User)
                ImageUrl = user.ImageUrl
            };

            return View(model);
        }

        // ─────────────────────────────────────────────
        // MISE À JOUR DU PROFIL (SETTINGS)
        // ─────────────────────────────────────────────
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
                string uploadsFolder = Path.Combine(
                    _hostEnvironment.WebRootPath, "uploads", "profiles");

                if (!Directory.Exists(uploadsFolder))
                    Directory.CreateDirectory(uploadsFolder);

                string uniqueFileName = Guid.NewGuid() + "_"
                    + Path.GetFileName(model.ProfilePicture.FileName);

                string filePath = Path.Combine(uploadsFolder, uniqueFileName);

                using (var fileStream = new FileStream(filePath, FileMode.Create))
                {
                    await model.ProfilePicture.CopyToAsync(fileStream);
                }

                // ✅ CORRECTION P11 : Écrire dans ImageUrl (pas ProfileImageUrl)
                user.ImageUrl = "/uploads/profiles/" + uniqueFileName;
            }

            var result = await _userManager.UpdateAsync(user);

            TempData[result.Succeeded ? "Success" : "Error"] = result.Succeeded
                ? "Profil mis à jour avec succès !"
                : "Erreur lors de la mise à jour.";

            return RedirectToAction(nameof(Settings));
        }

        // ─────────────────────────────────────────────
        // SUPPRESSION PORTFOLIO (délégation à Provider)
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePortfolioItem(int id)
        {
            // ✅ CORRECTION P8 : On délègue à ProviderController
            // pour éviter la duplication de code
            return RedirectToAction("DeletePortfolioItem", "Provider", new { id });
        }
    }
}