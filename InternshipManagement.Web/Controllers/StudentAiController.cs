using System.Text;
using System.Text.Json;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace InternshipManagement.Web.Controllers
{
    /// <summary>
    /// Student AI pages: recommended internships, CV analysis, skill gaps and the career assistant.
    /// The pages are normal views; the actions marked "ajax" are small proxies the page script calls,
    /// because the API token stays in the server session and must never reach the browser.
    /// </summary>
    public class StudentAiController : Controller
    {
        private const long MaxCvBytes = 5 * 1024 * 1024;

        private readonly IAiApiClient _ai;

        public StudentAiController(IAiApiClient ai)
        {
            _ai = ai;
        }

        // ------------------------------------------------------------------ pages

        [HttpGet]
        public IActionResult Matches() => StudentPage("Recommended for you");

        [HttpGet]
        public IActionResult CvAnalysis() => StudentPage("CV analysis");

        [HttpGet]
        public IActionResult SkillGaps(int? internshipId)
        {
            ViewData["InternshipId"] = internshipId;
            return StudentPage("Skill gaps");
        }

        [HttpGet]
        public IActionResult Assistant() => StudentPage("Career assistant");

        // ------------------------------------------------------------------ ajax: matches

        [HttpGet]
        public Task<IActionResult> QuickMatches(int maxResults = 10, CancellationToken ct = default) =>
            Forward(HttpMethod.Get, $"api/Matching/student/internships?maxResults={Math.Clamp(maxResults, 1, 20)}", null, ct);

        [HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> StartMatches(int maxResults = 5, CancellationToken ct = default) =>
            Forward(HttpMethod.Post, $"api/Matching/student/internships/explained?maxResults={Math.Clamp(maxResults, 1, 10)}", null, ct);

        // ------------------------------------------------------------------ ajax: CV analysis

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> StartCv(IFormFile? file, CancellationToken ct)
        {
            if (file == null || file.Length == 0)
                return BadRequest(new { error = "Choose a PDF file first." });
            if (file.Length > MaxCvBytes)
                return BadRequest(new { error = "The CV is larger than 5 MB." });
            if (!Path.GetExtension(file.FileName).Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                return BadRequest(new { error = "Only PDF files are supported." });

            await using var stream = file.OpenReadStream();
            using var form = new MultipartFormDataContent();
            var part = new StreamContent(stream);
            part.Headers.ContentType = new MediaTypeHeaderValue("application/pdf");
            form.Add(part, "file", "cv.pdf");   // the original name is not needed and is not forwarded

            return await Forward(HttpMethod.Post, "api/students/cv-analysis", form, ct);
        }

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(512 * 1024)]
        public Task<IActionResult> ConfirmCv([FromBody] JsonElement body, CancellationToken ct) =>
            Forward(HttpMethod.Post, "api/students/cv-analysis/confirm",
                new StringContent(body.GetRawText(), Encoding.UTF8, "application/json"), ct);

        // ------------------------------------------------------------------ ajax: skill gaps

        [HttpGet]
        public Task<IActionResult> Gaps(int? internshipId, CancellationToken ct = default) =>
            Forward(HttpMethod.Get, "api/students/skill-gaps" + (internshipId is > 0 ? $"?internshipId={internshipId}" : string.Empty), null, ct);

        [HttpPost, ValidateAntiForgeryToken]
        public Task<IActionResult> StartGaps(int? internshipId, CancellationToken ct = default) =>
            Forward(HttpMethod.Post, "api/students/skill-gaps" + (internshipId is > 0 ? $"?internshipId={internshipId}" : string.Empty), null, ct);

        // ------------------------------------------------------------------ ajax: job status

        [HttpGet]
        public Task<IActionResult> Job(int id, CancellationToken ct = default) =>
            Forward(HttpMethod.Get, $"api/ai-jobs/{id}", null, ct);

        // ------------------------------------------------------------------ ajax: career assistant (streaming)

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(64 * 1024)]
        public async Task Chat([FromBody] JsonElement body, CancellationToken ct)
        {
            if (!TryGetStudentToken(out var token))
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            HttpResponseMessage upstream;
            try
            {
                upstream = await _ai.OpenStreamAsync(token, "api/students/career-assistant/chat", body.GetRawText(), ct);
            }
            catch (HttpRequestException)
            {
                await WriteJsonAsync(StatusCodes.Status503ServiceUnavailable,
                    "{\"error\":\"The assistant is not available right now. Please try again shortly.\"}", ct);
                return;
            }
            catch (OperationCanceledException)
            {
                return;
            }

            using (upstream)
            {
                if (!upstream.IsSuccessStatusCode)
                {
                    // Limits (429), validation (400) and so on arrive as normal JSON: pass them on.
                    var errorBody = await upstream.Content.ReadAsStringAsync(ct);
                    if (upstream.Headers.RetryAfter?.Delta is { } wait)
                        Response.Headers.RetryAfter = ((int)wait.TotalSeconds).ToString();
                    await WriteJsonAsync((int)upstream.StatusCode, errorBody, ct);
                    return;
                }

                Response.StatusCode = StatusCodes.Status200OK;
                Response.ContentType = "text/event-stream; charset=utf-8";
                Response.Headers.CacheControl = "no-cache";
                Response.Headers["X-Accel-Buffering"] = "no";
                HttpContext.Features.Get<IHttpResponseBodyFeature>()?.DisableBuffering();

                try
                {
                    await using var stream = await upstream.Content.ReadAsStreamAsync(ct);
                    var buffer = new byte[4096];
                    int read;
                    while ((read = await stream.ReadAsync(buffer, ct)) > 0)
                    {
                        await Response.Body.WriteAsync(buffer.AsMemory(0, read), ct);
                        await Response.Body.FlushAsync(ct);   // forward each piece as it arrives
                    }
                }
                catch (OperationCanceledException)
                {
                    // the browser closed the page or pressed Stop: nothing more to send
                }
            }
        }

        // ------------------------------------------------------------------ helpers

        private IActionResult StudentPage(string title)
        {
            if (!TryGetStudentToken(out _))
                return RedirectToAction("Login", "Account");

            ViewData["Title"] = title;
            return View();
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

            var result = await _ai.SendAsync(token, method, path, content, ct);
            if (result.RetryAfter != null)
                Response.Headers.RetryAfter = result.RetryAfter;

            // The API sometimes answers with an empty body (for example a 429 from the rate limiter).
            var body = string.IsNullOrWhiteSpace(result.Body) ? "{}" : result.Body;
            return new ContentResult { StatusCode = result.StatusCode, Content = body, ContentType = "application/json; charset=utf-8" };
        }

        private async Task WriteJsonAsync(int status, string json, CancellationToken ct)
        {
            Response.StatusCode = status;
            Response.ContentType = "application/json; charset=utf-8";
            await Response.WriteAsync(string.IsNullOrWhiteSpace(json) ? "{}" : json, ct);
        }
    }
}
