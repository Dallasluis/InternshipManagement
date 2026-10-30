using InternshipManagement.Application.Interfaces;

namespace InternshipManagement.Api.Jobs
{
    public sealed class EmailVerificationHousekeepingService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<EmailVerificationHousekeepingService> _logger;

        public EmailVerificationHousekeepingService(
            IServiceScopeFactory scopeFactory,
            ILogger<EmailVerificationHousekeepingService> logger)
        {
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            using var timer = new PeriodicTimer(TimeSpan.FromHours(1));
            while (await timer.WaitForNextTickAsync(stoppingToken))
            {
                try
                {
                    await using var scope = _scopeFactory.CreateAsyncScope();
                    var authService = scope.ServiceProvider.GetRequiredService<IAuthService>();
                    await authService.RunEmailVerificationHousekeepingAsync(stoppingToken);
                    var companyService = scope.ServiceProvider.GetRequiredService<ICompanyService>();
                    var reverificationCount = await companyService.UpdateAgedVerificationsAsync();
                    if (reverificationCount > 0)
                        _logger.LogInformation("Marked {Count} companies as requiring re-verification.", reverificationCount);
                }
                catch (OperationCanceledException) when (stoppingToken.IsCancellationRequested)
                {
                    break;
                }
                catch (Exception exception)
                {
                    _logger.LogError(exception, "Email verification housekeeping failed.");
                }
            }
        }
    }
}
