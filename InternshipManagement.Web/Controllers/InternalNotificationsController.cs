using System.Security.Cryptography;
using System.Text;
using InternshipManagement.Web.Hubs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;

namespace InternshipManagement.Web.Controllers
{
    /// <summary>
    /// Called by the API (not by browsers) when a notification is created, so it can be pushed to that user's open
    /// tabs. Protected by a shared key; with no key configured the endpoint does not exist.
    /// </summary>
    [ApiController]
    [Route("internal/notifications")]
    [IgnoreAntiforgeryToken]
    public sealed class InternalNotificationsController : ControllerBase
    {
        private readonly IHubContext<NotificationHub> _hub;
        private readonly IConfiguration _configuration;

        public InternalNotificationsController(IHubContext<NotificationHub> hub, IConfiguration configuration)
        {
            _hub = hub;
            _configuration = configuration;
        }

        [HttpPost("push")]
        public async Task<IActionResult> Push([FromBody] PushPayload payload, CancellationToken ct)
        {
            var expected = _configuration["Notifications:PushKey"];
            if (string.IsNullOrWhiteSpace(expected))
                return NotFound();

            var supplied = Request.Headers["X-Push-Key"].ToString();
            if (!CryptographicOperations.FixedTimeEquals(Encoding.UTF8.GetBytes(supplied), Encoding.UTF8.GetBytes(expected)))
                return Unauthorized();

            if (payload.UserId <= 0)
                return BadRequest();

            await _hub.Clients.User(payload.UserId.ToString()).SendAsync("notification", new
            {
                payload.Id,
                payload.Type,
                payload.Title,
                payload.Message,
                payload.LinkPath,
                payload.CreatedAt,
                payload.UnreadCount
            }, ct);

            return NoContent();
        }

        public sealed class PushPayload
        {
            public int UserId { get; set; }
            public long Id { get; set; }
            public string Type { get; set; } = string.Empty;
            public string Title { get; set; } = string.Empty;
            public string Message { get; set; } = string.Empty;
            public string? LinkPath { get; set; }
            public DateTime CreatedAt { get; set; }
            public int UnreadCount { get; set; }
        }
    }
}
