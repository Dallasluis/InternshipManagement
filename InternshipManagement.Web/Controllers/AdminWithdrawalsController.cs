using System.Net.Http.Json;
using System.Text.Json;
using InternshipManagement.Web.Helpers;
using InternshipManagement.Web.Models.Admin;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    /// <summary>Administrators decide withdrawals that an intern disputed.</summary>
    [Route("Admin")]
    public class AdminWithdrawalsController : Controller
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly IAiApiClient _api;

        public AdminWithdrawalsController(IAiApiClient api)
        {
            _api = api;
        }

        [HttpGet("Withdrawals")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");
            if (!IsAdmin) return Forbid();

            var items = new List<DisputedWithdrawalItem>();
            var result = await _api.SendAsync(token, HttpMethod.Get, "api/admin/withdrawals/disputed", null, ct);
            if (result.StatusCode == StatusCodes.Status200OK)
            {
                try { items = JsonSerializer.Deserialize<List<DisputedWithdrawalItem>>(result.Body, JsonOptions) ?? items; }
                catch (JsonException) { ViewBag.Error = "The list could not be read."; }
            }
            else
            {
                ViewBag.Error = "The disputed withdrawals could not be loaded right now.";
            }

            return View(items);
        }

        [HttpPost("Withdrawals/{id:int}/Resolve")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolve(int id, string decision, string? notes, CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");
            if (!IsAdmin) return Forbid();

            var approve = string.Equals(decision, "approve", StringComparison.OrdinalIgnoreCase);
            var result = await _api.SendAsync(token, HttpMethod.Post, $"api/admin/withdrawals/{id}/resolve",
                JsonContent.Create(new { Approved = approve, Notes = notes }), ct);

            if (result.StatusCode is >= 200 and < 300)
                TempData["SuccessMessage"] = approve
                    ? "Withdrawal approved. The placement has ended and both sides were told."
                    : "The company's decision was kept. Both sides were told.";
            else
                TempData["ErrorMessage"] = ApiErrorReader.Read(result.Body, "The decision could not be saved.");

            return RedirectToAction(nameof(Index));
        }

        private bool IsAdmin => HttpContext.Session.GetString("UserType") == "Admin";
    }
}
