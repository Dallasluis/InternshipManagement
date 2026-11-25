using InternshipManagement.Api.Jobs;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Enums;
using InternshipManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Options;

namespace InternshipManagement.Api.Jobs;

/// <summary>Purges files only after an explicit, configured retention period.</summary>
public sealed class DeletedDocumentPurgeService : BackgroundService
{
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IOptions<DeletedDocumentPurgeOptions> _options;
    private readonly ILogger<DeletedDocumentPurgeService> _logger;

    public DeletedDocumentPurgeService(
        IServiceScopeFactory scopeFactory,
        IOptions<DeletedDocumentPurgeOptions> options,
        ILogger<DeletedDocumentPurgeService> logger)
    {
        _scopeFactory = scopeFactory;
        _options = options;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(TimeSpan.FromHours(24));
        while (await timer.WaitForNextTickAsync(stoppingToken))
        {
            if (_options.Value.AfterDays is not > 0)
                continue;

            try
            {
                await PurgeEligibleDocumentsAsync(_options.Value.AfterDays.Value, stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
            {
                break;
            }
            catch (Exception exception)
            {
                _logger.LogError(exception, "Deleted document retention purge failed.");
            }
        }
    }

    internal async Task<int> PurgeEligibleDocumentsAsync(int afterDays, CancellationToken cancellationToken)
    {
        if (afterDays <= 0)
            throw new ArgumentOutOfRangeException(nameof(afterDays), "Purge retention must be a positive number of days.");

        await using var scope = _scopeFactory.CreateAsyncScope();
        var context = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
        var storage = scope.ServiceProvider.GetRequiredService<IPrivateDocumentStorage>();
        var cutoff = DateTimeOffset.UtcNow.AddDays(-afterDays);
        var eligible = await context.PrivateDocuments
            .Where(document => document.RetentionState == DocumentRetentionState.Deleted
                && document.ClosedAt != null
                && document.ClosedAt <= cutoff)
            .OrderBy(document => document.ClosedAt)
            .Take(100)
            .ToListAsync(cancellationToken);

        foreach (var document in eligible)
        {
            // Storage deletion is idempotent. If database cleanup fails, the next run retries safely.
            await storage.DeleteAsync(document.Id, cancellationToken);
            await context.DocumentAccessTokens
                .Where(token => token.DocumentId == document.Id)
                .ExecuteDeleteAsync(cancellationToken);
            context.PrivateDocuments.Remove(document);
        }

        if (eligible.Count > 0)
            await context.SaveChangesAsync(cancellationToken);

        if (eligible.Count > 0)
            _logger.LogInformation("Purged {Count} documents past their configured deletion retention period.", eligible.Count);

        return eligible.Count;
    }
}
