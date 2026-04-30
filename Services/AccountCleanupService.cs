// Services/AccountCleanupService.cs
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using MadaServices.Models;

namespace MadaServices.Services
{
    public class AccountCleanupService : BackgroundService
    {
        // IServiceScopeFactory car UserManager est un service "Scoped"
        // et BackgroundService est "Singleton" → on ne peut pas l'injecter directement
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AccountCleanupService> _logger;

        // Intervalle de vérification : toutes les 24 heures
        private readonly TimeSpan _interval = TimeSpan.FromHours(24);

        public AccountCleanupService(
            IServiceScopeFactory scopeFactory,
            ILogger<AccountCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger       = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("AccountCleanupService démarré.");

            while (!stoppingToken.IsCancellationRequested)
            {
                await RunCleanupAsync();
                await Task.Delay(_interval, stoppingToken);
            }
        }

        private async Task RunCleanupAsync()
        {
            using var scope       = _scopeFactory.CreateScope();
            var userManager       = scope.ServiceProvider.GetRequiredService<UserManager<User>>();
            var logger            = _logger;
            var expirationDate    = DateTime.UtcNow.AddDays(-30); // suspendu depuis > 30 jours

            // Récupérer tous les comptes suspendus depuis plus de 30 jours
            var expiredUsers = await userManager.Users
                .Where(u => u.SuspendedAt.HasValue
                         && u.SuspendedAt.Value <= expirationDate)
                .ToListAsync();

            if (!expiredUsers.Any())
            {
                logger.LogInformation(
                    "Nettoyage : aucun compte expiré trouvé ({Date})",
                    DateTime.UtcNow.ToString("dd/MM/yyyy HH:mm"));
                return;
            }

            int deleted = 0;
            foreach (var user in expiredUsers)
            {
                var result = await userManager.DeleteAsync(user);
                if (result.Succeeded)
                {
                    deleted++;
                    logger.LogWarning(
                        "Compte supprimé automatiquement : {Name} ({Email}) — suspendu le {Date}",
                        user.FullName, user.Email,
                        user.SuspendedAt!.Value.ToString("dd/MM/yyyy"));
                }
                else
                {
                    logger.LogError(
                        "Échec suppression de {Email} : {Errors}",
                        user.Email,
                        string.Join(", ", result.Errors.Select(e => e.Description)));
                }
            }

            logger.LogInformation(
                "Nettoyage terminé : {Count} compte(s) supprimé(s).", deleted);
        }
    }
}