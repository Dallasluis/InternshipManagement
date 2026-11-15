using System.Net.Http.Json;
using System.Text.Json;
using InternshipManagement.Web.Helpers;
using InternshipManagement.Web.Models.Admin;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    /// <summary>The internship category list: a read-only feed for the forms, and the administrator's editor.</summary>
    public class CategoriesController : Controller
    {
        private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
        private readonly IAiApiClient _api;

        public CategoriesController(IAiApiClient api)
        {
            _api = api;
        }

        // the forms ask for this to offer suggestions; an empty list is fine if it cannot be loaded
        [HttpGet("Categories/List")]
        public async Task<IActionResult> List(CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return Json(Array.Empty<string>());

            var result = await _api.SendAsync(token, HttpMethod.Get, "api/categories", null, ct);
            return new ContentResult
            {
                StatusCode = StatusCodes.Status200OK,
                ContentType = "application/json; charset=utf-8",
                Content = result.StatusCode == StatusCodes.Status200OK && !string.IsNullOrWhiteSpace(result.Body) ? result.Body : "[]"
            };
        }

        [HttpGet("Admin/Categories")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");
            if (!IsAdmin) return Forbid();

            var items = new List<CategoryItem>();
            var result = await _api.SendAsync(token, HttpMethod.Get, "api/admin/categories", null, ct);
            if (result.StatusCode == StatusCodes.Status200OK)
            {
                try { items = JsonSerializer.Deserialize<List<CategoryItem>>(result.Body, JsonOptions) ?? items; }
                catch (JsonException) { ViewBag.Error = "The categories could not be read."; }
            }
            else
            {
                ViewBag.Error = "The categories could not be loaded right now.";
            }

            return View("~/Views/AdminCategories/Index.cshtml", items);
        }

        [HttpPost("Admin/Categories/Add")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Add(string name, CancellationToken ct) =>
            Send(HttpMethod.Post, "api/admin/categories", new { Name = name ?? string.Empty }, "Category added.", ct);

        [HttpPost("Admin/Categories/{id:int}/Update")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Update(int id, string name, bool isActive, CancellationToken ct) =>
            Send(HttpMethod.Put, $"api/admin/categories/{id}", new { Name = name ?? string.Empty, IsActive = isActive }, "Category saved.", ct);

        [HttpPost("Admin/Categories/{id:int}/Delete")]
        [ValidateAntiForgeryToken]
        public Task<IActionResult> Delete(int id, CancellationToken ct) =>
            Send(HttpMethod.Delete, $"api/admin/categories/{id}", null, "Category removed. Existing internships keep their text.", ct);

        private async Task<IActionResult> Send(HttpMethod method, string path, object? body, string success, CancellationToken ct)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrEmpty(token)) return RedirectToAction("Login", "Account");
            if (!IsAdmin) return Forbid();

            var result = await _api.SendAsync(token, method, path, body == null ? null : JsonContent.Create(body), ct);
            if (result.StatusCode is >= 200 and < 300)
                TempData["SuccessMessage"] = success;
            else
                TempData["ErrorMessage"] = ApiErrorReader.Read(result.Body, "The change could not be saved.");

            return RedirectToAction(nameof(Index));
        }

        private bool IsAdmin => HttpContext.Session.GetString("UserType") == "Admin";
    }
}
