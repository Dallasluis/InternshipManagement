using System.Net.Http.Json;
using InternshipManagement.Application.DTOs.Notifications;
using InternshipManagement.Application.Interfaces;

namespace InternshipManagement.Api.Services
{
    /// <summary>
    /// Tells the website a notification was created so it can show it at once. It sends one small request in the
    /// background and never waits for it. With no URL configured nothing is sent and the bell simply polls.
    /// </summary>
    public sealed class HttpNotificationPublisher : INotificationPublisher
    {
        private readonly IHttpClientFactory _factory;
        private readonly IConfiguration _configuration;
        private readonly ILogger<HttpNotificationPublisher> _logger;
        private long _lastWarningTicks;

        public HttpNotificationPublisher(IHttpClientFactory factory, IConfiguration configuration, ILogger<HttpNotificationPublisher> logger)
        {
            _factory = factory;
            _configuration = configuration;
            _logger = logger;
        }

        public void Publish(int userId, NotificationResponse notification, int unreadCount)
        {
            var url = _configuration["Notifications:Push:Url"];
            var key = _configuration["Notifications:Push:Key"];
            if (string.IsNullOrWhiteSpace(url) || string.IsNullOrWhiteSpace(key))
                return;

            var payload = new
            {
                userId,
                id = notification.Id,
                type = notification.Type,
                title = notification.Title,
                message = notification.Message,
                linkPath = notification.LinkPath,
                createdAt = notification.CreatedAt,
                unreadCount
            };

            _ = Task.Run(async () =>
            {
                try
                {
                    using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(3));
                    using var request = new HttpRequestMessage(HttpMethod.Post, url) { Content = JsonContent.Create(payload) };
                    request.Headers.Add("X-Push-Key", key);

                    using var response = await _factory.CreateClient("NotificationPush").SendAsync(request, cts.Token);
                    if (!response.IsSuccessStatusCode)
                        Warn($"the website answered {(int)response.StatusCode}");
                }
                catch (Exception ex)
                {
                    Warn(ex.Message);
                }
            });
        }

        // at most one warning every 5 minutes, so a website that is down does not flood the log
        private void Warn(string reason)
        {
            var now = DateTime.UtcNow.Ticks;
            var last = Interlocked.Read(ref _lastWarningTicks);
            if (now - last < TimeSpan.FromMinutes(5).Ticks) return;
            if (Interlocked.CompareExchange(ref _lastWarningTicks, now, last) != last) return;

            _logger.LogWarning("Live notification push failed ({Reason}). The bell will update by polling instead.", reason);
        }
    }
}
