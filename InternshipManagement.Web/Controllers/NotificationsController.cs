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
