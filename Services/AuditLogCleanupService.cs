using AdminMembers.Data;
using Microsoft.EntityFrameworkCore;

namespace AdminMembers.Services
{
    public class AuditLogCleanupService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<AuditLogCleanupService> _logger;
        private const int RetentionDays = 90;
        private static readonly TimeSpan RunInterval = TimeSpan.FromDays(1);

        public AuditLogCleanupService(IServiceScopeFactory scopeFactory, ILogger<AuditLogCleanupService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Audit log cleanup service started. Retention: {Days} days, runs every 24 hours.", RetentionDays);

            // Run once immediately on startup, then every 24 hours
            while (!stoppingToken.IsCancellationRequested)
            {
                await PurgeOldLogsAsync(stoppingToken);
                await Task.Delay(RunInterval, stoppingToken);
            }
        }

        private async Task PurgeOldLogsAsync(CancellationToken cancellationToken)
        {
            try
            {
                var cutoff = DateTime.UtcNow.AddDays(-RetentionDays);

                using var scope = _scopeFactory.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var deleted = await db.AuditLogs
                    .Where(l => l.Timestamp < cutoff)
                    .ExecuteDeleteAsync(cancellationToken);

                if (deleted > 0)
                    _logger.LogInformation("Audit log cleanup: deleted {Count} entries older than {Days} days.", deleted, RetentionDays);
            }
            catch (OperationCanceledException)
            {
                // Graceful shutdown — expected
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during audit log cleanup.");
            }
        }
    }
}
