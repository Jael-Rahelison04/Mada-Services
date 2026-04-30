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
        public async Task<IActionResult> Create(int providerId, decimal rating, string comment)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null) return Challenge();

            // Correction : providerId est maintenant un int
            if (user.Id == providerId)
            {
                TempData["Error"] = "Interdit sur son propre profil.";
                return RedirectToAction("Details", "Provider", new { id = providerId });
            }

            var review = new Review
            {
                Rating = rating,
                Comment = comment ?? "",
                ProviderId = providerId, // int = int (OK)
                CustomerName = user.FullName ?? "Anonyme",
                DatePosted = DateTime.Now
            };

            _context.Reviews.Add(review);
            await _context.SaveChangesAsync();
            return RedirectToAction("Details", "Provider", new { id = providerId });
        }
    }
}