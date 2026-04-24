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
        // 1. Prestataires vedettes (gardé tel quel)
        var featuredProviders = await _context.Providers
            .Include(p => p.Reviews)
            .OrderByDescending(p => p.Reviews.Any()
                ? p.Reviews.Average(r => (double)r.Rating)
                : 0)
            .Take(4)
            .ToListAsync();

        // 2. Données pour les filtres de la page d’accueil
        ViewBag.Cities = await _context.Cities
            .OrderBy(c => c.Name)
            .ToListAsync();

        ViewBag.Categories = await _context.Categories
            .OrderBy(c => c.Name)
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

        // Plus besoin de ViewBag.IsAuthenticated
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