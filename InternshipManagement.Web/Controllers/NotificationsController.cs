using System.Text.Json;
using InternshipManagement.Web.Models.Notifications;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    /// <summary>The signed-in user's notifications: the list page, the bell's unread count, and mark-as-read.</summary>
    public class NotificationsController : Controller
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

        private readonly IAiApiClient _api;

        public NotificationsController(IAiApiClient api)
        {
            _api = api;
        }

        [HttpGet]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var token = GetToken();
            if (token == null)
                return RedirectToAction("Login", "Account");

            var items = new List<NotificationItem>();
            var result = await _api.SendAsync(token, HttpMethod.Get, "api/Notifications?take=50", null, ct);
            if (result.StatusCode == StatusCodes.Status200OK)
            {
                try { items = JsonSerializer.Deserialize<List<NotificationItem>>(result.Body, JsonOptions) ?? items; }
                catch (JsonException) { ViewBag.Error = "Your notifications could not be read. Please try again."; }
            }
            else if (result.StatusCode == StatusCodes.Status401Unauthorized)
            {
                return RedirectToAction("Login", "Account");
            }
            else
            {
                ViewBag.Error = "Your notifications could not be loaded right now. Please try again.";
            }

            return View(items);
        }

        // The bell asks for this every minute.
        [HttpGet]
        public async Task<IActionResult> Count(CancellationToken ct)
        {
            var token = GetToken();
            if (token == null)
                return Unauthorized(new { count = 0 });

            var result = await _api.SendAsync(token, HttpMethod.Get, "api/Notifications/unread-count", null, ct);
            return new ContentResult
            {
                StatusCode = result.StatusCode,
                Content = string.IsNullOrWhiteSpace(result.Body) ? "{\"count\":0}" : result.Body,
                ContentType = "application/json; charset=utf-8"
            };
        }

        [HttpGet]
        public async Task<IActionResult> Preview(CancellationToken ct)
        {
            var token = GetToken();
            if (token == null)
                return Unauthorized(new { message = "Please sign in again to view notifications." });

            var result = await _api.SendAsync(token, HttpMethod.Get, "api/Notifications?take=6", null, ct);
            if (result.StatusCode == StatusCodes.Status401Unauthorized)
                return Unauthorized(new { message = "Please sign in again to view notifications." });
            if (result.StatusCode != StatusCodes.Status200OK)
                return StatusCode(StatusCodes.Status502BadGateway, new { message = "Notifications could not be loaded. Please try again." });

            try
            {
                var items = JsonSerializer.Deserialize<List<NotificationItem>>(result.Body, JsonOptions) ?? new List<NotificationItem>();
                return Json(items.Select(item => new
                {
                    item.Id,
                    item.Title,
                    item.Message,
                    item.LinkPath,
                    item.IsRead,
                    item.CreatedAt
                }));
            }
            catch (JsonException)
            {
                return StatusCode(StatusCodes.Status502BadGateway, new { message = "Notifications could not be read. Please try again." });
            }
        }

        // Marks one notification as read, then goes to the page it points to.
        [HttpGet]
        public async Task<IActionResult> Open(long id, string? to, CancellationToken ct)
        {
            var token = GetToken();
            if (token == null)
                return RedirectToAction("Login", "Account");

            if (id > 0)
                await _api.SendAsync(token, HttpMethod.Post, $"api/Notifications/{id}/read", null, ct);

            return !string.IsNullOrWhiteSpace(to) && Url.IsLocalUrl(to)
                ? LocalRedirect(to)
                : RedirectToAction(nameof(Index));
        }

        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkAllRead(CancellationToken ct)
        {
            var token = GetToken();
            if (token == null)
                return RedirectToAction("Login", "Account");

            await _api.SendAsync(token, HttpMethod.Post, "api/Notifications/read-all", null, ct);
            return RedirectToAction(nameof(Index));
        }

        private string? GetToken()
        {
            var token = HttpContext.Session.GetString("JwtToken");
            return string.IsNullOrEmpty(token) ? null : token;
        }
    }
}
