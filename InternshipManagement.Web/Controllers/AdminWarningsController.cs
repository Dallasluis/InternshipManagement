using System.Net.Http.Json;
using InternshipManagement.Web.Helpers;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    /// <summary>Sends a formal warning to a company from the administrator's company pages.</summary>
    public class AdminWarningsController : Controller
    {
        private readonly IAiApiClient _api;

        public AdminWarningsController(IAiApiClient api)
        {
            _api = api;
        }

        [HttpPost("Admin/WarnCompany")]
        public async Task<IActionResult> Warn(int companyId, string? message, CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return Json(new { success = false, message = "Not authenticated" });
            if (HttpContext.Session.GetString("UserType") != "Admin") return Json(new { success = false, message = "Not allowed" });

            var result = await _api.SendAsync(token, HttpMethod.Post, $"api/admin/companies/{companyId}/warn",
                JsonContent.Create(new { Message = message ?? string.Empty }), ct);

            return result.StatusCode is >= 200 and < 300
                ? Json(new { success = true })
                : Json(new { success = false, message = ApiErrorReader.Read(result.Body, "The warning could not be sent.") });
        }

        [HttpGet("Admin/CompanyWarnings")]
        public async Task<IActionResult> List(int companyId, CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return Json(new { success = false, message = "Not authenticated" });
            if (HttpContext.Session.GetString("UserType") != "Admin") return Json(new { success = false, message = "Not allowed" });

            var result = await _api.SendAsync(token, HttpMethod.Get, $"api/admin/companies/{companyId}/warnings", null, ct);
            return result.StatusCode is >= 200 and < 300
                ? Content(result.Body, "application/json")
                : Json(new { success = false, message = ApiErrorReader.Read(result.Body, "The warnings could not be loaded.") });
        }
    }
}
