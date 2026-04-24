// Controllers/ProviderController.cs (VERSION ENTIÈREMENT CORRIGÉE)
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

        // Limite du portfolio définie ici pour être facile à modifier
        private const int MaxPortfolioItems = 10;

        public ProviderController(
            ApplicationDbContext context,
            UserManager<User> userManager,
            IWebHostEnvironment environment)
        {
            _context = context;
            _userManager = userManager;
            _environment = environment;
        }

        // ─────────────────────────────────────────────
        // FICHE DÉTAILS D'UN PRESTATAIRE (PUBLIC)
        // ─────────────────────────────────────────────
        public async Task<IActionResult> Details(string id)
        {
            if (string.IsNullOrEmpty(id)) return NotFound();

            if (!int.TryParse(id, out int providerId))
                return NotFound();

            var provider = await _context.Providers
                .Include(p => p.Reviews)
                .Include(p => p.PortfolioItems)
                .Include(p => p.Availabilities)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == providerId);

            if (provider == null) return NotFound();

            return View(provider);
        }

        // ─────────────────────────────────────────────
        // LISTE / RECHERCHE (PUBLIC)
        // ─────────────────────────────────────────────
        public async Task<IActionResult> Index(string? search, string? city)
        {
            var providerQuery = _context.Providers
                .Include(p => p.Reviews)
                .Include(p => p.Category)
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
                providerQuery = providerQuery.Where(p => p.City == city);

            var results = await providerQuery.ToListAsync();

            // Passer les filtres à la vue
            ViewBag.Cities = await _context.Cities.OrderBy(c => c.Name).ToListAsync();
            ViewBag.Categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            ViewData["CurrentQuery"] = search ?? string.Empty;
            ViewData["CurrentCity"] = city ?? string.Empty;

            return View(results);
        }

        // ─────────────────────────────────────────────
        // DASHBOARD PRESTATAIRE
        // ─────────────────────────────────────────────
        [Authorize(Roles = "Provider")]
        public async Task<IActionResult> Dashboard()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var provider = await _context.Providers
                .Include(p => p.Reviews)
                .Include(p => p.PortfolioItems)
                .Include(p => p.Bookings)
                .Include(p => p.Availabilities)
                .Include(p => p.Category)
                .FirstOrDefaultAsync(p => p.Id == user.Id);

            if (provider == null) return RedirectToAction("Index", "Home");

            // ✅ AJOUT : Passer la liste des catégories pour le <select>
            ViewBag.Categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            // Passer la limite au ViewBag pour l'afficher dans la vue
            ViewBag.MaxPortfolioItems = MaxPortfolioItems;
            ViewBag.CanAddPortfolio = (provider.PortfolioItems?.Count ?? 0) < MaxPortfolioItems;

            return View("~/Views/Dashboard/Index.cshtml", provider);
        }

        // ─────────────────────────────────────────────
        // MISE À JOUR DU PROFIL
        // ─────────────────────────────────────────────
        // Dans ProviderController.cs
        // Remplacer entièrement l'action UpdateProfile par celle-ci :

        [HttpPost]
        [Authorize(Roles = "Provider")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateProfile(
            string? JobTitle,
            string? City,
            string? Description,
            // ✅ FIX 2 : Recevoir HourlyRate en string pour éviter le problème de culture
            string? HourlyRate,
            // ✅ FIX 1 : Ajouter PhoneNumber dans les paramètres
            string? PhoneNumber,
            // ✅ AJOUT : Récupérer la catégorie choisie par le prestataire
            int? CategoryId)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var provider = await _context.Providers.FindAsync(user.Id);
            if (provider == null) return NotFound();

            // ✅ FIX 2 : Parser HourlyRate manuellement en InvariantCulture
            // pour éviter le bug avec la virgule française
            decimal parsedRate = 0;
            if (!string.IsNullOrWhiteSpace(HourlyRate))
            {
                // Nettoyer la valeur : supprimer espaces et remplacer virgule par point
                var cleanRate = HourlyRate
                    .Replace(" ", "")
                    .Replace("\u00a0", "") // espace insécable
                    .Replace(",", ".");

                decimal.TryParse(
                    cleanRate,
                    System.Globalization.NumberStyles.Any,
                    System.Globalization.CultureInfo.InvariantCulture,
                    out parsedRate);
            }

            // Mise à jour des champs du prestataire
            provider.JobTitle    = JobTitle    ?? string.Empty;
            provider.City        = City        ?? string.Empty;
            provider.Description = Description ?? string.Empty;
            provider.HourlyRate  = parsedRate;

            // ✅ FIX 1 : Sauvegarder le téléphone dans les DEUX champs
            // pour éviter toute confusion future
            var cleanPhone = PhoneNumber?.Trim() ?? string.Empty;
            provider.Phone       = cleanPhone;   // champ custom User
            provider.PhoneNumber = cleanPhone;   // champ standard Identity

            await _context.SaveChangesAsync();

            // Mettre à jour aussi via UserManager pour que PhoneNumber
            // soit bien synchronisé avec Identity
            await _userManager.SetPhoneNumberAsync(provider, cleanPhone);

            // ✅ AJOUT : Sauvegarder la catégorie
            // Si l'utilisateur choisit "-- Aucune catégorie --", CategoryId sera null
            if (CategoryId.HasValue && CategoryId.Value > 0)
            {
                // Vérifier que la catégorie existe réellement en base
                bool categoryExists = await _context.Categories
                    .AnyAsync(c => c.Id == CategoryId.Value);

                provider.CategoryId = categoryExists ? CategoryId : null;
            }
            else
            {
                provider.CategoryId = null;
            }

            await _context.SaveChangesAsync();

            TempData["Success"] = "Profil mis à jour avec succès !";
            return RedirectToAction(nameof(Dashboard));
        }

        // ─────────────────────────────────────────────
        // UPLOAD AVATAR
        // ─────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Provider")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadAvatar(IFormFile? avatarFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (avatarFile == null || avatarFile.Length == 0)
            {
                TempData["Error"] = "Aucun fichier sélectionné.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Vérification du type de fichier
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp" };
            var ext = Path.GetExtension(avatarFile.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
            {
                TempData["Error"] = "Format non supporté. Utilisez jpg, png ou webp.";
                return RedirectToAction(nameof(Dashboard));
            }

            var provider = await _context.Providers.FindAsync(user.Id);
            if (provider != null)
            {
                var fileName = await SaveFile(avatarFile, "avatars");

                // ✅ CORRECTION P11 : On écrit dans ImageUrl (hérité de User)
                // et non plus dans ProfileImageUrl (supprimé)
                provider.ImageUrl = "/uploads/avatars/" + fileName;
                await _context.SaveChangesAsync();
                TempData["Success"] = "Photo de profil mise à jour !";
            }

            return RedirectToAction(nameof(Dashboard));
        }

        // ─────────────────────────────────────────────
        // ✅ CORRECTION P8 : UPLOAD PORTFOLIO (unique, dans ce controller)
        // L'action identique dans DashboardController.cs doit être supprimée
        // ─────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Provider")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadPortfolio(
            IFormFile? portfolioFile,
            string? description)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (portfolioFile == null || portfolioFile.Length == 0)
            {
                TempData["Error"] = "Aucun fichier sélectionné.";
                return RedirectToAction(nameof(Dashboard));
            }

            // ✅ CORRECTION P12 : Vérifier la limite de 10 photos AVANT l'upload
            int currentCount = await _context.PortfolioItems
                .CountAsync(pi => pi.ProviderId == user.Id);

            if (currentCount >= MaxPortfolioItems)
            {
                TempData["Error"] = $"Limite atteinte : vous ne pouvez pas ajouter plus de {MaxPortfolioItems} photos dans votre portfolio.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Vérification du type de fichier image
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".webp", ".gif" };
            var ext = Path.GetExtension(portfolioFile.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
            {
                TempData["Error"] = "Format non supporté. Utilisez jpg, png, webp ou gif.";
                return RedirectToAction(nameof(Dashboard));
            }

            var fileName = await SaveFile(portfolioFile, "portfolio");

            var portfolioItem = new PortfolioItem
            {
                ImageUrl = "/uploads/portfolio/" + fileName,
                Description = description ?? "Sans description",
                ProviderId = user.Id
            };

            _context.PortfolioItems.Add(portfolioItem);
            await _context.SaveChangesAsync();

            // Informer combien il reste de places
            int remaining = MaxPortfolioItems - (currentCount + 1);
            TempData["Success"] = remaining > 0
                ? $"Photo ajoutée ! Il vous reste {remaining} emplacement(s)."
                : "Photo ajoutée ! Vous avez atteint la limite de 10 photos.";

            return RedirectToAction(nameof(Dashboard));
        }

        // ─────────────────────────────────────────────
        // SUPPRESSION D'UNE PHOTO DU PORTFOLIO
        // ─────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Provider")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeletePortfolioItem(int id)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Vérifier que l'item appartient bien au prestataire connecté
            var item = await _context.PortfolioItems
                .FirstOrDefaultAsync(pi => pi.Id == id && pi.ProviderId == user.Id);

            if (item != null)
            {
                // Supprimer le fichier physique
                if (!string.IsNullOrEmpty(item.ImageUrl))
                {
                    var fullPath = Path.Combine(
                        _environment.WebRootPath,
                        item.ImageUrl.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                    if (System.IO.File.Exists(fullPath))
                        System.IO.File.Delete(fullPath);
                }

                _context.PortfolioItems.Remove(item);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Photo supprimée du portfolio.";
            }
            else
            {
                TempData["Error"] = "Photo introuvable ou non autorisée.";
            }

            return RedirectToAction(nameof(Dashboard));
        }

        // ─────────────────────────────────────────────
        // ✅ CORRECTION P9 : MISE À JOUR STATUT RÉSERVATION
        // Commentaire corrigé + vérification renforcée
        // ─────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Provider")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UpdateBookingStatus(int bookingId, string status)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Statuts autorisés (évite une injection de valeur arbitraire)
            var allowedStatuses = new[] { "Accepted", "Completed", "Cancelled" };
            if (!allowedStatuses.Contains(status))
            {
                TempData["Error"] = "Statut invalide.";
                return RedirectToAction(nameof(Dashboard));
            }

            var booking = await _context.Bookings.FindAsync(bookingId);

            // ✅ CORRECTION P9 :
            // - Commentaire corrigé : ProviderId et user.Id sont tous les deux des int
            // - Vérification que la réservation appartient bien AU prestataire connecté
            //   pour éviter qu'un autre prestataire devine l'ID et change le statut
            if (booking == null || booking.ProviderId != user.Id)
            {
                TempData["Error"] = "Réservation introuvable ou accès refusé.";
                return RedirectToAction(nameof(Dashboard));
            }

            // On ne peut pas modifier une réservation déjà terminée ou annulée
            if (booking.Status == "Completed" || booking.Status == "Cancelled")
            {
                TempData["Error"] = "Cette réservation ne peut plus être modifiée.";
                return RedirectToAction(nameof(Dashboard));
            }

            booking.Status = status;
            await _context.SaveChangesAsync();

            TempData["Success"] = $"Statut de la mission mis à jour : {status}";
            return RedirectToAction(nameof(Dashboard));
        }

        // ─────────────────────────────────────────────
        // METTRE EN PAUSE / RÉACTIVER LE PROFIL
        // ─────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Provider")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TogglePause()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            var provider = await _context.Providers.FindAsync(user.Id);
            if (provider == null) return NotFound();

            provider.IsPaused = !provider.IsPaused;
            await _context.SaveChangesAsync();

            TempData["Success"] = provider.IsPaused
                ? "Votre profil est maintenant en pause. Vous n'apparaissez plus dans les recherches."
                : "Votre profil est de nouveau actif !";

            return RedirectToAction(nameof(Dashboard));
        }

        // ─────────────────────────────────────────────
        // GESTION DES DISPONIBILITÉS
        // ─────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Provider")]
        public async Task<IActionResult> UpdateAvailability(string day, bool isActive)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            // Valider que le jour est valide
            var validDays = new[] { "Lundi", "Mardi", "Mercredi", "Jeudi", "Vendredi", "Samedi", "Dimanche" };
            if (!validDays.Contains(day))
                return BadRequest("Jour invalide.");

            var availability = await _context.Availabilities
                .FirstOrDefaultAsync(a => a.ProviderId == user.Id && a.DayOfWeek == day);

            if (availability == null)
            {
                availability = new Availability
                {
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
            return Ok(new { success = true, day, isActive });
        }

        // ─────────────────────────────────────────────
        // ✅ CORRECTION P10 : UPLOAD DOCUMENT DE VÉRIFICATION (NOUVEAU)
        // ─────────────────────────────────────────────
        [HttpPost]
        [Authorize(Roles = "Provider")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> UploadVerificationDocument(IFormFile? documentFile)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Unauthorized();

            if (documentFile == null || documentFile.Length == 0)
            {
                TempData["Error"] = "Aucun fichier sélectionné.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Formats acceptés : images et PDF
            var allowedExtensions = new[] { ".jpg", ".jpeg", ".png", ".pdf" };
            var ext = Path.GetExtension(documentFile.FileName).ToLower();
            if (!allowedExtensions.Contains(ext))
            {
                TempData["Error"] = "Format non supporté. Utilisez jpg, png ou pdf.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Taille max : 5 Mo
            if (documentFile.Length > 5 * 1024 * 1024)
            {
                TempData["Error"] = "Le fichier est trop volumineux (max 5 Mo).";
                return RedirectToAction(nameof(Dashboard));
            }

            var provider = await _context.Providers.FindAsync(user.Id);
            if (provider == null) return NotFound();

            // Un prestataire déjà vérifié n'a pas besoin de re-soumettre
            if (provider.IsVerified)
            {
                TempData["Error"] = "Votre profil est déjà vérifié.";
                return RedirectToAction(nameof(Dashboard));
            }

            // Sauvegarder dans un dossier sécurisé (pas dans wwwroot !)
            // Les documents sensibles ne doivent pas être accessibles publiquement
            var uploadsFolder = Path.Combine(
                Directory.GetCurrentDirectory(),
                "PrivateUploads",
                "verification");

            if (!Directory.Exists(uploadsFolder))
                Directory.CreateDirectory(uploadsFolder);

            var fileName = $"doc_{user.Id}_{Guid.NewGuid()}{ext}";
            var filePath = Path.Combine(uploadsFolder, fileName);

            using (var stream = new FileStream(filePath, FileMode.Create))
            {
                await documentFile.CopyToAsync(stream);
            }

            // Mettre à jour le statut du prestataire
            provider.HasSubmittedDocs = true;
            provider.VerificationStatus = "Pending";   // En attente de validation admin
            provider.VerificationDocumentPath = fileName; // Stocker le nom (pas le chemin complet)

            await _context.SaveChangesAsync();

            TempData["Success"] = "Document envoyé ! L'administrateur examinera votre dossier sous 48h.";
            return RedirectToAction(nameof(Dashboard));
        }

        // ─────────────────────────────────────────────
        // UTILITAIRE : SAUVEGARDE DE FICHIER
        // ─────────────────────────────────────────────
        private async Task<string> SaveFile(IFormFile file, string subFolder)
        {
            var rootPath = _environment.WebRootPath;
            if (string.IsNullOrEmpty(rootPath))
                rootPath = Path.Combine(Directory.GetCurrentDirectory(), "wwwroot");

            var folderPath = Path.Combine(rootPath, "uploads", subFolder);
            if (!Directory.Exists(folderPath))
                Directory.CreateDirectory(folderPath);

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