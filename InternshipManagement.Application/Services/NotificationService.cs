using InternshipManagement.Application.DTOs.Notifications;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace InternshipManagement.Application.Services
{
    public class NotificationService : INotificationService
    {
        private const int MaxTake = 100;

        private readonly IApplicationDbContext _context;
        private readonly IIdentityService _identity;
        private readonly IEmailSender _email;
        private readonly ILogger<NotificationService> _logger;
        private readonly INotificationPublisher? _publisher;

        public NotificationService(
            IApplicationDbContext context,
            IIdentityService identity,
            IEmailSender email,
            ILogger<NotificationService> logger,
            INotificationPublisher? publisher = null)
        {
            _publisher = publisher;
            _context = context;
            _identity = identity;
            _email = email;
            _logger = logger;
        }

        public async Task NotifyAsync(
            int userId, string type, string title, string message,
            string? linkPath = null, bool sendEmail = false, CancellationToken cancellationToken = default)
        {
            if (userId <= 0) return;

            var notification = new Notification
            {
                UserId = userId,
                Type = Trim(type, 50),
                Title = Trim(title, 200),
                Message = Trim(message, 1000),
                LinkPath = IsSafePath(linkPath) ? linkPath : null,
                CreatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Notifications.Add(notification);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Could not save a notification for user {UserId}.", userId);
                try { _context.Notifications.Remove(notification); } catch { /* nothing more to do */ }
                return;
            }

            await TryPushAsync(userId, notification, cancellationToken);

            if (sendEmail)
                await TrySendEmailAsync(userId, notification, cancellationToken);
        }

        public async Task<List<NotificationResponse>> GetAsync(
            int userId, bool unreadOnly, int take, CancellationToken cancellationToken = default)
        {
            take = Math.Clamp(take, 1, MaxTake);

            var query = _context.Notifications.AsNoTracking().Where(n => n.UserId == userId);
            if (unreadOnly)
                query = query.Where(n => !n.IsRead);

            return await query
                .OrderByDescending(n => n.CreatedAt)
                .ThenByDescending(n => n.Id)
                .Take(take)
                .Select(n => new NotificationResponse
                {
                    Id = n.Id,
                    Type = n.Type,
                    Title = n.Title,
                    Message = n.Message,
                    LinkPath = n.LinkPath,
                    IsRead = n.IsRead,
                    CreatedAt = n.CreatedAt
                })
                .ToListAsync(cancellationToken);
        }

        public Task<int> CountUnreadAsync(int userId, CancellationToken cancellationToken = default) =>
            _context.Notifications.CountAsync(n => n.UserId == userId && !n.IsRead, cancellationToken);

        public async Task<bool> MarkReadAsync(int userId, long id, CancellationToken cancellationToken = default)
        {
            var notification = await _context.Notifications
                .FirstOrDefaultAsync(n => n.Id == id && n.UserId == userId, cancellationToken);
            if (notification == null) return false;

            if (!notification.IsRead)
            {
                notification.IsRead = true;
                notification.ReadAt = DateTime.UtcNow;
                await _context.SaveChangesAsync(cancellationToken);
            }

            return true;
        }

        public async Task<int> MarkAllReadAsync(int userId, CancellationToken cancellationToken = default)
        {
            var unread = await _context.Notifications
                .Where(n => n.UserId == userId && !n.IsRead)
                .ToListAsync(cancellationToken);

            var now = DateTime.UtcNow;
            foreach (var notification in unread)
            {
                notification.IsRead = true;
                notification.ReadAt = now;
            }

            if (unread.Count > 0)
                await _context.SaveChangesAsync(cancellationToken);

            return unread.Count;
        }

        // ------------------------------------------------------------------ helpers

        // live push to open browser tabs; the bell still catches up by polling if this fails
        private async Task TryPushAsync(int userId, Notification notification, CancellationToken cancellationToken)
        {
            if (_publisher == null) return;

            try
            {
                var unread = await CountUnreadAsync(userId, cancellationToken);
                _publisher.Publish(userId, new NotificationResponse
                {
                    Id = notification.Id,
                    Type = notification.Type,
                    Title = notification.Title,
                    Message = notification.Message,
                    LinkPath = notification.LinkPath,
                    IsRead = false,
                    CreatedAt = notification.CreatedAt
                }, unread);
            }
            catch (Exception ex)
            {
                _logger.LogDebug(ex, "Could not push a notification to user {UserId}.", userId);
            }
        }

        private async Task TrySendEmailAsync(int userId, Notification notification, CancellationToken cancellationToken)
        {
            try
            {
                var user = await _identity.FindUserByIdAsync(userId.ToString());
                if (user == null || !user.IsActive || !user.EmailConfirmed || string.IsNullOrWhiteSpace(user.Email))
                    return;

                var body = $"Hello {user.FirstName},\n\n{notification.Message}\n\n" +
                           "Sign in to the Internship Management Platform to see the details.\n";
                await _email.SendEmailAsync(user.Email, notification.Title, body, cancellationToken);
            }
            catch (Exception ex)
            {
                // the notification is already saved; a mail problem must not matter to the caller
                _logger.LogWarning(ex, "Could not email a notification to user {UserId}.", userId);
            }
        }

        private static string Trim(string? value, int max)
        {
            value = (value ?? string.Empty).Trim();
            return value.Length <= max ? value : value[..max];
        }

        // only paths inside the website, so a notification can never point somewhere else
        private static bool IsSafePath(string? path) =>
            !string.IsNullOrWhiteSpace(path)
            && path.Length <= 300
            && path.StartsWith('/')
            && !path.StartsWith("//")
            && !path.Contains('\\')
            && !path.Contains("://");
    }
}
