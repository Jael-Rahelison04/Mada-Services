using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using MadaServices.Data;
using MadaServices.Models;

namespace MadaServices.Controllers;

public class HomeController : Controller
{
    private readonly ApplicationDbContext _context;

    public HomeController(ApplicationDbContext context)
    {
        _context = context;
    }

    // --- PAGE D'ACCUEIL ---
    public async Task<IActionResult> Index()
    {
        // CORRECTION : On utilise une expression conditionnelle simple (ternaire)
        // EF traduit cela très bien en : CASE WHEN EXISTS... THEN AVG... ELSE 0 END
        var featuredProviders = await _context.Providers
            .Include(p => p.Reviews)
            .OrderByDescending(p => p.Reviews.Any() 
                ? p.Reviews.Average(r => (double)r.Rating) 
                : 0)
            .Take(4)
            .ToListAsync();
            
        return View(featuredProviders);
    }

    // --- DÉTAILS D'UN PRESTATAIRE ---
    public async Task<IActionResult> Details(int id)
    {
        var provider = await _context.Providers
            .Include(p => p.Reviews)
            .FirstOrDefaultAsync(p => p.Id == id);

        if (provider == null) return NotFound();

        // Détermine si on affiche le contact (utilisé dans la vue)
        ViewBag.IsAuthenticated = User.Identity?.IsAuthenticated ?? false;

        return View(provider);
    }

    // --- PAGE CONTACT ---
    public IActionResult Contact()
    {
        return View();
    }

    // --- PAGE À PROPOS ---
    public IActionResult About()
    {
        return View();
    }

    // --- GESTION DES ERREURS ---
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}