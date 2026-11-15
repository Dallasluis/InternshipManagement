using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace InternshipManagement.Web.Controllers
{
    /// <summary>
    /// The company assistant page and the small streaming proxy it calls. The API token stays in the server session
    /// and never reaches the browser. Reuses the student assistant's script and styles.
    /// </summary>
    public class CompanyAssistantController : Controller
    {
        private readonly IAiApiClient _ai;

        public CompanyAssistantController(IAiApiClient ai)
        {
            _ai = ai;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (!TryGetCompanyToken(out _))
                return RedirectToAction("Login", "Account");

            ViewData["Title"] = "AI assistant";
            return View();
        }

        [HttpPost, ValidateAntiForgeryToken]
        [RequestSizeLimit(64 * 1024)]
        public async Task Chat([FromBody] JsonElement body, CancellationToken ct)
        {
            if (!TryGetCompanyToken(out var token))
            {
                Response.StatusCode = StatusCodes.Status401Unauthorized;
                return;
            }

            HttpResponseMessage upstream;
            try
            {
                upstream = await _ai.OpenStreamAsync(token, "api/companies/assistant/chat", body.GetRawText(), ct);
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
                        await Response.Body.FlushAsync(ct);
                    }
                }
                catch (OperationCanceledException)
                {
                    // the page was closed or Stop was pressed
                }
            }
        }

        private bool TryGetCompanyToken(out string token)
        {
            token = HttpContext.Session.GetString("JwtToken") ?? string.Empty;
            return token.Length > 0 && HttpContext.Session.GetString("UserType") == "Company";
        }

        private async Task WriteJsonAsync(int status, string json, CancellationToken ct)
        {
            Response.StatusCode = status;
            Response.ContentType = "application/json; charset=utf-8";
            await Response.WriteAsync(string.IsNullOrWhiteSpace(json) ? "{}" : json, ct);
        }
    }
}
