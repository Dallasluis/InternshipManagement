using InternshipManagement.Web.Models.Internship;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text.Json;

namespace InternshipManagement.Web.Controllers
{
    public class InternshipsController : Controller
    {
        private readonly IInternshipApiClient _internshipApiClient;
        private readonly IReportApiClient _reportApiClient;
        private readonly IDocumentApiClient _documentApiClient;
        private readonly IAiApiClient _aiApiClient;

        public InternshipsController(
            IInternshipApiClient internshipApiClient,
            IReportApiClient reportApiClient,
            IDocumentApiClient documentApiClient,
            IAiApiClient aiApiClient)
        {
            _internshipApiClient = internshipApiClient;
            _reportApiClient = reportApiClient;
            _documentApiClient = documentApiClient;
            _aiApiClient = aiApiClient;
        }

        [HttpGet]
        public async Task<IActionResult> Index(InternshipSearchViewModel filters, CancellationToken cancellationToken)
        {
            if (filters.PageNumber < 1) filters.PageNumber = 1;
            if (filters.PageSize < 1) filters.PageSize = 12;

            var result = await _internshipApiClient.SearchAsync(filters);

            var token = HttpContext.Session.GetString("JwtToken");
            if (HttpContext.Session.GetString("UserType") == "Student" && !string.IsNullOrWhiteSpace(token))
            {
                var savedResult = await _aiApiClient.SendAsync(token, HttpMethod.Get, "api/students/saved-internships/ids", cancellationToken: cancellationToken);
                if (savedResult.StatusCode == StatusCodes.Status200OK)
                {
                    try
                    {
                        var savedIds = JsonSerializer.Deserialize<HashSet<int>>(savedResult.Body, new JsonSerializerOptions(JsonSerializerDefaults.Web)) ?? new HashSet<int>();
                        foreach (var internship in result.Items)
                            internship.IsSaved = savedIds.Contains(internship.Id);
                    }
                    catch (JsonException)
                    {
                        // Keep browsing available if the saved-state response is malformed.
                    }
                }
            }

            ViewBag.Filters = filters;
            return View(result);
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var internship = await _internshipApiClient.GetByIdAsync(id);

            if (internship == null)
                return NotFound();

            var token = HttpContext.Session.GetString("JwtToken");
            if (HttpContext.Session.GetString("UserType") == "Student" && !string.IsNullOrWhiteSpace(token))
                ViewBag.ApplicationDocuments = await _documentApiClient.GetOwnedDocumentsAsync(token);

            return View(internship);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Report(SubmitInternshipReportRequest request)
        {
            if (HttpContext.Session.GetString("UserType") != "Student")
                return Forbid();

            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            try
            {
                await _reportApiClient.SubmitInternshipReportAsync(token, request);
                TempData["SuccessMessage"] = "Thank you. Your report has been submitted for review.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Details), new { id = request.InternshipId });
        }
    }
}
