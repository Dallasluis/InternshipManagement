using System.Security.Claims;
using InternshipManagement.Application.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace InternshipManagement.Api.Controllers
{
    [ApiController]
    [Route("api/documents")]
    [Authorize(Roles = "Student,Company,Admin")]
    public sealed class DocumentsController : ControllerBase
    {
        private readonly IPrivateDocumentService _documents;

        public DocumentsController(IPrivateDocumentService documents)
        {
            _documents = documents;
        }

        [HttpPost]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(10 * 1024 * 1024)]
        public async Task<IActionResult> Upload(
            [FromForm] UploadPrivateDocumentRequest request,
            CancellationToken cancellationToken)
        {
            try
            {
                await using var content = request.File.OpenReadStream();
                var document = await _documents.UploadAsync(
                    CurrentUserId,
                    CurrentRole,
                    request.Category,
                    request.File.FileName,
                    request.File.Length,
                    content,
                    cancellationToken);
                return CreatedAtAction(nameof(GetDocument), new { documentId = document.Id }, document);
            }
            catch (InvalidDataException exception)
            {
                return BadRequest(new ProblemDetails { Title = "Invalid document", Detail = exception.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetOwnedDocuments(CancellationToken cancellationToken) =>
            Ok(await _documents.GetOwnedDocumentsAsync(CurrentUserId, cancellationToken));

        [HttpGet("{documentId:guid}")]
        public async Task<IActionResult> GetDocument(Guid documentId, CancellationToken cancellationToken)
        {
            var document = (await _documents.GetOwnedDocumentsAsync(CurrentUserId, cancellationToken))
                .FirstOrDefault(item => item.Id == documentId);
            return document == null ? NotFound() : Ok(document);
        }

        [HttpPost("{documentId:guid}/access-link")]
        public async Task<IActionResult> CreateAccessLink(
            Guid documentId,
            [FromQuery] int? reportId,
            [FromQuery] int? companyProfileId,
            CancellationToken cancellationToken)
        {
            var link = await _documents.CreateAccessLinkAsync(
                documentId,
                CurrentUserId,
                CurrentRole,
                reportId,
                companyProfileId,
                "/api/documents/access",
                cancellationToken);
            return link == null ? NotFound() : Ok(link);
        }

        [HttpGet("access/{token}")]
        public async Task<IActionResult> OpenAccessLink(string token, CancellationToken cancellationToken)
        {
            var access = await _documents.OpenAccessLinkAsync(
                token, CurrentUserId, CurrentRole, cancellationToken);
            if (access == null)
                return NotFound();

            return File(
                access.Value.Content,
                access.Value.Document.ContentType,
                access.Value.Document.OriginalFileName,
                enableRangeProcessing: true);
        }

        [HttpPut("{documentId:guid}/retention")]
        public async Task<IActionResult> SetRetentionState(
            Guid documentId,
            [FromBody] SetDocumentRetentionRequest request,
            CancellationToken cancellationToken)
        {
            var changed = await _documents.SetRetentionStateAsync(
                documentId, CurrentUserId, CurrentRole, request.State, cancellationToken);
            return changed ? NoContent() : NotFound();
        }

        private int CurrentUserId =>
            int.TryParse(User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var userId)
                ? userId
                : 0;

        private string CurrentRole => User.FindFirst(ClaimTypes.Role)?.Value
            ?? User.FindFirst("role")?.Value
            ?? string.Empty;

        public sealed class UploadPrivateDocumentRequest
        {
            public required string Category { get; init; }
            public required IFormFile File { get; init; }
        }

        public sealed class SetDocumentRetentionRequest
        {
            public required string State { get; init; }
        }
    }
}
