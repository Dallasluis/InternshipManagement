using System.Net.Http.Headers;
using System.Text.Json;
using InternshipManagement.Web.Helpers;
using InternshipManagement.Web.Models.Offer;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    /// <summary>The offer letter page, the attached contract, and attaching a contract (company).</summary>
    public class OfferLettersController : Controller
    {
        private static readonly JsonSerializerOptions Json = new() { PropertyNameCaseInsensitive = true };

        private readonly IAiApiClient _api;
        private readonly IHttpClientFactory _http;

        public OfferLettersController(IAiApiClient api, IHttpClientFactory http)
        {
            _api = api;
            _http = http;
        }

        [HttpGet("Offer/Letter/{id:int}")]
        public async Task<IActionResult> Letter(int id, CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");

            var result = await _api.SendAsync(token, HttpMethod.Get, $"api/offers/{id}/letter", null, ct);
            if (result.StatusCode is < 200 or >= 300)
            {
                TempData["ErrorMessage"] = "That offer could not be found.";
                return LocalRedirect("~/");
            }

            var model = JsonSerializer.Deserialize<OfferLetterModel>(result.Body, Json);
            if (model == null) return LocalRedirect("~/");

            ViewBag.UserType = HttpContext.Session.GetString("UserType");
            return View(model);
        }

        [HttpGet("Offer/Contract/{id:int}")]
        public async Task<IActionResult> Contract(int id, CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");

            using var request = new HttpRequestMessage(HttpMethod.Get, $"api/offers/{id}/contract");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            using var response = await _http.CreateClient("InternshipApi")
                .SendAsync(request, HttpCompletionOption.ResponseContentRead, ct);
            if (!response.IsSuccessStatusCode) return NotFound();

            var bytes = await response.Content.ReadAsByteArrayAsync(ct);
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            var name = response.Content.Headers.ContentDisposition?.FileNameStar
                ?? response.Content.Headers.ContentDisposition?.FileName?.Trim('"')
                ?? "contract";
            return File(bytes, contentType, name);
        }

        [HttpPost("Offer/Contract/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Attach(int id, IFormFile? file, CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");
            if (HttpContext.Session.GetString("UserType") != "Company") return StatusCode(403);

            if (file == null || file.Length == 0)
            {
                TempData["ErrorMessage"] = "Choose a file to attach.";
                return RedirectToAction(nameof(Letter), new { id });
            }

            await using var stream = file.OpenReadStream();
            using var fileContent = new StreamContent(stream);
            fileContent.Headers.ContentType = new MediaTypeHeaderValue("application/octet-stream");
            using var form = new MultipartFormDataContent { { fileContent, "file", Path.GetFileName(file.FileName) } };

            var result = await _api.SendAsync(token, HttpMethod.Post, $"api/offers/{id}/contract", form, ct);
            if (result.StatusCode is >= 200 and < 300)
                TempData["SuccessMessage"] = "Contract attached. The student has been notified.";
            else
                TempData["ErrorMessage"] = ApiErrorReader.Read(result.Body, "The contract could not be attached.");

            return RedirectToAction(nameof(Letter), new { id });
        }
    }
}
