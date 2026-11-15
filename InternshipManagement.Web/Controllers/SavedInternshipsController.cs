using System.Text.Json;
using InternshipManagement.Web.Helpers;
using InternshipManagement.Web.Models.Student;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    /// <summary>A student's saved (bookmarked) internships: the list page and the save/unsave buttons.</summary>
    [Route("Student")]
    public class SavedInternshipsController : Controller
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly IAiApiClient _api;

        public SavedInternshipsController(IAiApiClient api)
        {
            _api = api;
        }

        [HttpGet("Saved")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var token = Token;
            if (token == null) return RedirectToAction("Login", "Account");

            var items = new List<SavedInternshipItem>();
            var result = await _api.SendAsync(token, HttpMethod.Get, "api/students/saved-internships", null, ct);
            if (result.StatusCode == StatusCodes.Status200OK)
            {
                try { items = JsonSerializer.Deserialize<List<SavedInternshipItem>>(result.Body, JsonOptions) ?? items; }
                catch (JsonException) { ViewBag.Error = "Your saved internships could not be read."; }
            }
            else
            {
                ViewBag.Error = "Your saved internships could not be loaded right now.";
            }

            return View(items);
        }

        [HttpGet("Saved/State/{id:int}")]
        public async Task<IActionResult> State(int id, CancellationToken ct)
        {
            var token = Token;
            if (token == null) return Json(new { saved = false });

            var result = await _api.SendAsync(token, HttpMethod.Get, "api/students/saved-internships/ids", null, ct);
            if (result.StatusCode != StatusCodes.Status200OK) return Json(new { saved = false });

            try
            {
                var ids = JsonSerializer.Deserialize<List<int>>(result.Body, JsonOptions) ?? new List<int>();
                return Json(new { saved = ids.Contains(id) });
            }
            catch (JsonException)
            {
                return Json(new { saved = false });
            }
        }

        [HttpPost("Saved/Add/{id:int}")]
        public Task<IActionResult> Add(int id, CancellationToken ct) => Change(HttpMethod.Post, id, ct);

        [HttpPost("Saved/Remove/{id:int}")]
        public Task<IActionResult> Remove(int id, CancellationToken ct) => Change(HttpMethod.Delete, id, ct);

        private async Task<IActionResult> Change(HttpMethod method, int id, CancellationToken ct)
        {
            var token = Token;
            if (token == null) return StatusCode(StatusCodes.Status401Unauthorized, new { message = "Please sign in again." });

            var result = await _api.SendAsync(token, method, $"api/students/saved-internships/{id}", null, ct);
            if (result.StatusCode is >= 200 and < 300)
                return Json(new { saved = method == HttpMethod.Post });

            return StatusCode(result.StatusCode, new { message = ApiErrorReader.Read(result.Body, "Could not update your saved list.") });
        }

        private string? Token => string.IsNullOrEmpty(HttpContext.Session.GetString("JwtToken")) ? null : HttpContext.Session.GetString("JwtToken");
    }
}
