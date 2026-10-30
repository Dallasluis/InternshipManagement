using InternshipManagement.Web.Services;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Web.Controllers
{
    public sealed class DocumentsController : Controller
    {
        private readonly IDocumentApiClient _documents;

        public DocumentsController(IDocumentApiClient documents)
        {
            _documents = documents;
        }

        [HttpGet]
        public async Task<IActionResult> Download(
            Guid id,
            int? reportId,
            int? companyProfileId,
            CancellationToken cancellationToken)
        {
            var token = HttpContext.Session.GetString("JwtToken");
            if (string.IsNullOrWhiteSpace(token))
                return RedirectToAction("Login", "Account");

            var file = await _documents.DownloadAsync(token, id, reportId, companyProfileId, cancellationToken);
            return file == null ? NotFound() : File(file.Content, file.ContentType, file.FileName);
        }
    }
}
