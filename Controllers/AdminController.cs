using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;   // ✅ FIX 13
using MadaServices.Models;
using Microsoft.EntityFrameworkCore;
using MadaServices.Data;
using System.Security.Claims;

namespace MadaServices.Controllers
{
    // ✅ FIX 13 : Protection totale — seul le rôle Admin peut accéder
    // Sans cet attribut, n'importe quel visiteur pouvait accéder à /Admin/Index
    [Authorize(Roles = "Admin")]
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _hostEnvironment;

        public AdminController(
            UserManager<User> userManager,
            ApplicationDbContext context,
            IWebHostEnvironment hostEnvironment)
        {
            _userManager = userManager;
            _context = context;
            _hostEnvironment = hostEnvironment;
        }

        // ─────────────────────────────────────────────
        // DASHBOARD PRINCIPAL
        // ─────────────────────────────────────────────
        public async Task<IActionResult> Index()
        {
            // 1. Tous les utilisateurs triés par date d'inscription
            var allUsers = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            // ✅ FIX 14 : Compter séparément Providers et Clients
            // On utilise GetUsersInRoleAsync pour être précis
            var providers = await _userManager.GetUsersInRoleAsync("Provider");
            var clients   = await _userManager.GetUsersInRoleAsync("Client");

            // ✅ FIX 16 : Include(r => r.Provider) pour éviter NullReferenceException
            // On prend les 20 derniers avis avec le prestataire associé
            var reviews = await _context.Reviews
                .Include(r => r.Provider)           // ← était manquant
                .OrderByDescending(r => r.DatePosted)
                .Take(20)
                .ToListAsync();

            var categories = await _context.Categories
                .OrderBy(c => c.Name)
                .ToListAsync();

            var cities = await _context.Cities
                .OrderBy(v => v.Name)
                .ToListAsync();

            // 2. Construction du ViewModel
            var model = new AdminDashboardViewModel
            {
                TotalUsers = allUsers.Count,

                // ✅ FIX 14 : Ces deux champs étaient toujours à 0
                TotalProviders = providers.Count,
                TotalClients   = clients.Count,

                VerifiedProvidersCount = await _context.Providers
                    .CountAsync(p => p.IsVerified),

                Categories    = categories,
                Cities        = cities,
                RecentReviews = reviews,   // ✅ FIX 16 : avec Provider inclus
                RecentUsers   = new List<UserWithRole>()
            };

            // 3. Mapping des 15 derniers utilisateurs avec leur rôle
            foreach (var user in allUsers.Take(15))
            {
                var roles    = await _userManager.GetRolesAsync(user);
                var roleName = roles.FirstOrDefault() ?? "Client";

                bool isVerified = false;
                if (roleName == "Provider")
                {
                    var provider = await _context.Providers
                        .FirstOrDefaultAsync(p => p.Id == user.Id);
                    isVerified = provider?.IsVerified ?? false;
                }

                // ✅ FIX 15 : Vérifier le statut de suspension correctement
                // LockoutEnd > UtcNow signifie que le compte est suspendu
                bool isSuspended = user.LockoutEnd.HasValue
                    && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

                model.RecentUsers.Add(new UserWithRole
                {
                    User        = user,
                    Role        = roleName,
                    IsVerified  = isVerified,
                    IsSuspended = isSuspended
                });
            }

            return View(model);
        }

        // ─────────────────────────────────────────────
        // ✅ FIX 15 : SUSPENSION / RÉACTIVATION
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound();

            if (!user.LockoutEnabled)
            {
                user.LockoutEnabled = true;
                await _userManager.UpdateAsync(user);
            }

            bool isSuspended = user.LockoutEnd.HasValue
                && user.LockoutEnd.Value > DateTimeOffset.UtcNow;

            if (isSuspended)
            {
                // ── Lever la suspension ──────────────────────────
                await _userManager.SetLockoutEndDateAsync(user, null);

                // Recharger pour éviter conflit de concurrence
                user = await _userManager.FindByIdAsync(id.ToString());
                user.SuspendedAt = null;
                await _userManager.UpdateAsync(user);

                TempData["Success"] = $"Compte de {user.FullName} réactivé.";
            }
            else
            {
                // ── Suspendre + enregistrer la date ─────────────
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);

                user = await _userManager.FindByIdAsync(id.ToString());
                user.SuspendedAt = DateTime.UtcNow; // ← point de départ du compte à rebours
                await _userManager.UpdateAsync(user);

                TempData["Success"] =
                    $"Compte de {user.FullName} suspendu. " +
                    $"Suppression automatique dans 30 jours si non réactivé.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────────────────────────
        // VALIDER UN PRESTATAIRE
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> VerifyProvider(int id)
        {
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.Id == id);

            if (provider != null)
            {
                provider.IsVerified          = true;
                provider.VerificationStatus  = "Approved";
                await _context.SaveChangesAsync();
                TempData["Success"] = $"{provider.FullName} est maintenant vérifié.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────────────────────────
        // REJETER UN DOCUMENT DE VÉRIFICATION
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectProvider(int id)
        {
            var provider = await _context.Providers
                .FirstOrDefaultAsync(p => p.Id == id);

            if (provider != null)
            {
                provider.VerificationStatus  = "Rejected";
                provider.HasSubmittedDocs    = false;
                await _context.SaveChangesAsync();
                TempData["Warning"] = $"Dossier de {provider.FullName} refusé.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────────────────────────
        // SUPPRIMER UN AVIS
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();

                // ✅ Incrémenter le compteur de l'admin connecté
                var adminId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
                var admin   = await _userManager.FindByIdAsync(adminId.ToString());
                if (admin != null)
                {
                    admin.DeletedReviewsCount++;
                    await _userManager.UpdateAsync(admin);
                }

                TempData["Success"] = "Avis supprimé.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────────────────────────
        // GESTION DES CATÉGORIES
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCategory(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                bool exists = await _context.Categories
                    .AnyAsync(c => c.Name.ToLower() == name.ToLower());

                if (!exists)
                {
                    _context.Categories.Add(new Category { Name = name });
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Catégorie « {name} » ajoutée.";
                }
                else
                {
                    TempData["Error"] = $"La catégorie « {name} » existe déjà.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCategory(int id)
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat != null)
            {
                _context.Categories.Remove(cat);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Catégorie supprimée.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────────────────────────
        // GESTION DES VILLES
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddCity(string name)
        {
            if (!string.IsNullOrWhiteSpace(name))
            {
                bool exists = await _context.Cities
                    .AnyAsync(c => c.Name.ToLower() == name.ToLower());

                if (!exists)
                {
                    _context.Cities.Add(new City { Name = name });
                    await _context.SaveChangesAsync();
                    TempData["Success"] = $"Ville « {name} » ajoutée.";
                }
                else
                {
                    TempData["Error"] = $"La ville « {name} » existe déjà.";
                }
            }

            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteCity(int id)
        {
            var city = await _context.Cities.FindAsync(id);
            if (city != null)
            {
                _context.Cities.Remove(city);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Ville supprimée.";
            }

            return RedirectToAction(nameof(Index));
        }

        // ─────────────────────────────────────────────
        // DÉTAILS D'UN UTILISATEUR
        // ─────────────────────────────────────────────
        public async Task<IActionResult> UserDetails(int id)
        {
            var provider = await _context.Providers
                .Include(p => p.Reviews)
                .Include(p => p.Bookings)
                .Include(p => p.PortfolioItems)
                .FirstOrDefaultAsync(p => p.Id == id);

            User user;
            if (provider != null)
            {
                user = provider;
            }
            else
            {
                user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
                if (user == null) return NotFound();

                var roles = await _userManager.GetRolesAsync(user);
                var role  = roles.FirstOrDefault() ?? "Client";

                if (role == "Admin")
                {
                    // ✅ Stats spécifiques admin
                    ViewBag.SuspendedCount = await _userManager.Users
                        .CountAsync(u => u.LockoutEnd.HasValue
                                    && u.LockoutEnd.Value > DateTimeOffset.UtcNow);

                    ViewBag.VerifiedCount =
                        (await _context.Providers.CountAsync(p => p.IsVerified))
                        + await _userManager.Users.CountAsync(u => u.IsVerified
                            && !_context.Providers.Select(p => p.Id).Contains(u.Id));

                    ViewBag.DeletedReviews = user.DeletedReviewsCount;
                }
                else
                {
                    // ... chargement documents + bookings client (code existant)
                    var clientDocs = await _context.ClientDocuments
                        .Where(d => d.UserId == id)
                        .OrderByDescending(d => d.UploadedAt)
                        .ToListAsync();
                    ViewBag.ClientDocuments = clientDocs;

                    var clientBookings = await _context.Bookings
                        .Where(b => b.ClientId == id)
                        .OrderByDescending(b => b.CreatedAt)
                        .Take(5)
                        .ToListAsync();
                    ViewBag.ClientBookings      = clientBookings;
                    ViewBag.ClientBookingsTotal = await _context.Bookings
                        .CountAsync(b => b.ClientId == id);
                }
            }

            var userRoles = await _userManager.GetRolesAsync(user);
            ViewBag.UserRole = userRoles.FirstOrDefault() ?? "Client";

            return View(user);
        }

        // ─────────────────────────────────────────────
        // MODIFIER LE PROFIL ADMIN
        // ─────────────────────────────────────────────
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditProfile(
            int id,
            string fullName,
            string email,
            string? newPassword,
            string? confirmPassword,
            IFormFile? photo)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound();

            // ── 1. Propriétés simples (FullName + photo) ──────────────────
            user.FullName = fullName;

            if (photo != null && photo.Length > 0)
            {
                var uploadsDir = Path.Combine(
                    Directory.GetCurrentDirectory(), "wwwroot", "uploads", "avatars");
                Directory.CreateDirectory(uploadsDir);

                var fileName = $"avatar_{id}_{Guid.NewGuid()}{Path.GetExtension(photo.FileName)}";
                var filePath = Path.Combine(uploadsDir, fileName);

                using (var stream = new FileStream(filePath, FileMode.Create))
                    await photo.CopyToAsync(stream);

                user.ImageUrl = $"/uploads/avatars/{fileName}";
            }

            // ✅ UN SEUL UpdateAsync pour FullName + ImageUrl
            var updateResult = await _userManager.UpdateAsync(user);
            if (!updateResult.Succeeded)
            {
                TempData["Error"] = "Erreur lors de la mise à jour du profil.";
                return RedirectToAction(nameof(UserDetails), new { id });
            }

            // ── 2. Email (SetEmailAsync fait son propre UpdateAsync) ───────
            if (!string.Equals(user.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                // Recharger l'objet frais pour éviter le conflit de concurrence
                user = await _userManager.FindByIdAsync(id.ToString());

                var setEmailResult = await _userManager.SetEmailAsync(user, email);
                if (!setEmailResult.Succeeded)
                {
                    TempData["Error"] = "Erreur lors du changement d'email.";
                    return RedirectToAction(nameof(UserDetails), new { id });
                }

                await _userManager.SetUserNameAsync(user, email);
            }

            // ── 3. Mot de passe (indépendant, token frais) ─────────────────
            if (!string.IsNullOrWhiteSpace(newPassword))
            {
                if (newPassword != confirmPassword)
                {
                    TempData["Error"] = "Les mots de passe ne correspondent pas.";
                    return RedirectToAction(nameof(UserDetails), new { id });
                }

                // Recharger à nouveau pour avoir le ConcurrencyStamp à jour
                user = await _userManager.FindByIdAsync(id.ToString());

                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var passResult = await _userManager.ResetPasswordAsync(user, token, newPassword);

                if (!passResult.Succeeded)
                {
                    TempData["Error"] = "Mot de passe invalide (min. 6 car., majuscule, chiffre, symbole).";
                    return RedirectToAction(nameof(UserDetails), new { id });
                }
            }

            TempData["Success"] = "Profil mis à jour avec succès.";
            return RedirectToAction(nameof(UserDetails), new { id });
        }

        // Ajouter cette action dans AdminController.cs
        // Elle sert le fichier document de vérification de manière sécurisée

        // ❌ REMPLACER toute l'action ViewDocument existante par celle-ci :

        [HttpGet]
        public IActionResult ViewDocument(int id, string docType = "cin", bool download = false)
        {
            var provider = _context.Providers.FirstOrDefault(p => p.Id == id);
            if (provider == null) return NotFound("Prestataire introuvable.");

            // ── Sélectionner le bon chemin selon le type demandé ──────────
            string? fileName = docType switch
            {
                "cin"       => provider.CinDocumentPath,
                "cv"        => provider.CvDocumentPath,
                "residence" => provider.ResidenceCertPath,
                "diplome"   => provider.DiplomaDocumentPath,
                _           => null
            };

            if (string.IsNullOrEmpty(fileName))
                return NotFound("Aucun document soumis pour ce type.");

            var filePath = Path.Combine(
                Directory.GetCurrentDirectory(),
                "PrivateUploads", "verification", fileName);

            if (!System.IO.File.Exists(filePath))
                return NotFound("Fichier introuvable sur le serveur.");

            var ext = Path.GetExtension(filePath).ToLower();
            var contentType = ext switch
            {
                ".pdf"  => "application/pdf",
                ".png"  => "image/png",
                ".jpg"  => "image/jpeg",
                ".jpeg" => "image/jpeg",
                _       => "application/octet-stream"
            };

            var fileBytes = System.IO.File.ReadAllBytes(filePath);
            var outFileName = $"{docType}_{id}{ext}";

            return download
                ? File(fileBytes, contentType, outFileName)
                : File(fileBytes, contentType);
        }

        // ─────────────────────────────────────────────────────────────
        // 2. NOUVELLE ACTION — Visualiser / Télécharger un document client
        //    Même pattern que ViewDocument (providers) déjà en place
        // ─────────────────────────────────────────────────────────────
        
        [HttpGet]
        public async Task<IActionResult> ViewClientDocument(int id, bool download = false)
        {
            var doc = await _context.ClientDocuments.FindAsync(id);
            if (doc == null) return NotFound();
        
            var absolutePath = Path.Combine(
                _hostEnvironment.WebRootPath,
                doc.FilePath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar)
            );
        
            if (!System.IO.File.Exists(absolutePath))
                return NotFound("Le fichier est introuvable sur le serveur.");
        
            var fileBytes = await System.IO.File.ReadAllBytesAsync(absolutePath);
        
            var ext = Path.GetExtension(absolutePath).ToLowerInvariant();
            var contentType = ext switch
            {
                ".pdf"  => "application/pdf",
                ".png"  => "image/png",
                ".jpg"  => "image/jpeg",
                ".jpeg" => "image/jpeg",
                _       => "application/octet-stream"
            };
        
            // Nom lisible pour le téléchargement
            var fileName = $"{doc.DocumentType}_user{doc.UserId}{ext}";
        
            // Même fix que pour providers :
            // File(bytes, type, fileName) = Content-Disposition: attachment
            // File(bytes, type)           = Content-Disposition: inline
            if (download)
                return File(fileBytes, contentType, fileName);
            else
                return File(fileBytes, contentType);
        }
        
        
        // ─────────────────────────────────────────────────────────────
        // 3. NOUVELLE ACTION — Approuver un document client
        // ─────────────────────────────────────────────────────────────
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ApproveClientDocument(int id)
        {
            var doc = await _context.ClientDocuments.FindAsync(id);
            if (doc == null) return NotFound();
        
            doc.Status = "Approved";
            await _context.SaveChangesAsync();
        
            // Si TOUS les documents du client sont approuvés → passer IsVerified à true
            var allDocs = await _context.ClientDocuments
                .Where(d => d.UserId == doc.UserId)
                .ToListAsync();
        
            if (allDocs.Any() && allDocs.All(d => d.Status == "Approved"))
            {
                var user = await _userManager.FindByIdAsync(doc.UserId.ToString());
                if (user != null)
                {
                    user.IsVerified = true;
                    await _userManager.UpdateAsync(user);
                }
            }
        
            TempData["Success"] = "Document approuvé avec succès.";
            return RedirectToAction(nameof(UserDetails), new { id = doc.UserId });
        }
        
        
        // ─────────────────────────────────────────────────────────────
        // 4. NOUVELLE ACTION — Rejeter un document client
        // ─────────────────────────────────────────────────────────────
        
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> RejectClientDocument(int id)
        {
            var doc = await _context.ClientDocuments.FindAsync(id);
            if (doc == null) return NotFound();
        
            doc.Status = "Rejected";
        
            // Si on rejette un document, le compte repasse en non-vérifié
            var user = await _userManager.FindByIdAsync(doc.UserId.ToString());
            if (user != null && user.IsVerified)
            {
                user.IsVerified = false;
                await _userManager.UpdateAsync(user);
            }
        
            await _context.SaveChangesAsync();
        
            TempData["Error"] = "Document rejeté. Le client devra le resoumettre.";
            return RedirectToAction(nameof(UserDetails), new { id = doc.UserId });
        }
    }
}