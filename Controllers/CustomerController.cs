using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using MadaServices.Data;
using MadaServices.Models;
using System.Security.Claims;

namespace MadaServices.Controllers
{
    [Authorize] // Oblige la connexion pour tout le contrôleur
    public class CustomerController : Controller
    {
        private readonly ApplicationDbContext _context;

        public CustomerController(ApplicationDbContext context)
        {
            _context = context;
        }

        // --- DASHBOARD CLIENT ---
        public async Task<IActionResult> Index()
        {
            // Récupère les avis (Idéalement filtrés par l'ID du client connecté plus tard)
            var myReviews = await _context.Reviews
                .Include(r => r.Provider)
                .OrderByDescending(r => r.DatePosted) // Tri par date
                .ToListAsync(); 

            ViewBag.TotalReviews = myReviews.Count;
            var lastProviderId = myReviews.FirstOrDefault()?.ProviderId;
                if (lastProviderId.HasValue)
                {
                    var lastProvider = await _context.Users.OfType<Provider>()
                        .FirstOrDefaultAsync(p => p.Id == lastProviderId.Value);
                    ViewBag.LastProvider = lastProvider?.FullName ?? "Aucun";
                }

            return View(myReviews.Take(3).ToList());
        }

        // --- LISTE COMPLÈTE DES AVIS DU CLIENT ---
        public async Task<IActionResult> MyReviews()
        {
            var myReviews = await _context.Reviews
                .Include(r => r.Provider)
                .OrderByDescending(r => r.DatePosted)
                .ToListAsync();

            return View(myReviews);
        }

        // --- SUPPRESSION D'UN AVIS ---
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
                TempData["Success"] = "L'avis a été supprimé avec succès.";
            }

            return RedirectToAction(nameof(MyReviews));
        }
    }
}