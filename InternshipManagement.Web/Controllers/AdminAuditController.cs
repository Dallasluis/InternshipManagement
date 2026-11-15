using System.Text.Json;
using InternshipManagement.Web.Models.Admin;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    /// <summary>Administrators read the audit log of administrator and company actions.</summary>
    [Route("Admin")]
    public class AdminAuditController : Controller
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly IAiApiClient _api;

        public AdminAuditController(IAiApiClient api)
        {
            _api = api;
        }

        [HttpGet("AuditLog")]
        public async Task<IActionResult> Index(
            string? actionName, string? role, string? user, DateTime? from, DateTime? to, int page = 1, CancellationToken ct = default)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");
            if (HttpContext.Session.GetString("UserType") != "Admin") return Forbid();

            var query = new List<string> { $"page={Math.Max(page, 1)}", "pageSize=50" };
            if (!string.IsNullOrWhiteSpace(actionName)) query.Add("actionName=" + Uri.EscapeDataString(actionName.Trim()));
            if (!string.IsNullOrWhiteSpace(role)) query.Add("role=" + Uri.EscapeDataString(role.Trim()));
            if (!string.IsNullOrWhiteSpace(user)) query.Add("user=" + Uri.EscapeDataString(user.Trim()));
            if (from.HasValue) query.Add("from=" + from.Value.ToString("yyyy-MM-dd"));
            if (to.HasValue) query.Add("to=" + to.Value.ToString("yyyy-MM-dd"));

            var model = new AuditLogPage { Page = Math.Max(page, 1) };
            var result = await _api.SendAsync(token, HttpMethod.Get, "api/admin/audit-logs?" + string.Join('&', query), null, ct);
            if (result.StatusCode == StatusCodes.Status200OK)
            {
                try { model = JsonSerializer.Deserialize<AuditLogPage>(result.Body, JsonOptions) ?? model; }
                catch (JsonException) { ViewBag.Error = "The log could not be read."; }
            }
            else
            {
                ViewBag.Error = "The audit log could not be loaded right now.";
            }

            ViewBag.Action = actionName; ViewBag.Role = role; ViewBag.User = user; ViewBag.From = from; ViewBag.To = to;
            return View(model);
        }
    }
}
