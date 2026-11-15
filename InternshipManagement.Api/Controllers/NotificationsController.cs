using InternshipManagement.Application.DTOs.Notifications;
using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>
    /// The signed-in user's own notifications (students, companies and admins alike).
    ///   GET  api/Notifications?unreadOnly=false&amp;take=30
    ///   GET  api/Notifications/unread-count
    ///   POST api/Notifications/{id}/read
    ///   POST api/Notifications/read-all
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class NotificationsController : ControllerBase
    {
        private readonly INotificationService _notifications;

        public NotificationsController(INotificationService notifications)
        {
            _notifications = notifications;
        }

        [HttpGet]
        public async Task<ActionResult<List<NotificationResponse>>> Get(
            [FromQuery] bool unreadOnly = false, [FromQuery] int take = 30, CancellationToken cancellationToken = default)
        {
            return Ok(await _notifications.GetAsync(GetUserId(), unreadOnly, take, cancellationToken));
        }

        [HttpGet("unread-count")]
        public async Task<IActionResult> UnreadCount(CancellationToken cancellationToken)
        {
            return Ok(new { count = await _notifications.CountUnreadAsync(GetUserId(), cancellationToken) });
        }

        [HttpPost("{id:long}/read")]
        public async Task<IActionResult> MarkRead(long id, CancellationToken cancellationToken)
        {
            return await _notifications.MarkReadAsync(GetUserId(), id, cancellationToken)
                ? Ok(new { read = true })
                : NotFound(new { error = "Notification not found." });
        }

        [HttpPost("read-all")]
        public async Task<IActionResult> MarkAllRead(CancellationToken cancellationToken)
        {
            return Ok(new { marked = await _notifications.MarkAllReadAsync(GetUserId(), cancellationToken) });
        }

        private int GetUserId() => int.Parse(User.FindFirst("userId")?.Value ?? "0");
    }
}
