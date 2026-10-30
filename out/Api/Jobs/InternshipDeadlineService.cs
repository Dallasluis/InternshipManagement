using InternshipManagement.Domain.Enums;
using InternshipManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Api.Jobs
{
    /// <summary>Closes published internships past their deadline and activates placements that reach their start date.</summary>
    public sealed class InternshipDeadlineService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<InternshipDeadlineService> _logger;

        public InternshipDeadlineService(IServiceScopeFactory scopeFactory, ILogger<InternshipDeadlineService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await RunOnceAsync(stoppingToken);
            using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
            while (await timer.WaitForNextTickAsync(stoppingToken))
                await RunOnceAsync(stoppingToken);
        }

        private async Task RunOnceAsync(CancellationToken ct)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var context = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var now = DateTime.UtcNow;
                var closed = await context.Internships
                    .Where(i => i.Status == InternshipStatus.Published && i.ApplicationDeadline < now)
                    .ExecuteUpdateAsync(s => s.SetProperty(i => i.Status, InternshipStatus.Closed), ct);
                if (closed > 0)
                    _logger.LogInformation("Closed {Count} internships past their deadline.", closed);

                // Placements created before their start date become Active when the start date arrives.
                var activated = await context.Placements
                    .Where(p => p.Status == PlacementStatus.Pending && p.CurrentStartDate <= now)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(p => p.Status, PlacementStatus.Active)
                        .SetProperty(p => p.ActivatedAt, now), ct);
                if (activated > 0)
                    _logger.LogInformation("Activated {Count} placements that reached their start date.", activated);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Closing expired internships failed.");
            }
        }
    }
}
