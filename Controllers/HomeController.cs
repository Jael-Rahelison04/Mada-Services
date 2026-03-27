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
        var featuredProviders = await _context.Users.OfType<Provider>()
            .Include(p => p.Reviews)
            .OrderByDescending(p => p.Reviews.Average(r => r.Rating))
            .Take(4)
            .ToListAsync();
            
        return View(featuredProviders);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}