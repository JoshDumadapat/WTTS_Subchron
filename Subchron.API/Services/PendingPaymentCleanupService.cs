using Microsoft.EntityFrameworkCore;
using Subchron.API.Data;

namespace Subchron.API.Services;

/// <summary>Removes pending payment transactions older than 15 minutes so they do not accumulate.</summary>
public class PendingPaymentCleanupService : BackgroundService
{
    private static readonly TimeSpan PendingMaxAge = TimeSpan.FromMinutes(15);
    private static readonly TimeSpan RunInterval = TimeSpan.FromMinutes(5);
    private readonly IServiceProvider _services;
    private readonly ILogger<PendingPaymentCleanupService> _logger;

    public PendingPaymentCleanupService(IServiceProvider services, ILogger<PendingPaymentCleanupService> logger)
    {
        _services = services;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await CleanupAsync();
            }
            catch (OperationCanceledException)
            {
                break;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Pending payment cleanup failed.");
            }

            await Task.Delay(RunInterval, stoppingToken);
        }
    }

    private async Task CleanupAsync()
    {
        using var scope = _services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<SubchronDbContext>();
        var cutoff = DateTime.UtcNow - PendingMaxAge;

        var toRemove = await db.PaymentTransactions
            .Where(t => t.Status == "pending" && t.CreatedAt < cutoff)
            .ToListAsync();

        if (toRemove.Count == 0)
            return;

        db.PaymentTransactions.RemoveRange(toRemove);
        await db.SaveChangesAsync();
        _logger.LogInformation("Removed {Count} pending payment transaction(s) older than 15 minutes.", toRemove.Count);
    }
}
