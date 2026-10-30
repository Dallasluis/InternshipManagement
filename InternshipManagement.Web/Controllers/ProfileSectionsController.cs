using System.Text;
using System.Text.Json;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    /// <summary>
    /// Lets a student add and remove education, work experience, skills, certificates and projects.
    /// The page script calls the actions below; they pass the request to the API with the session token.
    /// </summary>
    public class ProfileSectionsController : Controller
    {
        // Only these names are forwarded, so the "section" value can never build another API address.
        private static readonly HashSet<string> Sections = new(StringComparer.OrdinalIgnoreCase)
        {
            "education", "experience", "skill", "certification", "project"
        };

        private readonly IAiApiClient _api;

        public ProfileSectionsController(IAiApiClient api)
        {
            _api = api;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!TryGetStudentToken(out _))
                return RedirectToAction("Login", "Account");

            ViewData["Title"] = "Education & Skills";
            return View();
        }

        [HttpGet]
        public Task<IActionResult> ProfileData(CancellationToken ct) =>
            Forward(HttpMethod.Get, "api/Students/profile", null, ct);

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(16 * 1024)]
        public Task<IActionResult> Add(string section, [FromBody] JsonElement body, CancellationToken ct)
        {
            if (!Sections.Contains(section))
                return Task.FromResult<IActionResult>(BadRequest(new { error = "Unknown section." }));

            return Forward(HttpMethod.Post, $"api/Students/{section.ToLowerInvariant()}",
                new StringContent(body.GetRawText(), Encoding.UTF8, "application/json"), ct);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> Remove(string section, int id, CancellationToken ct)
        {
            if (!Sections.Contains(section) || id <= 0)
                return Task.FromResult<IActionResult>(BadRequest(new { error = "Unknown item." }));

            return Forward(HttpMethod.Delete, $"api/Students/{section.ToLowerInvariant()}/{id}", null, ct);
        }

        private bool TryGetStudentToken(out string token)
        {
            token = HttpContext.Session.GetString("JwtToken") ?? string.Empty;
            return token.Length > 0 && HttpContext.Session.GetString("UserType") == "Student";
        }

        private async Task<IActionResult> Forward(HttpMethod method, string path, HttpContent? content, CancellationToken ct)
        {
            if (!TryGetStudentToken(out var token))
                return Unauthorized(new { error = "Your session has expired. Please log in again." });

            var result = await _api.SendAsync(token, method, path, content, ct);
            var body = string.IsNullOrWhiteSpace(result.Body) ? "{}" : result.Body;
            return new ContentResult { StatusCode = result.StatusCode, Content = body, ContentType = "application/json; charset=utf-8" };
        }
    }
}
