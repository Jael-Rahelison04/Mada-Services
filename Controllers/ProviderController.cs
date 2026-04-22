using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MadaServices.Data;
using MadaServices.Models;

namespace MadaServices.Controllers
{
    public class ProviderController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;
        private readonly IWebHostEnvironment _environment;

        public ProviderController(ApplicationDbContext context, UserManager<User> userManager, IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // --- FICHE DÉTAILS ---
        public async Task<IActionResult> Details(string id)
{
    if (string.IsNullOrEmpty(id)) return NotFound();

    // 1. Conversion sécurisée du string en int
    if (!int.TryParse(id, out int providerId))
    {
        return NotFound();
    }

    // 2. Recherche avec l'ID converti
    var provider = await _context.Providers
        .Include(p => p.Reviews)
        .Include(p => p.PortfolioItems)
        .Include(p => p.Availabilities)
        .FirstOrDefaultAsync(p => p.Id == providerId); // Utilisation de l'entier

    if (provider == null) return NotFound();

    return View(provider);
}
        // --- RECHERCHE (Index) ---
        public async Task<IActionResult> Index(string? search, string? city)
        {
            var providerQuery = _context.Providers
                .Include(p => p.Reviews)
                .Where(p => !p.IsPaused) 
                .AsQueryable();

            if (!string.IsNullOrEmpty(search))
            {
                var lowerSearch = search.ToLower();
                providerQuery = providerQuery.Where(p => 
                    p.FullName.ToLower().Contains(lowerSearch) || 
                    (p.JobTitle != null && p.JobTitle.ToLower().Contains(lowerSearch)) ||
                    (p.Description != null && p.Description.ToLower().Contains(lowerSearch)));
            }

            if (!string.IsNullOrEmpty(city))
            {
                providerQuery = providerQuery.Where(p => p.City == city);
            }

            var results = await providerQuery.ToListAsync();
            ViewData["CurrentQuery"] = search ?? string.Empty;
            ViewData["CurrentCity"] = city ?? string.Empty;

            return View(results); 
        }

        // --- DASHBOARD ---
        [Authorize]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var provider = await _context.Providers
                .Include(p => p.Reviews)
                .Include(p => p.PortfolioItems)
                .Include(p => p.Bookings)
                .Include(p => p.Availabilities)
                .FirstOrDefaultAsync(p => p.Id == user.Id);

            if (provider == null) return RedirectToAction("Index", "Home");

            return View("~/Views/Dashboard/Index.cshtml", provider);
        }

        // --- GESTION DES MISSIONS ---
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, string status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var booking = await _context.Bookings.FindAsync(bookingId);
            
            // Correction de la comparaison (ProviderId doit être comparé à user.Id)
            // Si ProviderId dans ton modèle Booking est un int, il faut s'assurer de la cohérence.
            // Ici on assume que ProviderId est une string (ID Identity).
            if (booking == null || booking.ProviderId != user.Id) return NotFound();

            booking.Status = status;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Statut mis à jour : {status}";
            return RedirectToAction(nameof(Dashboard));
        }

        // --- GESTION DISPONIBILITÉS ---
        [HttpPost]
        [Authorize]
        public async Task<IActionResult> UpdateAvailability(string day, bool isActive)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var availability = await _context.Availabilities
                .FirstOrDefaultAsync(a => a.ProviderId == user.Id && a.DayOfWeek == day);

            if (availability == null)
            {
                availability = new Availability { 
                    DayOfWeek = day, 
                    IsActive = isActive, 
                    ProviderId = user.Id 
                };
                _context.Availabilities.Add(availability);
            }
            else
            {
                availability.IsActive = isActive;
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // --- METTRE EN PAUSE ---
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePause()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var provider = await _context.Providers.FindAsync(user.Id);
            if (provider == null) return NotFound();

            provider.IsPaused = !provider.IsPaused;
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Dashboard));
        }

        // --- MISE À JOUR PROFIL ---
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(string? JobTitle, string? City, string? Description, decimal HourlyRate)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var provider = await _context.Providers.FindAsync(user.Id);
            
            if (provider != null)
            {
                provider.JobTitle = JobTitle ?? string.Empty;
                provider.City = City ?? string.Empty;
                provider.Description = Description ?? string.Empty;
                provider.HourlyRate = HourlyRate;

                await _context.SaveChangesAsync();
                TempData["Success"] = "Profil mis à jour !";
            }
            return RedirectToAction(nameof(Dashboard));
        }

        // --- UPLOAD AVATAR ---
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile? avatarFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (avatarFile != null && avatarFile.Length > 0)
            {
                var provider = await _context.Providers.FindAsync(user.Id);
                if (provider != null)
                {
                    var fileName = await SaveFile(avatarFile, "avatars");
                    provider.ProfileImageUrl = "/uploads/avatars/" + fileName;
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Dashboard));
        }

        // --- UPLOAD PORTFOLIO ---
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPortfolio(IFormFile? portfolioFile, string? description)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (portfolioFile != null && portfolioFile.Length > 0)
            {
                var fileName = await SaveFile(portfolioFile, "portfolio");
                var portfolioItem = new PortfolioItem
                {
                    ImageUrl = "/uploads/portfolio/" + fileName,
                    Description = description ?? "Sans description",
                    ProviderId = user.Id
                };
                _context.PortfolioItems.Add(portfolioItem);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Dashboard));
        }

        // --- UTILITAIRE SAUVEGARDE ---
        private async Task<string> SaveFile(IFormFile file, string subFolder)
        {
            var rootPath = _environment.WebRootPath;
            if (string.IsNullOrEmpty(rootPath)) 
            {
                // Sécurité pour éviter l'erreur de déréférencement null sur WebRootPath
                rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");
            }

            var folderPath = Path.Combine(rootPath, "uploads", subFolder);
            if (!Directory.Exists(folderPath)) Directory.CreateDirectory(folderPath);

            var fileName = $"{Guid.NewGuid()}{Path.GetExtension(file.FileName)}";
            var filePath = Path.Combine(folderPath, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await file.CopyToAsync(stream);
            }
            return fileName;
        }
    }
}