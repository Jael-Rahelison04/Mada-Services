using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MadaServices.Data;
using MadaServices.Models;

namespace MadaServices.Controllers
{
    // Seuls les utilisateurs avec le rôle "Admin" peuvent accéder à ce contrôleur
    [Authorize(Roles = "Admin")] 
    public class AdminController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Liste des prestataires en attente de vérification
        public async Task<IActionResult> Index()
        {
            var pendingProviders = await _context.Providers
                .Where(p => p.HasSubmittedDocs && !p.IsVerified)
                .ToListAsync();

            return View(pendingProviders);
        }

        // Action pour valider un expert
        [HttpPost]
        public async Task<IActionResult> VerifyProvider(int id)
        {
            var provider = await _context.Providers.FindAsync(id);
            if (provider != null)
            {
                provider.IsVerified = true;
                _context.Update(provider);
                await _context.SaveChangesAsync();
                TempData["Success"] = $"Le profil de {provider.FullName} est désormais certifié !";
            }
            return RedirectToAction(nameof(Index));
        }

        // Action pour rejeter (si le document est illisible par exemple)
        [HttpPost]
        public async Task<IActionResult> RejectProvider(int id)
        {
            var provider = await _context.Providers.FindAsync(id);
            if (provider != null)
            {
                provider.HasSubmittedDocs = false; // Permet au prestataire de renvoyer un document
                _context.Update(provider);
                await _context.SaveChangesAsync();
                TempData["Success"] = "Demande rejetée. Le prestataire pourra soumettre un nouveau document.";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}