using System.Text;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    /// <summary>Downloads a copy of the signed-in user's own data as a JSON file.</summary>
    public class MyDataController : Controller
    {
        private readonly IAiApiClient _api;

        public MyDataController(IAiApiClient api)
        {
            _api = api;
        }

        [HttpGet("MyData/Download")]
        public async Task<IActionResult> Download(CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");

            var result = await _api.SendAsync(token, HttpMethod.Get, "api/account/export", null, ct);
            if (result.StatusCode != StatusCodes.Status200OK)
            {
                TempData["ErrorMessage"] = "Your data could not be prepared right now. Please try again.";
                return LocalRedirect("~/");
            }

            return File(Encoding.UTF8.GetBytes(result.Body), "application/json", $"my-data-{DateTime.UtcNow:yyyyMMdd}.json");
        }
    }
}
