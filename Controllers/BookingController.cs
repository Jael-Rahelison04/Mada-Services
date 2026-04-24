// Controllers/BookingController.cs (VERSION CORRIGÉE - Redirection propre)
// Ce controller n'est plus nécessaire. Toute la logique des réservations
// est gérée dans CustomerController.cs (CreateBooking, MyBookings).
//
// ✅ ACTION : Supprimer ce fichier ou le vider ainsi :

using Microsoft.AspNetCore.Mvc;

namespace MadaServices.Controllers
{
    // Ce controller est conservé uniquement pour la compatibilité
    // avec d'éventuels liens existants, mais redirige vers Customer.
    public class BookingController : Controller
    {
        // Redirige tout vers l'espace client
        public IActionResult Index()
        {
            return RedirectToAction("Index", "Customer");
        }
    }
}