using System.Text.Json;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    [Route("Account/SecurityCode")]
    public class SecurityCodeController : Controller
    {
        private readonly IAiApiClient _api;

        public SecurityCodeController(IAiApiClient api) => _api = api;

        [HttpPost("Send")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Send(CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token))
                return RedirectToAction("Login", "Account");

            var result = await _api.SendAsync(token, HttpMethod.Post, "api/Auth/security-code", null, ct);
            string? message = null;
            try
            {
                using var doc = JsonDocument.Parse(result.Body);
                foreach (var name in new[] { "message", "Message" })
                    if (doc.RootElement.TryGetProperty(name, out var m)) { message = m.GetString(); break; }
            }
            catch (JsonException) { }

            TempData[result.StatusCode is >= 200 and < 300 ? "StatusMessage" : "ErrorMessage"] =
                message ?? "We could not send a code. Please try again.";
            return RedirectToAction("Settings", "Account");
        }
    }
}
