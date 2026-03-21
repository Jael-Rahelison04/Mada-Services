using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MadaServices.Data;
using MadaServices.Models;
using System.Security.Claims;

namespace MadaServices.Controllers
{
    public class ProviderController : Controller
    {
        private readonly ApplicationDbContext _context;

        public ProviderController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- MA RÉPUTATION (Vue pour le prestataire connecté) ---
        [Authorize(Roles = "Provider")]
        public async Task<IActionResult> Reviews()
        {
            var userEmail = User.Identity?.Name;

            var provider = await _context.Providers
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(p => p.Email == userEmail); 

            if (provider == null) 
                return NotFound("Profil prestataire non trouvé.");

            return View(provider);
        }

        // --- RECHERCHE AVANCÉE ---
        public async Task<IActionResult> Search(string query, string city)
        {
            var providersQuery = _context.Providers
                .Include(p => p.Reviews) 
                .AsQueryable();

            if (!string.IsNullOrEmpty(query))
            {
                providersQuery = providersQuery.Where(p => 
                    p.JobTitle.Contains(query) || 
                    p.FullName.Contains(query) || 
                    p.Description.Contains(query));
            }

            if (!string.IsNullOrEmpty(city))
            {
                providersQuery = providersQuery.Where(p => p.City == city);
            }

            var results = await providersQuery
                .OrderByDescending(p => p.IsVerified)
                .ToListAsync();
            
            ViewData["SearchTerm"] = query;
            ViewData["City"] = city;
            
            return View("~/Views/Home/Index.cshtml", results); 
        }

        // --- DÉTAILS DU PRESTATAIRE ---
        public async Task<IActionResult> Details(int? id)
        {
            if (id == null) return NotFound();

            var provider = await _context.Providers
                .Include(p => p.Reviews)
                .FirstOrDefaultAsync(m => m.Id == id);

            if (provider == null) return NotFound();

            // Règle 4.2 : Masquer le téléphone si non connecté
            ViewBag.CanSeeContact = User.Identity?.IsAuthenticated ?? false;

            return View(provider);
        }

        // --- FORMULAIRE D'AVIS ---
        [Authorize]
        public async Task<IActionResult> AddReview(int id)
        {
            var provider = await _context.Providers.FindAsync(id);
            if (provider == null) return NotFound();

            // Sécurité : Un prestataire ne peut pas se noter lui-même (via son email)
            if (User.IsInRole("Provider") && provider.Email == User.Identity?.Name)
            {
                TempData["Error"] = "Vous ne pouvez pas laisser un avis sur votre propre profil.";
                return RedirectToAction("Details", new { id = id });
            }

            return View(provider);
        }

        // --- TRAITEMENT DE L'AVIS (Règle 4.3) ---
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> SubmitReview(int ProviderId, int Rating, string Comment)
        {
            var currentCustomerName = User.Identity?.Name;

            // 1. Validation de la note
            if (Rating < 1 || Rating > 5) 
            {
                TempData["Error"] = "La note doit être comprise entre 1 et 5.";
                return RedirectToAction("Details", new { id = ProviderId });
            }

            // 2. Vérification d'unicité (Règle 4.3 : Un seul avis par client/prestataire)
            bool alreadyExists = await _context.Reviews.AnyAsync(r => 
                r.ProviderId == ProviderId && r.CustomerName == currentCustomerName);

            if (alreadyExists)
            {
                TempData["Error"] = "Vous avez déjà déposé un avis sur ce prestataire.";
                return RedirectToAction("Details", new { id = ProviderId });
            }

            // 3. Création de l'avis
            var review = new Review
            {
                ProviderId = ProviderId,
                Rating = Rating,
                Comment = Comment ?? string.Empty,
                CustomerName = currentCustomerName ?? "Anonyme",
                DatePosted = DateTime.Now // Activé car présent dans ton modèle
            };

            try 
            {
                _context.Reviews.Add(review);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Votre avis a été publié avec succès !";
            }
            catch (Exception) 
            {
                TempData["Error"] = "Une erreur est survenue lors de l'enregistrement.";
            }

            return RedirectToAction("Details", new { id = ProviderId });
        }
    }
}