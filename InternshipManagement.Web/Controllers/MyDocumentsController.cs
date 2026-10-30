using InternshipManagement.Web.Models.Documents;
using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    /// <summary>A student's documents: upload as many as needed in each category and remove the ones no longer wanted.</summary>
    [Route("Documents/Mine")]
    public class MyDocumentsController : Controller
    {
        private const int MaxFilesPerUpload = 20;

        private static readonly (string Value, string Label)[] Categories =
        {
            ("Qualification", "Qualification / diploma"),
            ("Certificate", "Certificate"),
            ("Transcript", "Academic transcript"),
            ("Resume", "CV / résumé"),
            ("CoverLetter", "Cover letter"),
            ("Identification", "ID / passport"),
            ("Supporting", "Other supporting document")
        };

        private readonly IDocumentApiClient _documents;

        public MyDocumentsController(IDocumentApiClient documents) => _documents = documents;

        private string? Token => HttpContext.Session.GetString("JwtToken");
        private bool IsStudent => HttpContext.Session.GetString("UserType") == "Student";

        [HttpGet("")]
        public async Task<IActionResult> Index(CancellationToken ct)
        {
            if (string.IsNullOrEmpty(Token)) return RedirectToAction("Login", "Account");
            if (!IsStudent) return Forbid();

            ViewBag.Categories = Categories;
            var documents = await _documents.GetOwnedDocumentsAsync(Token!, ct);
            return View(documents.Where(d => d.RetentionState == "Active").ToList());
        }

        [HttpPost("Upload")]
        [ValidateAntiForgeryToken]
        [RequestSizeLimit(120_000_000)]
        [RequestFormLimits(MultipartBodyLengthLimit = 120_000_000)]
        public async Task<IActionResult> Upload(List<IFormFile> files, List<string> categories, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(Token)) return RedirectToAction("Login", "Account");
            if (!IsStudent) return Forbid();

            var picked = (files ?? new List<IFormFile>()).Where(f => f.Length > 0).ToList();
            if (picked.Count == 0)
            {
                TempData["ErrorMessage"] = "Choose at least one file to upload.";
                return RedirectToAction(nameof(Index));
            }
            if (picked.Count > MaxFilesPerUpload)
            {
                TempData["ErrorMessage"] = $"You can upload up to {MaxFilesPerUpload} files at a time.";
                return RedirectToAction(nameof(Index));
            }

            var saved = 0;
            var problems = new List<string>();
            for (var i = 0; i < picked.Count; i++)
            {
                var category = i < categories.Count && Categories.Any(c => c.Value == categories[i])
                    ? categories[i] : "Supporting";
                try
                {
                    await _documents.UploadAsync(Token!, picked[i], category, ct);
                    saved++;
                }
                catch (Exception ex)
                {
                    problems.Add($"{picked[i].FileName}: {ex.Message}");
                }
            }

            if (saved > 0)
                TempData["SuccessMessage"] = saved == 1 ? "1 document uploaded." : $"{saved} documents uploaded.";
            if (problems.Count > 0)
                TempData["ErrorMessage"] = "Some files were not uploaded. " + string.Join(" ", problems);
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("Remove")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Remove(Guid id, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(Token)) return RedirectToAction("Login", "Account");
            if (!IsStudent) return Forbid();

            var removed = await _documents.SetRetentionStateAsync(Token!, id, "Deleted", ct);
            TempData[removed ? "SuccessMessage" : "ErrorMessage"] = removed
                ? "Document removed."
                : "The document could not be removed.";
            return RedirectToAction(nameof(Index));
        }
    }
}
