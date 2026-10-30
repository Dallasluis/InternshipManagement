using System.Text.Json;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    /// <summary>Company: its own verification decisions and the reasons given.</summary>
    [Route("Company/VerificationHistory")]
    public class VerificationHistoryController : Controller
    {
        private readonly IAiApiClient _api;

        public VerificationHistoryController(IAiApiClient api) => _api = api;

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");
            if (HttpContext.Session.GetString("UserType") != "Company") return Forbid();

            var result = await _api.SendAsync(token, HttpMethod.Get, "api/Companies/verification/history", null, ct);
            var entries = new List<VerificationHistoryEntry>();
            if (result.StatusCode is >= 200 and < 300)
            {
                using var doc = JsonDocument.Parse(result.Body);
                foreach (var item in doc.RootElement.EnumerateArray())
                {
                    entries.Add(new VerificationHistoryEntry
                    {
                        Status = Str(item, "status"),
                        ReasonCode = Str(item, "reasonCode"),
                        Notes = Str(item, "notes"),
                        IsAppeal = item.TryGetProperty("isAppeal", out var a) && a.ValueKind == JsonValueKind.True,
                        CreatedAt = DateTimeOffset.TryParse(Str(item, "createdAt"), out var d) ? d : null
                    });
                }
            }
            else
            {
                ViewBag.Error = "Your verification history could not be loaded.";
            }

            return View(entries.OrderByDescending(e => e.CreatedAt).ToList());
        }

        private static string? Str(JsonElement e, string camel)
        {
            if (!e.TryGetProperty(camel, out var v) && !e.TryGetProperty(char.ToUpperInvariant(camel[0]) + camel[1..], out v))
                return null;
            return v.ValueKind == JsonValueKind.Null ? null : v.ToString();
        }
    }

    public class VerificationHistoryEntry
    {
        public string? Status { get; set; }
        public string? ReasonCode { get; set; }
        public string? Notes { get; set; }
        public bool IsAppeal { get; set; }
        public DateTimeOffset? CreatedAt { get; set; }
    }
}
