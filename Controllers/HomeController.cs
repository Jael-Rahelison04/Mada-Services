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

    public async Task<IActionResult> Index()
    {
        // On récupère les 4 prestataires les mieux notés pour la page d'accueil
        var featuredProviders = await _context.Providers
            .Include(p => p.Reviews)
            .OrderByDescending(p => p.Reviews.Average(r => r.Rating))
            .Take(4)
            .ToListAsync();
            
        return View(featuredProviders);
    }

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

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}