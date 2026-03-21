using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using MadaServices.Data;
using MadaServices.Models;
using Microsoft.EntityFrameworkCore;

namespace MadaServices.Controllers
{
    [Authorize] 
    public class ReviewController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly UserManager<User> _userManager;

        public ReviewController(ApplicationDbContext context, UserManager<User> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(int providerId, int rating, string comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            var provider = await _context.Providers.FindAsync(providerId);
            if (provider == null) return NotFound("Prestataire introuvable.");

            if (user.Id == providerId)
            {
                TempData["Error"] = "Vous ne pouvez pas laisser un avis sur votre propre profil.";
                return RedirectToAction("Details", "Provider", new { id = providerId });
            }

            var review = new Review
            {
                Rating = rating,
                Comment = comment ?? "",
                ProviderId = providerId
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();

            TempData["Success"] = "Merci ! Votre avis a été publié.";
            
            return RedirectToAction("Details", "Provider", new { id = providerId });
        }
    }
}