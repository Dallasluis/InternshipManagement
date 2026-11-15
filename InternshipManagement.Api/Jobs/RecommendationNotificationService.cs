using InternshipManagement.Application.Interfaces;

namespace InternshipManagement.Api.Jobs
{
    /// <summary>Every few minutes, tells students about newly published internships that fit their profile.</summary>
    public sealed class RecommendationNotificationService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RecommendationNotificationService> _logger;

        public RecommendationNotificationService(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            ILogger<RecommendationNotificationService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_configuration.GetValue("Notifications:Recommendations:Enabled", true))
                return;

            var minutes = Math.Clamp(_configuration.GetValue("Notifications:Recommendations:IntervalMinutes", 15), 1, 1440);

            try
            {
                // give the API a minute to finish starting (migrations, seeding)
                await Task.Delay(TimeSpan.FromSeconds(60), stoppingToken);
                await RunOnceAsync(stoppingToken);

                using var timer = new PeriodicTimer(TimeSpan.FromMinutes(minutes));
                while (await timer.WaitForNextTickAsync(stoppingToken))
                    await RunOnceAsync(stoppingToken);
            }
            catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested) { }
        }

        private async Task RunOnceAsync(CancellationToken ct)
        {
            try
            {
                await using var scope = _scopeFactory.CreateAsyncScope();
                var notifier = scope.ServiceProvider.GetRequiredService<IRecommendationNotifier>();
                await notifier.NotifyNewMatchesAsync(ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "New-match notifications failed.");
            }
        }
    }
}
