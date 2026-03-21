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

            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.Id == user.Id);
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

            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.Id == user.Id);

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
        public async Task<IActionResult> UploadPortfolio(IFormFile photo)
        {
            if (photo == null || photo.Length == 0) return RedirectToAction(nameof(Index));

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.Id == user.Id);
            // CORRECTION CS8602 : Vérifier si provider est null
            if (provider == null) return NotFound();

            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads/portfolio");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = Guid.NewGuid().ToString() + "_" + photo.FileName;
            string filePath = Path.Combine(uploadsFolder, uniqueFileName);

            using (var fileStream = new FileStream(filePath, FileMode.Create))
            {
                await photo.CopyToAsync(fileStream);
            }

            // CORRECTION CS8602 : Initialisation si la liste est nulle
            if (provider.PortfolioImages == null) 
            {
                provider.PortfolioImages = new List<string>();
            }
            
            provider.PortfolioImages.Add("/uploads/portfolio/" + uniqueFileName);

            _context.Update(provider);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Image ajoutée au portfolio !";
            return RedirectToAction(nameof(Index));
        }

        // --- UPLOAD VÉRIFICATION (CIN) ---
        [HttpPost]
        public async Task<IActionResult> UploadVerification(IFormFile document)
        {
            if (document == null) return RedirectToAction(nameof(Index));

            var user = await _userManager.GetUserAsync(User);
            if (user == null) return RedirectToAction("Login", "Account");

            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.Id == user.Id);
            if (provider == null) return NotFound();

            string uploadsFolder = Path.Combine(_hostEnvironment.WebRootPath, "uploads/verification");
            if (!Directory.Exists(uploadsFolder)) Directory.CreateDirectory(uploadsFolder);

            string uniqueFileName = "VERIF_" + user.Id + "_" + document.FileName;
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