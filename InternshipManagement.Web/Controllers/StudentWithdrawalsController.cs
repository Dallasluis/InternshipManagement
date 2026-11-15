using System.Net.Http.Json;
using InternshipManagement.Web.Helpers;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    /// <summary>An intern disputes a company's refusal of their withdrawal; an administrator then decides.</summary>
    [Route("Student")]
    public class StudentWithdrawalsController : Controller
    {
        private readonly IAiApiClient _api;

        public StudentWithdrawalsController(IAiApiClient api)
        {
            _api = api;
        }

        [HttpPost("DisputeWithdrawal/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Dispute(int id, int placementId, string? reason, CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");

            var result = await _api.SendAsync(token, HttpMethod.Post, $"api/lifecycle/withdrawals/{id}/dispute",
                JsonContent.Create(new { Reason = reason ?? string.Empty }), ct);

            if (result.StatusCode is >= 200 and < 300)
                TempData["SuccessMessage"] = "Your dispute was sent. An administrator will review it.";
            else
                TempData["ErrorMessage"] = ApiErrorReader.Read(result.Body, "Your dispute could not be sent.");

            return RedirectToAction("PlacementDetails", "Student", new { id = placementId });
        }
    }
}
