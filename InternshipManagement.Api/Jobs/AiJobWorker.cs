using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Api.Jobs
{
    /// <summary>
    /// Runs queued AI jobs outside the HTTP request, so a slow n8n/Gemini never causes a client timeout.
    /// A few consumers run in parallel (the limit also protects the AI quota). Each job gets its own DI scope.
    /// </summary>
    public sealed class AiJobWorker : BackgroundService
    {
        private const int Consumers = 2;
        private static readonly TimeSpan JobTimeout = TimeSpan.FromMinutes(4);
        private static readonly TimeSpan RetryDelay = TimeSpan.FromSeconds(3);
        private static readonly TimeSpan PurgeEvery = TimeSpan.FromMinutes(30);
        private static readonly TimeSpan KeepJobsFor = TimeSpan.FromHours(6);

        private static readonly JsonSerializerOptions ResultJsonOptions = new(JsonSerializerDefaults.Web);

        private readonly AiJobQueue _queue;
        private readonly IServiceScopeFactory _scopes;
        private readonly ILogger<AiJobWorker> _logger;

        public AiJobWorker(AiJobQueue queue, IServiceScopeFactory scopes, ILogger<AiJobWorker> logger)
        {
            _queue = queue;
            _scopes = scopes;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            await FailInterruptedJobsAsync();

            var tasks = new List<Task> { PurgeLoopAsync(stoppingToken) };
            for (var i = 0; i < Consumers; i++)
                tasks.Add(ConsumeAsync(stoppingToken));

            await Task.WhenAll(tasks);
        }

        // ---- consuming ---------------------------------------------------------------------

        private async Task ConsumeAsync(CancellationToken stoppingToken)
        {
            try
            {
                await foreach (var work in _queue.Reader.ReadAllAsync(stoppingToken))
                {
                    try
                    {
                        await ProcessAsync(work, stoppingToken);
                    }
                    catch (Exception ex)   // never let one job kill the consumer
                    {
                        _logger.LogError(ex, "AI job {JobId} crashed outside its handler.", work.JobId);
                    }
                }
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
        }

        private async Task ProcessAsync(AiJobWork work, CancellationToken stoppingToken)
        {
            using var scope = _scopes.CreateScope();
            var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();

            var job = await db.AiJobs.FirstOrDefaultAsync(
                j => j.Id == work.JobId && j.UserId == work.UserId, CancellationToken.None);

            if (job == null || job.Status != AiJobStatuses.Pending)
                return;   // gone, or already marked failed (stale / queue was full)

            job.Status = AiJobStatuses.Running;
            job.StartedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);

            using var timeout = CancellationTokenSource.CreateLinkedTokenSource(stoppingToken);
            timeout.CancelAfter(JobTimeout);

            try
            {
                object result;
                try
                {
                    result = await work.Run(scope.ServiceProvider, timeout.Token);
                }
                catch (N8nException ex) when (!timeout.IsCancellationRequested)
                {
                    // one retry for a transient AI/n8n failure (Gemini "high demand" 503, a dropped connection)
                    _logger.LogWarning("AI job {JobId} ({Type}) failed once: {Message}. Retrying.",
                        work.JobId, work.JobType, ex.Message);
                    await Task.Delay(RetryDelay, timeout.Token);
                    result = await work.Run(scope.ServiceProvider, timeout.Token);
                }

                job.Status = AiJobStatuses.Done;
                job.ResultJson = JsonSerializer.Serialize(result, result.GetType(), ResultJsonOptions);
                job.Error = null;
            }
            catch (Exception ex)
            {
                job.Status = AiJobStatuses.Failed;
                job.ResultJson = null;
                job.Error = FriendlyError(ex, stoppingToken, timeout.IsCancellationRequested);

                if (ex is not (InvalidOperationException or CvAnalysisRejectedException))
                    _logger.LogError(ex, "AI job {JobId} ({Type}) failed.", work.JobId, work.JobType);
            }

            job.CompletedAt = DateTime.UtcNow;
            job.UpdatedAt = DateTime.UtcNow;
            await db.SaveChangesAsync(CancellationToken.None);
        }

        private static string FriendlyError(Exception ex, CancellationToken stoppingToken, bool timedOut)
        {
            if (stoppingToken.IsCancellationRequested)
                return "The server was restarting. Please try again.";
            if (ex is OperationCanceledException || timedOut)
                return "The AI took too long to answer. Please try again.";
            if (ex is CvAnalysisRejectedException or InvalidOperationException)
                return ex.Message;   // our own user-facing messages
            if (ex is N8nException)
                return "The AI service is unavailable right now. Try again, or add your details manually.";
            return "Something went wrong. Please try again.";
        }

        // ---- housekeeping --------------------------------------------------------------------

        /// <summary>Jobs left Pending/Running by a previous run of the API can never finish: close them.</summary>
        private async Task FailInterruptedJobsAsync()
        {
            try
            {
                using var scope = _scopes.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                var now = DateTime.UtcNow;

                await db.AiJobs
                    .Where(j => j.Status == AiJobStatuses.Pending || j.Status == AiJobStatuses.Running)
                    .ExecuteUpdateAsync(s => s
                        .SetProperty(j => j.Status, AiJobStatuses.Failed)
                        .SetProperty(j => j.Error, "The server restarted while this was running. Please try again.")
                        .SetProperty(j => j.CompletedAt, now)
                        .SetProperty(j => j.UpdatedAt, now));
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not close interrupted AI jobs at startup.");
            }
        }

        private async Task PurgeLoopAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(PurgeEvery);
            try
            {
                do
                {
                    try
                    {
                        using var scope = _scopes.CreateScope();
                        var db = scope.ServiceProvider.GetRequiredService<IApplicationDbContext>();
                        var cutoff = DateTime.UtcNow - KeepJobsFor;
                        await db.AiJobs.Where(j => j.CreatedAt < cutoff).ExecuteDeleteAsync(stoppingToken);
                    }
                    catch (Exception ex) when (ex is not OperationCanceledException)
                    {
                        _logger.LogWarning(ex, "Purging old AI jobs failed.");
                    }
                }
                while (await timer.WaitForNextTickAsync(stoppingToken));
            }
            catch (OperationCanceledException)
            {
                // shutting down
            }
        }
    }
}
