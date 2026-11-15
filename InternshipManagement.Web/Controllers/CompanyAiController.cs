using System.Text;
using System.Text.Json;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    /// <summary>
    /// Company AI tools: write an internship draft from a short description, and see the best-matching
    /// students for one of the company's internships. The "ajax" actions are proxies, because the API
    /// token stays in the server session and never reaches the browser.
    /// </summary>
    public class CompanyAiController : Controller
    {
        private readonly IAiApiClient _ai;

        public CompanyAiController(IAiApiClient ai)
        {
            _ai = ai;
        }

        // ------------------------------------------------------------------ page

        [HttpGet]
        public IActionResult Candidates(int internshipId)
        {
            if (!TryGetCompanyToken(out _))
                return RedirectToAction("Login", "Account");
            if (internshipId <= 0)
                return RedirectToAction("ManageInternships", "Company");

            ViewData["Title"] = "Best candidates";
            ViewData["InternshipId"] = internshipId;
            return View();
        }

        // ------------------------------------------------------------------ ajax

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(16 * 1024)]
        public Task<IActionResult> GenerateDraft([FromBody] JsonElement body, CancellationToken ct) =>
            Forward(HttpMethod.Post, "api/internships/ai-draft",
                new StringContent(body.GetRawText(), Encoding.UTF8, "application/json"), ct);

        [HttpGet]
        public Task<IActionResult> Drafts(CancellationToken ct) =>
            Forward(HttpMethod.Get, "api/internships/ai-draft", null, ct);

        [HttpGet]
        public Task<IActionResult> Draft(int id, CancellationToken ct) =>
            Forward(HttpMethod.Get, $"api/internships/ai-draft/{id}", null, ct);

        // Discards a saved draft. The API answers 204 (no body), so it is mapped to a small JSON reply here.
        [HttpPost, ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteDraft(int id, CancellationToken ct)
        {
            if (id <= 0)
                return BadRequest(new { error = "Unknown draft." });
            if (!TryGetCompanyToken(out var token))
                return Unauthorized(new { error = "Your session has expired. Please log in again." });

            var result = await _ai.SendAsync(token, HttpMethod.Delete, $"api/internships/ai-draft/{id}", null, ct);
            if (result.StatusCode is >= 200 and < 300)
                return Ok(new { deleted = true });

            var body = string.IsNullOrWhiteSpace(result.Body) ? "{}" : result.Body;
            return new ContentResult { StatusCode = result.StatusCode, Content = body, ContentType = "application/json; charset=utf-8" };
        }

        [HttpGet]
        public Task<IActionResult> CandidatesData(int internshipId, int maxResults = 10, CancellationToken ct = default) =>
            Forward(HttpMethod.Get,
                $"api/Matching/company/internships/{internshipId}/students?maxResults={Math.Clamp(maxResults, 1, 25)}", null, ct);

        // ------------------------------------------------------------------ helpers

        private bool TryGetCompanyToken(out string token)
        {
            token = HttpContext.Session.GetString("JwtToken") ?? string.Empty;
            return token.Length > 0 && HttpContext.Session.GetString("UserType") == "Company";
        }

        private async Task<IActionResult> Forward(HttpMethod method, string path, HttpContent? content, CancellationToken ct)
        {
            if (!TryGetCompanyToken(out var token))
                return Unauthorized(new { error = "Your session has expired. Please log in again." });

            var result = await _ai.SendAsync(token, method, path, content, ct);
            if (result.RetryAfter != null)
                Response.Headers.RetryAfter = result.RetryAfter;

            var body = string.IsNullOrWhiteSpace(result.Body) ? "{}" : result.Body;
            return new ContentResult { StatusCode = result.StatusCode, Content = body, ContentType = "application/json; charset=utf-8" };
        }
    }
}
