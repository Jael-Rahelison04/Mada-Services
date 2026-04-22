using Microsoft.AspNetCore.Mvc;
using MadaServices.Models; // Assure-toi que c'est le bon namespace pour tes modèles
using System;

namespace MadaServices.Controllers
{
    public class BookingController : Controller
    {
        // Si tu as une base de données (Entity Framework)
        // private readonly ApplicationDbContext _context;
        // public BookingController(ApplicationDbContext context) { _context = context; }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public IActionResult Create(int ProviderId, string ServiceName, DateTime BookingDate, decimal Price, string Description)
        {
            // 1. Logique d'enregistrement (Exemple sans DB pour tester)
            // Ici, tu devrais créer un objet Booking et l'ajouter à ta table Bookings
            
            /* var newBooking = new Booking {
                ProviderId = ProviderId,
                ServiceName = ServiceName,
                BookingDate = BookingDate,
                Price = Price,
                Description = Description,
                Status = "Pending", // Statut par défaut
                UserId = ... // L'ID de l'utilisateur connecté
            };
            _context.Bookings.Add(newBooking);
            _context.SaveChanges();
            */

            // 2. Message de succès (Optionnel)
            TempData["Success"] = "Votre demande de service a été envoyée avec succès !";

            // 3. Redirection vers la page "Espace Client" (remplace "Client" par le nom de ton contrôleur actuel)
            return RedirectToAction("Index", "Client"); 
        }
    }
}