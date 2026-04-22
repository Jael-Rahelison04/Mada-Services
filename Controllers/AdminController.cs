using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using MadaServices.Models;
using Microsoft.EntityFrameworkCore;
using MadaServices.Data;
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;

namespace MadaServices.Controllers
{
    public class AdminController : Controller
    {
        private readonly UserManager<User> _userManager;
        private readonly ApplicationDbContext _context;

        public AdminController(UserManager<User> userManager, ApplicationDbContext context)
        {
            _userManager = userManager;
            _context = context;
        }

        public async Task<IActionResult> Index()
        {
            // 1. Récupération des données globales
            var allUsers = await _userManager.Users
                .OrderByDescending(u => u.CreatedAt)
                .ToListAsync();

            var categories = await _context.Categories.OrderBy(c => c.Name).ToListAsync();
            var cities = await _context.Cities.OrderBy(v => v.Name).ToListAsync();
            var reviews = await _context.Reviews.OrderByDescending(r => r.DatePosted).Take(10).ToListAsync();

            // 2. Initialisation du ViewModel
            var model = new AdminDashboardViewModel
            {
                TotalUsers = allUsers.Count,
                Categories = categories,
                Cities = cities,
                RecentReviews = reviews,
                VerifiedProvidersCount = await _context.Providers.CountAsync(p => p.IsVerified),
                RecentUsers = new List<UserWithRole>()
            };

            // 3. Mapping des utilisateurs avec vérification de rôle et statut
            foreach (var user in allUsers.Take(15))
            {
                var roles = await _userManager.GetRolesAsync(user);
                var roleName = roles.FirstOrDefault() ?? "Client";
                
                bool isVerified = false;
                if (roleName == "Provider")
                {
                    // Correction de l'erreur : Utilisation de Id au lieu de UserId
                    var provider = await _context.Providers.FirstOrDefaultAsync(p => p.Id == user.Id);
                    isVerified = provider?.IsVerified ?? false;
                }

                model.RecentUsers.Add(new UserWithRole 
                { 
                    User = user, 
                    Role = roleName,
                    IsVerified = isVerified
                });
            }

            return View(model);
        }

        [HttpPost]
        public async Task<IActionResult> VerifyProvider(int id) 
        {
            // Correction de l'erreur : Recherche par Id
            var provider = await _context.Providers.FirstOrDefaultAsync(p => p.Id == id);
            if (provider != null)
            {
                provider.IsVerified = true;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteReview(int id)
        {
            var review = await _context.Reviews.FindAsync(id);
            if (review != null)
            {
                _context.Reviews.Remove(review);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> AddCategory(string name) 
        {
            if (!string.IsNullOrWhiteSpace(name)) {
                if (!await _context.Categories.AnyAsync(c => c.Name.ToLower() == name.ToLower()))
                {
                    _context.Categories.Add(new Category { Name = name });
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCategory(int id) 
        {
            var cat = await _context.Categories.FindAsync(id);
            if (cat != null) {
                _context.Categories.Remove(cat);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> AddCity(string name) 
        {
            if (!string.IsNullOrWhiteSpace(name)) {
                if (!await _context.Cities.AnyAsync(c => c.Name.ToLower() == name.ToLower()))
                {
                    _context.Cities.Add(new City { Name = name });
                    await _context.SaveChangesAsync();
                }
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> DeleteCity(int id) 
        {
            var city = await _context.Cities.FindAsync(id);
            if (city != null) {
                _context.Cities.Remove(city);
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> UserDetails(int id) 
        {
            var user = await _userManager.Users.FirstOrDefaultAsync(u => u.Id == id);
            if (user == null) return NotFound();
            return View(user);
        }

        [HttpPost]
        public async Task<IActionResult> ToggleUserStatus(int id)
        {
            var user = await _userManager.FindByIdAsync(id.ToString());
            if (user == null) return NotFound();

            if (user.LockoutEnd > DateTime.Now)
            {
                await _userManager.SetLockoutEndDateAsync(user, null);
            }
            else
            {
                await _userManager.SetLockoutEndDateAsync(user, DateTimeOffset.MaxValue);
            }

            return RedirectToAction(nameof(Index));
        }
    } 
}