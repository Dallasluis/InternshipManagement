using System.Security.Cryptography;
using System.Text;
using InternshipManagement.Application.DTOs.Documents;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Application.Services
{
    public sealed class PrivateDocumentService : IPrivateDocumentService
    {
        private static readonly IReadOnlyDictionary<string, (string ContentType, byte[] Signature)> FileTypes =
            new Dictionary<string, (string, byte[])>(StringComparer.OrdinalIgnoreCase)
            {
                [".pdf"] = ("application/pdf", "%PDF-"u8.ToArray()),
                [".jpg"] = ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
                [".jpeg"] = ("image/jpeg", new byte[] { 0xFF, 0xD8, 0xFF }),
                [".png"] = ("image/png", new byte[] { 0x89, 0x50, 0x4E, 0x47, 0x0D, 0x0A, 0x1A, 0x0A }),
                [".webp"] = ("image/webp", "RIFF"u8.ToArray()),
                [".doc"] = ("application/msword", new byte[] { 0xD0, 0xCF, 0x11, 0xE0, 0xA1, 0xB1, 0x1A, 0xE1 }),
                [".docx"] = ("application/vnd.openxmlformats-officedocument.wordprocessingml.document", "PK\u0003\u0004"u8.ToArray())
            };

        private readonly IApplicationDbContext _context;
        private readonly IPrivateDocumentStorage _storage;

        public PrivateDocumentService(IApplicationDbContext context, IPrivateDocumentStorage storage)
        {
            _context = context;
            _storage = storage;
        }

        public async Task<PrivateDocumentResponse> UploadAsync(
            int userId,
            string role,
            string category,
            string fileName,
            long length,
            Stream content,
            CancellationToken cancellationToken)
        {
            if (userId <= 0 || length <= 0 || length > 10 * 1024 * 1024)
                throw new InvalidDataException("Choose a file no larger than 10 MB.");

            var normalizedCategory = NormalizeCategory(role, category);
            var safeFileName = Path.GetFileName(fileName);
            if (string.IsNullOrWhiteSpace(safeFileName) || safeFileName.Length > 255)
                throw new InvalidDataException("The file name is invalid.");

            var extension = Path.GetExtension(safeFileName);
            if (!FileTypes.TryGetValue(extension, out var fileType))
                throw new InvalidDataException("This file type is not supported.");

            if (!content.CanSeek)
                throw new InvalidDataException("The uploaded file stream must support validation.");

            var signature = new byte[fileType.Signature.Length];
            try
            {
                await content.ReadExactlyAsync(signature, cancellationToken);
            }
            catch (EndOfStreamException exception)
            {
                throw new InvalidDataException("The uploaded file is incomplete.", exception);
            }
            if (!signature.AsSpan().SequenceEqual(fileType.Signature))
                throw new InvalidDataException("The selected file does not match its file type.");

            if (extension.Equals(".webp", StringComparison.OrdinalIgnoreCase))
            {
                content.Position = 8;
                var marker = new byte[4];
                try
                {
                    await content.ReadExactlyAsync(marker, cancellationToken);
                }
                catch (EndOfStreamException exception)
                {
                    throw new InvalidDataException("The uploaded file is incomplete.", exception);
                }
                if (!marker.AsSpan().SequenceEqual("WEBP"u8))
                    throw new InvalidDataException("The selected file does not match its file type.");
            }

            content.Position = 0;
            var document = new PrivateDocument
            {
                Id = Guid.NewGuid(),
                OwnerUserId = userId,
                Category = normalizedCategory,
                OriginalFileName = safeFileName,
                ContentType = fileType.ContentType,
                Length = length,
                RetentionState = DocumentRetentionState.Active,
                CreatedAt = DateTimeOffset.UtcNow
            };

            await _storage.SaveAsync(document.Id, content, cancellationToken);
            try
            {
                _context.PrivateDocuments.Add(document);
                await _context.SaveChangesAsync(cancellationToken);
            }
            catch
            {
                await _storage.DeleteAsync(document.Id, cancellationToken);
                throw;
            }

            await AuditAsync(document.Id, userId, "Uploaded", null, null, cancellationToken);
            return ToResponse(document);
        }

        public async Task<IReadOnlyList<PrivateDocumentResponse>> GetOwnedDocumentsAsync(
            int userId,
            CancellationToken cancellationToken) =>
            await _context.PrivateDocuments
                .AsNoTracking()
                .Where(document => document.OwnerUserId == userId
                    && document.RetentionState != DocumentRetentionState.Deleted)
                .OrderByDescending(document => document.CreatedAt)
                .Select(document => new PrivateDocumentResponse(
                    document.Id,
                    document.OriginalFileName,
                    document.Category,
                    document.ContentType,
                    document.Length,
                    document.RetentionState.ToString(),
                    document.CreatedAt))
                .ToListAsync(cancellationToken);

        public async Task<DocumentAccessLinkResponse?> CreateAccessLinkAsync(
            Guid documentId,
            int userId,
            string role,
            int? reportId,
            int? companyProfileId,
            string accessPath,
            CancellationToken cancellationToken)
        {
            var document = await _context.PrivateDocuments
                .FirstOrDefaultAsync(item => item.Id == documentId, cancellationToken);
            if (document == null || document.RetentionState != DocumentRetentionState.Active
                || !await CanAccessAsync(document, userId, role, reportId, companyProfileId, cancellationToken))
                return null;

            var rawToken = Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
                .TrimEnd('=').Replace('+', '-').Replace('/', '_');
            var now = DateTimeOffset.UtcNow;
            _context.DocumentAccessTokens.Add(new DocumentAccessToken
            {
                TokenHash = HashToken(rawToken),
                DocumentId = documentId,
                RequestedByUserId = userId,
                ReportId = reportId,
                CompanyProfileId = companyProfileId,
                CreatedAt = now,
                ExpiresAt = now.AddMinutes(5)
            });
            await _context.SaveChangesAsync(cancellationToken);
            await AuditAsync(documentId, userId, "LinkIssued", reportId, companyProfileId, cancellationToken);
            return new DocumentAccessLinkResponse($"{accessPath.TrimEnd('/')}/{rawToken}", now.AddMinutes(5));
        }

        public async Task<(PrivateDocumentResponse Document, Stream Content)?> OpenAccessLinkAsync(
            string token,
            int userId,
            string role,
            CancellationToken cancellationToken)
        {
            var tokenHash = HashToken(token);
            var accessToken = await _context.DocumentAccessTokens
                .FirstOrDefaultAsync(item => item.TokenHash == tokenHash
                    && item.RequestedByUserId == userId
                    && item.ExpiresAt > DateTimeOffset.UtcNow
                    && item.RevokedAt == null,
                    cancellationToken);
            if (accessToken == null)
                return null;

            var document = await _context.PrivateDocuments
                .FirstOrDefaultAsync(item => item.Id == accessToken.DocumentId, cancellationToken);
            if (document == null || document.RetentionState != DocumentRetentionState.Active
                || !await CanAccessAsync(document, userId, role, accessToken.ReportId, accessToken.CompanyProfileId, cancellationToken))
                return null;

            var stream = await _storage.OpenReadAsync(document.Id, cancellationToken);
            await AuditAsync(document.Id, userId, "Downloaded", accessToken.ReportId, accessToken.CompanyProfileId, cancellationToken);
            return (ToResponse(document), stream);
        }

        public async Task<bool> SetRetentionStateAsync(
            Guid documentId,
            int userId,
            string role,
            string state,
            CancellationToken cancellationToken)
        {
            if (!Enum.TryParse<DocumentRetentionState>(state, true, out var retentionState)
                || !Enum.IsDefined(retentionState))
                throw new ArgumentException("The requested document retention state is invalid.", nameof(state));

            var document = await _context.PrivateDocuments
                .FirstOrDefaultAsync(item => item.Id == documentId && item.OwnerUserId == userId, cancellationToken);
            if (document == null)
                return false;
            if (document.RetentionState == DocumentRetentionState.Deleted
                && retentionState != DocumentRetentionState.Deleted)
                throw new InvalidOperationException("Deleted documents cannot be reactivated.");

            document.RetentionState = retentionState;
            document.ClosedAt = retentionState == DocumentRetentionState.Active ? null : DateTimeOffset.UtcNow;
            await _context.DocumentAccessTokens
                .Where(item => item.DocumentId == documentId && item.RevokedAt == null)
                .ExecuteUpdateAsync(setters => setters.SetProperty(
                    item => item.RevokedAt, DateTimeOffset.UtcNow), cancellationToken);
            await _context.SaveChangesAsync(cancellationToken);
            await AuditAsync(documentId, userId, $"Retention:{retentionState}", null, null, cancellationToken);
            return true;
        }

        private async Task<bool> CanAccessAsync(
            PrivateDocument document,
            int userId,
            string role,
            int? reportId,
            int? companyProfileId,
            CancellationToken cancellationToken)
        {
            if (document.OwnerUserId == userId)
                return true;

            var attachedApplications = await _context.InternshipApplications
                .AsNoTracking()
                .Where(application => !application.IsDeleted)
                .Select(application => new
                {
                    application.Id,
                    StudentUserId = application.StudentProfile.UserId,
                    application.InternshipId,
                    CompanyUserId = application.Internship.CompanyProfile.UserId,
                    application.AdditionalDocuments
                })
                .ToListAsync(cancellationToken);

            var isAttached = attachedApplications.Any(application =>
                ContainsDocumentId(application.AdditionalDocuments, document.Id));

            if (role == "Company" && isAttached)
                return attachedApplications.Any(application =>
                    application.StudentUserId == document.OwnerUserId && application.CompanyUserId == userId
                    && ContainsDocumentId(application.AdditionalDocuments, document.Id));

            if (role == "Company")
            {
                var companyProgressReports = await _context.ProgressReports
                    .AsNoTracking()
                    .Where(report => report.Placement.CompanyProfile.UserId == userId)
                    .Select(report => report.SupportingDocuments)
                    .ToListAsync(cancellationToken);
                if (companyProgressReports.Any(serialized => ContainsDocumentId(serialized, document.Id))
                    && document.OwnerUserId != userId)
                    return true;
            }

            if (role == "Admin" && reportId.HasValue)
            {
                var report = await _context.Reports
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == reportId.Value
                        && !item.IsDeleted
                        && (item.Status == ReportStatus.Pending
                            || item.Status == ReportStatus.UnderInvestigation),
                        cancellationToken);
                if (report == null)
                    return false;

                var internship = await _context.Internships
                    .AsNoTracking()
                    .Where(item => item.Id == report.InternshipId && !item.IsDeleted)
                    .Select(item => new { item.CompanyProfile.UserId })
                    .FirstOrDefaultAsync(cancellationToken);
                var progressReports = await _context.ProgressReports
                    .AsNoTracking()
                    .Where(progress => progress.Placement.InternshipId == report.InternshipId)
                    .Select(progress => new
                    {
                        progress.Placement.StudentProfile.UserId,
                        progress.SupportingDocuments
                    })
                    .ToListAsync(cancellationToken);
                return internship != null
                    && (document.OwnerUserId == internship.UserId
                        || attachedApplications.Any(application =>
                            application.InternshipId == report.InternshipId
                            && application.StudentUserId == document.OwnerUserId
                            && ContainsDocumentId(application.AdditionalDocuments, document.Id))
                        || progressReports.Any(progress =>
                            progress.UserId == document.OwnerUserId
                            && ContainsDocumentId(progress.SupportingDocuments, document.Id)));
            }

            if (role == "Admin" && companyProfileId.HasValue)
            {
                var company = await _context.CompanyProfiles
                    .AsNoTracking()
                    .FirstOrDefaultAsync(item => item.Id == companyProfileId.Value
                        && !item.IsDeleted
                        && item.VerificationStatus == CompanyVerificationStatus.PendingReview,
                        cancellationToken);
                return company != null
                    && company.UserId == document.OwnerUserId
                    && ContainsDocumentId(company.VerificationDocuments, document.Id);
            }

            return false;
        }

        private async Task AuditAsync(
            Guid documentId,
            int userId,
            string action,
            int? reportId,
            int? companyProfileId,
            CancellationToken cancellationToken)
        {
            _context.DocumentAccessAudits.Add(new DocumentAccessAudit
            {
                DocumentId = documentId,
                UserId = userId,
                Action = action,
                ReportId = reportId,
                CompanyProfileId = companyProfileId,
                AccessedAt = DateTimeOffset.UtcNow
            });
            await _context.SaveChangesAsync(cancellationToken);
        }

        private static string NormalizeCategory(string role, string category)
        {
            var allowedCategories = role switch
            {
                "Student" => new[] { "Resume", "CoverLetter", "Transcript", "Qualification", "Identification", "Certificate", "Supporting", "Application" },
                "Company" => new[] { "CompanyEvidence" },
                _ => Array.Empty<string>()
            };
            var normalized = allowedCategories.FirstOrDefault(
                candidate => candidate.Equals(category, StringComparison.OrdinalIgnoreCase));
            return normalized ?? throw new InvalidDataException("This document category is not allowed for your account.");
        }

        private static bool ContainsDocumentId(string? serializedIds, Guid documentId)
        {
            if (string.IsNullOrWhiteSpace(serializedIds))
                return false;
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(serializedIds)?.Contains(documentId) == true;
            }
            catch (System.Text.Json.JsonException)
            {
                return false;
            }
        }

        private static string HashToken(string token) =>
            Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(token)));

        private static PrivateDocumentResponse ToResponse(PrivateDocument document) =>
            new(document.Id, document.OriginalFileName, document.Category, document.ContentType,
                document.Length, document.RetentionState.ToString(), document.CreatedAt);
    }
}
