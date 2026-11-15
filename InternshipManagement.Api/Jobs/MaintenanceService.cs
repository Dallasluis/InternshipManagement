using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;
using InternshipManagement.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Api.Jobs
{
    /// <summary>
    /// Housekeeping that runs a few times a day: marks offers that passed their expiry date as expired (and tells
    /// both sides), and deletes old notifications and used-up tokens so the database does not grow forever.
    /// </summary>
    public sealed class MaintenanceService : BackgroundService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<MaintenanceService> _logger;

        public MaintenanceService(IServiceScopeFactory scopeFactory, IConfiguration configuration, ILogger<MaintenanceService> logger)
        {
            _scopeFactory = scopeFactory;
            _configuration = configuration;
            _logger = logger;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            if (!_configuration.GetValue("Maintenance:Enabled", true))
                return;

            try
            {
                await Task.Delay(TimeSpan.FromMinutes(2), stoppingToken); // let the API finish starting first
                await RunOnceAsync(stoppingToken);

                using var timer = new PeriodicTimer(TimeSpan.FromHours(6));
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
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
                var notifications = scope.ServiceProvider.GetRequiredService<INotificationService>();

                await ExpireOffersAsync(db, notifications, ct);
                await DeleteOldRecordsAsync(db, ct);
            }
            catch (OperationCanceledException) when (ct.IsCancellationRequested) { }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Maintenance run failed.");
            }
        }

        private async Task ExpireOffersAsync(ApplicationDbContext db, INotificationService notifications, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var today = now.Date;

            var overdue = await db.InternshipApplications
                .Include(a => a.Internship).ThenInclude(i => i.CompanyProfile)
                .Include(a => a.StudentProfile)
                .Where(a => a.Status == ApplicationStatus.OfferMade
                    && a.OfferExpiryDate != null
                    && a.OfferExpiryDate.Value.Date < today)
                .OrderBy(a => a.OfferExpiryDate)
                .Take(200)
                .ToListAsync(ct);

            if (overdue.Count == 0) return;

            foreach (var application in overdue)
            {
                application.Status = ApplicationStatus.OfferExpired;
                application.StatusUpdatedAt = now;
                db.ApplicationStatusHistories.Add(new ApplicationStatusHistory
                {
                    InternshipApplicationId = application.Id,
                    PreviousStatus = ApplicationStatus.OfferMade,
                    NewStatus = ApplicationStatus.OfferExpired,
                    ChangedAt = now,
                    ChangedByUserId = null,
                    Notes = $"The offer expired on {application.OfferExpiryDate!.Value:d MMM yyyy} without a reply."
                });
            }

            await db.SaveChangesAsync(ct);

            foreach (var application in overdue)
            {
                var title = application.Internship.Title;
                await notifications.NotifyAsync(application.StudentProfile.UserId, NotificationTypes.Offer,
                    "Your offer expired",
                    $"The offer for \"{title}\" expired on {application.OfferExpiryDate:d MMM yyyy} because it was not answered in time. Contact the company if you are still interested.",
                    $"/Student/ApplicationDetails/{application.Id}", sendEmail: true, cancellationToken: ct);

                await notifications.NotifyAsync(application.Internship.CompanyProfile.UserId, NotificationTypes.Offer,
                    "An offer expired",
                    $"Your offer to {application.StudentProfile.FirstName} {application.StudentProfile.LastName} for \"{title}\" expired without a reply.",
                    $"/Company/ApplicationReview/{application.Id}", sendEmail: false, cancellationToken: ct);
            }

            _logger.LogInformation("Marked {Count} unanswered offers as expired.", overdue.Count);
        }

        private async Task DeleteOldRecordsAsync(ApplicationDbContext db, CancellationToken ct)
        {
            var now = DateTime.UtcNow;
            var readDays = Math.Clamp(_configuration.GetValue("Maintenance:ReadNotificationDays", 90), 7, 3650);
            var anyDays = Math.Clamp(_configuration.GetValue("Maintenance:AnyNotificationDays", 180), readDays, 3650);
            var readCutoff = now.AddDays(-readDays);
            var anyCutoff = now.AddDays(-anyDays);

            var notifications = await db.Notifications
                .Where(n => (n.IsRead && n.ReadAt != null && n.ReadAt < readCutoff) || n.CreatedAt < anyCutoff)
                .ExecuteDeleteAsync(ct);

            var tokenCutoff = DateTimeOffset.UtcNow.AddDays(-7);
            var emailTokens = await db.EmailActionTokens.Where(t => t.ExpiresAt < tokenCutoff).ExecuteDeleteAsync(ct);
            var documentTokens = await db.DocumentAccessTokens.Where(t => t.ExpiresAt < tokenCutoff).ExecuteDeleteAsync(ct);

            if (notifications + emailTokens + documentTokens > 0)
                _logger.LogInformation("Cleanup removed {Notifications} notifications, {EmailTokens} email tokens and {DocumentTokens} document tokens.",
                    notifications, emailTokens, documentTokens);
        }
    }
}
