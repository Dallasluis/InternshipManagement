using System.Text.Json;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    /// <summary>Admin: verification turnaround (SLA) report and the re-verification run.</summary>
    [Route("Admin/Verification")]
    public class VerificationAdminController : Controller
    {
        private readonly IAiApiClient _api;

        public VerificationAdminController(IAiApiClient api) => _api = api;

        private bool IsAdmin => HttpContext.Session.GetString("UserType") == "Admin";

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");
            if (!IsAdmin) return Forbid();

            var result = await _api.SendAsync(token, HttpMethod.Get, "api/Admin/companies/verification/sla", null, ct);
            if (result.StatusCode is < 200 or >= 300)
            {
                ViewBag.Error = "The verification report could not be loaded.";
                return View(new VerificationSlaViewModel());
            }

            using var doc = JsonDocument.Parse(result.Body);
            var root = doc.RootElement;
            return View(new VerificationSlaViewModel
            {
                Loaded = true,
                DecisionsMeasured = Read(root, "decisionsMeasured").GetInt32(),
                DecisionsWithinThreeBusinessDays = Read(root, "decisionsWithinThreeBusinessDays").GetInt32(),
                WithinTargetPercentage = Read(root, "withinTargetPercentage").GetDouble(),
                TargetPercentage = Read(root, "targetPercentage").GetDouble(),
                OverdueAppeals = root.TryGetProperty("overdueAppeals", out var overdue) && overdue.ValueKind == JsonValueKind.Array
                    ? overdue.EnumerateArray().Select(item => new OverdueAppealRow
                    {
                        CompanyId = Read(item, "companyId").GetInt32(),
                        CompanyName = Read(item, "companyName").GetString() ?? "-",
                        SubmittedAt = Read(item, "submittedAt").GetDateTime(),
                        BusinessDaysWaiting = Read(item, "businessDaysWaiting").GetInt32()
                    }).ToList()
                    : new List<OverdueAppealRow>()
            });
        }

        [HttpPost("Reverify")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reverify(CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");
            if (!IsAdmin) return Forbid();

            var result = await _api.SendAsync(token, HttpMethod.Post, "api/Admin/companies/verification/reverification", null, ct);
            if (result.StatusCode is >= 200 and < 300)
            {
                var count = 0;
                try { using var doc = JsonDocument.Parse(result.Body); count = Read(doc.RootElement, "updatedCount").GetInt32(); } catch { }
                TempData["StatusMessage"] = count == 0
                    ? "No companies needed re-verification."
                    : $"{count} compan{(count == 1 ? "y was" : "ies were")} marked as needing re-verification.";
            }
            else
            {
                TempData["ErrorMessage"] = "The re-verification run failed. Please try again.";
            }

            return RedirectToAction(nameof(Index));
        }

        private static JsonElement Read(JsonElement root, string camel)
        {
            if (root.TryGetProperty(camel, out var value)) return value;
            return root.GetProperty(char.ToUpperInvariant(camel[0]) + camel[1..]);
        }
    }

    public class VerificationSlaViewModel
    {
        public bool Loaded { get; set; }
        public int DecisionsMeasured { get; set; }
        public int DecisionsWithinThreeBusinessDays { get; set; }
        public double WithinTargetPercentage { get; set; }
        public double TargetPercentage { get; set; }
        public bool MeetsTarget => WithinTargetPercentage >= TargetPercentage;
        public List<OverdueAppealRow> OverdueAppeals { get; set; } = new();
    }

    public class OverdueAppealRow
    {
        public int CompanyId { get; set; }
        public string CompanyName { get; set; } = string.Empty;
        public DateTime SubmittedAt { get; set; }
        public int BusinessDaysWaiting { get; set; }
    }
}
