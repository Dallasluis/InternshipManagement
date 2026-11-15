using System.Security.Claims;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;
using InternshipManagement.Infrastructure.Persistence;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Api.Controllers
{
    /// <summary>The offer letter an intern reads before answering, and the optional contract attached to it.</summary>
    [ApiController]
    [Authorize(Roles = "Student,Company,Admin")]
    public class OfferLettersController : ControllerBase
    {
        private const long MaxContractBytes = 5 * 1024 * 1024;

        private readonly ApplicationDbContext _db;
        private readonly INotificationService _notifications;

        public OfferLettersController(ApplicationDbContext db, INotificationService notifications)
        {
            _db = db;
            _notifications = notifications;
        }

        [HttpGet("api/offers/{applicationId:int}/letter")]
        public async Task<IActionResult> Letter(int applicationId, CancellationToken ct)
        {
            var offer = await LoadAsync(applicationId, ct);
            if (offer == null || !CanView(offer.StudentUserId, offer.CompanyUserId) || !offer.HasOffer)
                return NotFound(new { Error = "Offer not found." });

            var sentAt = await _db.ApplicationStatusHistories.AsNoTracking()
                .Where(h => h.InternshipApplicationId == applicationId && h.NewStatus == ApplicationStatus.OfferMade)
                .OrderByDescending(h => h.ChangedAt)
                .Select(h => (DateTime?)h.ChangedAt)
                .FirstOrDefaultAsync(ct);

            var contract = await _db.Set<OfferContract>().AsNoTracking()
                .Where(c => c.InternshipApplicationId == applicationId)
                .Select(c => new { c.FileName, c.Length })
                .FirstOrDefaultAsync(ct);

            return Ok(new
            {
                ApplicationId = applicationId,
                Status = offer.Status.ToString(),
                offer.CompanyName,
                offer.CompanyAddress,
                offer.InternshipTitle,
                offer.StudentName,
                offer.StipendAmount,
                offer.StartDate,
                offer.ExpiryDate,
                offer.Details,
                SentAt = sentAt,
                ContractFileName = contract?.FileName,
                ContractLength = contract?.Length,
                CanAttachContract = IsCompanyOwner(offer.CompanyUserId) && offer.Status == ApplicationStatus.OfferMade
            });
        }

        [HttpPost("api/offers/{applicationId:int}/contract")]
        [Authorize(Roles = "Company")]
        [Consumes("multipart/form-data")]
        [RequestSizeLimit(6 * 1024 * 1024)]
        public async Task<IActionResult> Attach(int applicationId, [FromForm] IFormFile file, CancellationToken ct)
        {
            var offer = await LoadAsync(applicationId, ct);
            if (offer == null || !IsCompanyOwner(offer.CompanyUserId))
                return NotFound(new { Error = "Offer not found." });
            if (offer.Status != ApplicationStatus.OfferMade)
                return BadRequest(new { Error = "A contract can only be attached while the offer is waiting for an answer." });
            if (file == null || file.Length == 0)
                return BadRequest(new { Error = "Choose a file to attach." });
            if (file.Length > MaxContractBytes)
                return BadRequest(new { Error = "The contract must be 5 MB or smaller." });

            var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
            var contentType = extension switch
            {
                ".pdf" => "application/pdf",
                ".doc" => "application/msword",
                ".docx" => "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                _ => null
            };
            if (contentType == null)
                return BadRequest(new { Error = "Attach a PDF or Word document (.pdf, .doc, .docx)." });

            byte[] bytes;
            using (var ms = new MemoryStream())
            {
                await file.CopyToAsync(ms, ct);
                bytes = ms.ToArray();
            }
            if (!LooksLike(extension, bytes))
                return BadRequest(new { Error = "That file does not look like a valid " + extension.TrimStart('.').ToUpperInvariant() + " document." });

            var safeName = Path.GetFileName(file.FileName);
            if (safeName.Length > 200) safeName = safeName[^200..];

            var existing = await _db.Set<OfferContract>().FirstOrDefaultAsync(c => c.InternshipApplicationId == applicationId, ct);
            if (existing == null)
            {
                existing = new OfferContract { InternshipApplicationId = applicationId };
                _db.Set<OfferContract>().Add(existing);
            }
            existing.FileName = safeName;
            existing.ContentType = contentType;
            existing.Length = bytes.Length;
            existing.Content = bytes;
            existing.UploadedByUserId = CurrentUserId;
            existing.UploadedAt = DateTime.UtcNow;
            await _db.SaveChangesAsync(ct);

            await _notifications.NotifyAsync(offer.StudentUserId, NotificationTypes.Offer,
                "A contract was attached to your offer",
                $"{offer.CompanyName} attached a contract to your offer for \"{offer.InternshipTitle}\". Please read it before you answer.",
                $"/Offer/Letter/{applicationId}", sendEmail: false, cancellationToken: ct);

            return Ok(new { Success = true });
        }

        [HttpGet("api/offers/{applicationId:int}/contract")]
        public async Task<IActionResult> Download(int applicationId, CancellationToken ct)
        {
            var offer = await LoadAsync(applicationId, ct);
            if (offer == null || !CanView(offer.StudentUserId, offer.CompanyUserId))
                return NotFound();

            var contract = await _db.Set<OfferContract>().AsNoTracking()
                .FirstOrDefaultAsync(c => c.InternshipApplicationId == applicationId, ct);
            if (contract == null) return NotFound();

            Response.Headers["X-Content-Type-Options"] = "nosniff";
            return File(contract.Content, contract.ContentType, contract.FileName);
        }

        private async Task<OfferInfo?> LoadAsync(int applicationId, CancellationToken ct) =>
            await _db.InternshipApplications.AsNoTracking()
                .Where(a => a.Id == applicationId && !a.IsDeleted)
                .Select(a => new OfferInfo
                {
                    Status = a.Status,
                    HasOffer = a.OfferExpiryDate != null,
                    CompanyName = a.Internship.CompanyProfile.CompanyName,
                    CompanyAddress = ((a.Internship.CompanyProfile.Address ?? "") + " "
                        + (a.Internship.CompanyProfile.City ?? "") + " "
                        + (a.Internship.CompanyProfile.Country ?? "")).Trim(),
                    InternshipTitle = a.Internship.Title,
                    StudentName = a.StudentProfile.FirstName + " " + a.StudentProfile.LastName,
                    StudentUserId = a.StudentProfile.UserId,
                    CompanyUserId = a.Internship.CompanyProfile.UserId,
                    StipendAmount = a.OfferStipendAmount,
                    StartDate = a.OfferStartDate,
                    ExpiryDate = a.OfferExpiryDate,
                    Details = a.OfferDetails
                })
                .FirstOrDefaultAsync(ct);

        private bool CanView(int studentUserId, int companyUserId) =>
            User.IsInRole("Admin")
            || (User.IsInRole("Student") && CurrentUserId == studentUserId)
            || (User.IsInRole("Company") && CurrentUserId == companyUserId);

        private bool IsCompanyOwner(int companyUserId) => User.IsInRole("Company") && CurrentUserId == companyUserId;

        private static bool LooksLike(string extension, byte[] b) => extension switch
        {
            ".pdf" => b.Length > 4 && b[0] == 0x25 && b[1] == 0x50 && b[2] == 0x44 && b[3] == 0x46,
            ".docx" => b.Length > 4 && b[0] == 0x50 && b[1] == 0x4B,
            ".doc" => b.Length > 4 && b[0] == 0xD0 && b[1] == 0xCF && b[2] == 0x11 && b[3] == 0xE0,
            _ => false
        };

        private int CurrentUserId =>
            int.TryParse(User.FindFirst("userId")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value, out var id) ? id : 0;

        private sealed class OfferInfo
        {
            public ApplicationStatus Status { get; set; }
            public bool HasOffer { get; set; }
            public string CompanyName { get; set; } = string.Empty;
            public string CompanyAddress { get; set; } = string.Empty;
            public string InternshipTitle { get; set; } = string.Empty;
            public string StudentName { get; set; } = string.Empty;
            public int StudentUserId { get; set; }
            public int CompanyUserId { get; set; }
            public decimal? StipendAmount { get; set; }
            public DateTime? StartDate { get; set; }
            public DateTime? ExpiryDate { get; set; }
            public string? Details { get; set; }
        }
    }
}
