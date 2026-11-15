using Microsoft.EntityFrameworkCore;
using InternshipManagement.Application.DTOs.Company;
using InternshipManagement.Application.DTOs.Documents;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;
using System.Collections.Generic;
using System.Text.Json;

namespace InternshipManagement.Application.Services
{
    public class CompanyService : ICompanyService
    {
        private static readonly HashSet<string> VerificationReasonCodes = new(StringComparer.OrdinalIgnoreCase)
        {
            "APPROVED",
            "INCOMPLETE_EVIDENCE",
            "REGISTRATION_MISMATCH",
            "FRAUD_RISK",
            "POLICY_VIOLATION",
            "OTHER"
        };

        private readonly IApplicationDbContext _context;

        private readonly INotificationService _notifications;

        public CompanyService(IApplicationDbContext context, INotificationService notifications)
        {
            _context = context;
            _notifications = notifications;
        }

        public async Task<CompanyProfileResponse> GetCompanyProfileAsync(int userId)
        {
            var profile = await _context.CompanyProfiles
                .Include(c => c.Internships)
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (profile == null) return null;

            return await MapToResponse(profile);
        }

        public async Task<CompanyProfileResponse> UpdateCompanyProfileAsync(int userId, UpdateCompanyProfileRequest request)
        {
            var profile = await _context.CompanyProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (profile == null)
                throw new Exception("Company profile not found.");

            if (request.LogoUrl != null
                && !request.LogoUrl.StartsWith("/uploads/company-logos/", StringComparison.Ordinal))
                throw new InvalidOperationException("Company logos must be uploaded as image files.");

            var identityChanged =
                (request.LegalName != null && profile.LegalName != request.LegalName)
                || (request.RegistrationNumber != null && profile.RegistrationNumber != request.RegistrationNumber)
                || (request.Address != null && profile.Address != request.Address);
            profile.LegalName = request.LegalName ?? profile.LegalName;
            profile.RegistrationNumber = request.RegistrationNumber ?? profile.RegistrationNumber;
            profile.Description = request.Description ?? profile.Description;
            profile.Industry = request.Industry ?? profile.Industry;
            profile.Website = request.Website ?? profile.Website;
            profile.LinkedInUrl = request.LinkedInUrl ?? profile.LinkedInUrl;
            profile.Address = request.Address ?? profile.Address;
            identityChanged = identityChanged
                || (request.City != null && profile.City != request.City)
                || (request.Country != null && profile.Country != request.Country);
            profile.City = request.City ?? profile.City;
            profile.Country = request.Country ?? profile.Country;
            profile.PhoneNumber = request.PhoneNumber ?? profile.PhoneNumber;
            profile.LogoUrl = request.LogoUrl ?? profile.LogoUrl;
            profile.UpdatedAt = DateTime.UtcNow;
            if (identityChanged && profile.VerificationStatus == CompanyVerificationStatus.Verified)
            {
                profile.VerificationStatus = CompanyVerificationStatus.ReverificationRequired;
                profile.ReverificationRequiredAt = DateTime.UtcNow;
                AddHistory(profile.Id, profile.VerificationStatus, "COMPANY_DETAILS_CHANGED",
                    "Re-verification required after legal identity or address changed.", null, false);
            }

            await _context.SaveChangesAsync();

            return await MapToResponse(profile);
        }

        public async Task<bool> SubmitVerificationAsync(int userId, SubmitVerificationRequest request)
        {
            var profile = await _context.CompanyProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (profile == null) return false;
            if (profile.VerificationStatus is CompanyVerificationStatus.Verified or CompanyVerificationStatus.Suspended)
                throw new InvalidOperationException("This company cannot submit verification evidence in its current status.");

            var evidenceIds = request.EvidenceDocumentIds.Distinct().ToList();
            if (evidenceIds.Count == 0)
                throw new InvalidOperationException("Upload at least one verification evidence document.");
            var ownedEvidenceCount = await _context.PrivateDocuments.CountAsync(document =>
                evidenceIds.Contains(document.Id)
                && document.OwnerUserId == userId
                && document.Category == "CompanyEvidence"
                && document.RetentionState == DocumentRetentionState.Active);
            if (ownedEvidenceCount != evidenceIds.Count)
                throw new InvalidOperationException("All verification evidence must be active documents uploaded by this company.");

            profile.VerificationDocuments = JsonSerializer.Serialize(evidenceIds);
            profile.AdminNotes = request.Notes;
            profile.VerificationStatus = CompanyVerificationStatus.PendingReview;
            profile.VerificationSubmittedAt = DateTime.UtcNow;
            profile.AppealPending = false;
            profile.UpdatedAt = DateTime.UtcNow;
            AddHistory(profile.Id, CompanyVerificationStatus.PendingReview, "SUBMITTED",
                request.Notes, userId, false, evidenceDocumentIds: evidenceIds);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ReviewVerificationAsync(
            int companyProfileId,
            int reviewerUserId,
            ReviewVerificationRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.ReasonCode))
                throw new ArgumentException("A verification decision reason code is required.", nameof(request));
            if (!VerificationReasonCodes.Contains(request.ReasonCode.Trim()))
                throw new ArgumentException("The verification decision reason code is not recognized.", nameof(request));
            var requiredCriteria = new[] { "EvidenceReviewed", "RegistrationReviewed", "AddressReviewed" };
            if (request.CriteriaAssessment == null
                || requiredCriteria.Any(key => !request.CriteriaAssessment.ContainsKey(key)))
                throw new ArgumentException("Evidence, registration, and address criteria must all be assessed.", nameof(request));

            var profile = await _context.CompanyProfiles
                .FirstOrDefaultAsync(c => c.Id == companyProfileId && !c.IsDeleted);

            if (profile == null) return false;
            if (profile.VerificationStatus != CompanyVerificationStatus.PendingReview)
                throw new InvalidOperationException("This company has no pending verification decision.");

            var decision = ResolveDecision(request.Status, request.Approved);
            if (decision == CompanyVerificationStatus.Verified
                && request.CriteriaAssessment.Values.Any(assessment => !assessment))
                throw new InvalidOperationException("A company cannot be approved until all verification criteria pass.");
            var isAppealDecision = profile.AppealPending;
            if (isAppealDecision)
            {
                var appealSubmittedAt = new DateTimeOffset(
                    DateTime.SpecifyKind(profile.AppealSubmittedAt!.Value, DateTimeKind.Utc));
                var originalDecision = await _context.CompanyVerificationHistories
                    .Where(item => item.CompanyProfileId == companyProfileId
                        && !item.IsAppeal
                        && item.CreatedAt < appealSubmittedAt)
                    .OrderByDescending(item => item.CreatedAt)
                    .FirstOrDefaultAsync();
                if (originalDecision?.ChangedByUserId == reviewerUserId)
                    throw new InvalidOperationException("A different administrator must review an appeal.");
            }

            profile.VerificationStatus = decision;
            profile.AdminNotes = request.Notes;
            if (decision == CompanyVerificationStatus.Verified)
                profile.VerifiedAt = DateTime.UtcNow;
            if (isAppealDecision)
            {
                profile.AppealReviewedAt = DateTime.UtcNow;
                profile.AppealPending = false;
            }

            profile.UpdatedAt = DateTime.UtcNow;
            AddHistory(profile.Id, decision, request.ReasonCode, request.Notes, reviewerUserId,
                isAppealDecision, request.CriteriaAssessment);

            await _context.SaveChangesAsync();

            var verified = decision == CompanyVerificationStatus.Verified;
            await _notifications.NotifyAsync(profile.UserId, NotificationTypes.Verification,
                verified ? "Your company is verified" : "Update on your company verification",
                verified
                    ? "Your company was verified. You can now publish internships."
                    : $"Your verification decision is: {decision}. Open the verification page for the details and next steps.",
                "/Company/Verification", sendEmail: true);
            return true;
        }

        public async Task<bool> SubmitVerificationAppealAsync(int userId, SubmitVerificationAppealRequest request)
        {
            var profile = await _context.CompanyProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);
            if (profile == null)
                return false;

            var now = DateTime.UtcNow;
            var lastDecision = await _context.CompanyVerificationHistories
                .Where(item => item.CompanyProfileId == profile.Id && !item.IsAppeal)
                .OrderByDescending(item => item.CreatedAt)
                .FirstOrDefaultAsync();
            if (profile.VerificationStatus != CompanyVerificationStatus.Rejected
                || profile.HasUsedVerificationAppeal
                || lastDecision == null
                || now - lastDecision.CreatedAt > TimeSpan.FromDays(30))
                throw new InvalidOperationException("This company is not eligible to appeal its verification decision.");

            profile.HasUsedVerificationAppeal = true;
            profile.AppealPending = true;
            profile.AppealSubmittedAt = now;
            profile.AppealReviewedAt = null;
            profile.VerificationStatus = CompanyVerificationStatus.PendingReview;
            profile.UpdatedAt = now;
            AddHistory(profile.Id, CompanyVerificationStatus.PendingReview, "APPEAL_SUBMITTED",
                request.Reason, userId, true);
            await _context.SaveChangesAsync();
            return true;
        }

        public Task<bool> ReviewVerificationAppealAsync(
            int companyProfileId,
            int reviewerUserId,
            ReviewVerificationAppealRequest request) =>
            ReviewVerificationAsync(companyProfileId, reviewerUserId, new ReviewVerificationRequest
            {
                Approved = request.Approved,
                Status = request.Approved
                    ? nameof(CompanyVerificationStatus.Verified)
                    : nameof(CompanyVerificationStatus.Rejected),
                ReasonCode = request.ReasonCode,
                Notes = request.Notes,
                CriteriaAssessment = request.CriteriaAssessment
            });

        public async Task<IReadOnlyList<CompanyVerificationHistory>> GetVerificationHistoryAsync(int companyProfileId) =>
            await _context.CompanyVerificationHistories
                .Where(item => item.CompanyProfileId == companyProfileId)
                .OrderBy(item => item.CreatedAt)
                .ToListAsync();

        public async Task<IReadOnlyList<PrivateDocumentResponse>> GetVerificationEvidenceAsync(int companyProfileId)
        {
            var company = await _context.CompanyProfiles
                .FirstOrDefaultAsync(profile => profile.Id == companyProfileId
                    && !profile.IsDeleted
                    && profile.VerificationStatus == CompanyVerificationStatus.PendingReview);
            if (company == null)
                return Array.Empty<PrivateDocumentResponse>();
            if (string.IsNullOrWhiteSpace(company.VerificationDocuments))
                return Array.Empty<PrivateDocumentResponse>();

            List<Guid> documentIds;
            try
            {
                documentIds = JsonSerializer.Deserialize<List<Guid>>(company.VerificationDocuments)
                    ?? new List<Guid>();
            }
            catch (JsonException exception)
            {
                throw new InvalidOperationException("Stored verification evidence references are invalid.", exception);
            }

            return await _context.PrivateDocuments
                .Where(document => documentIds.Contains(document.Id)
                    && document.OwnerUserId == company.UserId
                    && document.Category == "CompanyEvidence"
                    && document.RetentionState == DocumentRetentionState.Active)
                .Select(document => new PrivateDocumentResponse(
                    document.Id,
                    document.OriginalFileName,
                    document.Category,
                    document.ContentType,
                    document.Length,
                    document.RetentionState.ToString(),
                    document.CreatedAt))
                .ToListAsync();
        }

        public async Task<VerificationSlaResponse> GetVerificationSlaAsync()
        {
            var since = DateTimeOffset.UtcNow.AddYears(-1);
            var history = await _context.CompanyVerificationHistories
                .AsNoTracking()
                .Where(item => item.CreatedAt >= since)
                .OrderBy(item => item.CreatedAt)
                .ToListAsync();

            var submissions = history
                .Where(item => item.ReasonCode == "SUBMITTED" || item.ReasonCode == "APPEAL_SUBMITTED")
                .ToList();
            var measured = 0;
            var withinTarget = 0;
            foreach (var submission in submissions)
            {
                var decision = history.FirstOrDefault(item =>
                    item.CompanyProfileId == submission.CompanyProfileId
                    && item.CreatedAt > submission.CreatedAt
                    && (item.Status == CompanyVerificationStatus.Verified
                        || item.Status == CompanyVerificationStatus.Rejected
                        || item.Status == CompanyVerificationStatus.MoreInformationRequested
                        || item.Status == CompanyVerificationStatus.Suspended));
                if (decision == null)
                    continue;

                measured++;
                if (BusinessDaysBetween(submission.CreatedAt, decision.CreatedAt) <= 3)
                    withinTarget++;
            }

            // Appeals must be decided within 5 business days: list the ones that are still waiting past that.
            var now = DateTimeOffset.UtcNow;
            var openAppeals = await _context.CompanyProfiles
                .AsNoTracking()
                .Where(profile => profile.AppealPending && profile.AppealSubmittedAt != null)
                .Select(profile => new { profile.Id, profile.CompanyName, profile.AppealSubmittedAt })
                .ToListAsync();
            var overdueAppeals = openAppeals
                .Select(appeal => new OverdueAppealResponse(
                    appeal.Id,
                    appeal.CompanyName,
                    DateTime.SpecifyKind(appeal.AppealSubmittedAt!.Value, DateTimeKind.Utc),
                    BusinessDaysBetween(DateTime.SpecifyKind(appeal.AppealSubmittedAt!.Value, DateTimeKind.Utc), now)))
                .Where(appeal => appeal.BusinessDaysWaiting > 5)
                .OrderByDescending(appeal => appeal.BusinessDaysWaiting)
                .ToList();

            return new VerificationSlaResponse(
                measured,
                withinTarget,
                measured == 0 ? 100 : Math.Round(withinTarget * 100d / measured, 2),
                90,
                overdueAppeals);
        }

        public async Task<int> UpdateAgedVerificationsAsync()
        {
            var now = DateTime.UtcNow;
            var ageCutoff = now.AddMonths(-24);
            var subscriptionCutoff = now.AddMonths(-12);
            var candidates = await _context.CompanyProfiles
                .Where(profile => !profile.IsDeleted
                    && profile.VerificationStatus == CompanyVerificationStatus.Verified
                    && ((profile.VerifiedAt != null && profile.VerifiedAt < ageCutoff)
                        || (!profile.IsSubscribed
                            && ((profile.SubscriptionInactiveAt != null
                                    && profile.SubscriptionInactiveAt < subscriptionCutoff)
                                || (profile.SubscriptionInactiveAt == null
                                    && profile.SubscriptionEndDate != null
                                    && profile.SubscriptionEndDate < subscriptionCutoff)))))
                .ToListAsync();

            foreach (var profile in candidates)
            {
                profile.VerificationStatus = CompanyVerificationStatus.ReverificationRequired;
                profile.ReverificationRequiredAt = now;
                profile.UpdatedAt = now;
                AddHistory(profile.Id, CompanyVerificationStatus.ReverificationRequired,
                    "PERIODIC_REVERIFICATION", "Periodic re-verification is due.", null, false);
            }

            if (candidates.Count > 0)
                await _context.SaveChangesAsync();
            return candidates.Count;
        }

        public async Task<bool> UpdateSubscriptionStatusAsync(int companyProfileId, bool isSubscribed)
        {
            var profile = await _context.CompanyProfiles
                .FirstOrDefaultAsync(c => c.Id == companyProfileId && !c.IsDeleted);

            if (profile == null) return false;

            profile.IsSubscribed = isSubscribed;
            profile.UpdatedAt = DateTime.UtcNow;

            if (isSubscribed)
            {
                profile.SubscriptionStartDate = DateTime.UtcNow;
                profile.SubscriptionEndDate = DateTime.UtcNow.AddMonths(1);
                profile.SubscriptionInactiveAt = null;
            }
            else
            {
                profile.SubscriptionInactiveAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        private async Task<CompanyProfileResponse> MapToResponse(CompanyProfile profile)
        {
            var activeInternships = profile.Internships?
                .Count(i => i.Status == InternshipStatus.Published && !i.IsDeleted) ?? 0;

            var totalInternships = profile.Internships?
                .Count(i => !i.IsDeleted) ?? 0;

            var totalApplications = await _context.InternshipApplications
                .CountAsync(a => a.Internship.CompanyProfileId == profile.Id && !a.IsDeleted);

            return new CompanyProfileResponse
            {
                Id = profile.Id,
                CompanyName = profile.CompanyName,
                LegalName = profile.LegalName,
                RegistrationNumber = profile.RegistrationNumber,
                Description = profile.Description,
                Industry = profile.Industry,
                Website = profile.Website,
                LinkedInUrl = profile.LinkedInUrl,
                LogoUrl = profile.LogoUrl,
                Address = profile.Address,
                City = profile.City,
                Country = profile.Country,
                PhoneNumber = profile.PhoneNumber,
                VerificationStatus = profile.VerificationStatus.ToString(),
                HasUsedVerificationAppeal = profile.HasUsedVerificationAppeal,
                AppealPending = profile.AppealPending,
                AppealSubmittedAt = profile.AppealSubmittedAt,
                IsSubscribed = profile.IsSubscribed,
                SubscriptionStartDate = profile.SubscriptionStartDate,
                SubscriptionEndDate = profile.SubscriptionEndDate,
                ActiveInternships = activeInternships,
                TotalInternships = totalInternships,
                TotalApplications = totalApplications
            };
        }

        private void AddHistory(
            int companyProfileId,
            CompanyVerificationStatus status,
            string? reasonCode,
            string? notes,
            int? changedByUserId,
            bool isAppeal,
            Dictionary<string, bool>? criteriaAssessment = null,
            IReadOnlyCollection<Guid>? evidenceDocumentIds = null)
        {
            _context.CompanyVerificationHistories.Add(new CompanyVerificationHistory
            {
                CompanyProfileId = companyProfileId,
                Status = status,
                ReasonCode = reasonCode,
                Notes = notes,
                CriteriaAssessmentJson = criteriaAssessment == null
                    ? null
                    : JsonSerializer.Serialize(criteriaAssessment),
                EvidenceDocumentIdsJson = evidenceDocumentIds == null
                    ? null
                    : JsonSerializer.Serialize(evidenceDocumentIds),
                ChangedByUserId = changedByUserId,
                IsAppeal = isAppeal,
                CreatedAt = DateTimeOffset.UtcNow
            });
        }

        private static CompanyVerificationStatus ResolveDecision(string? requestedStatus, bool approved)
        {
            if (!string.IsNullOrWhiteSpace(requestedStatus))
            {
                if (Enum.TryParse<CompanyVerificationStatus>(requestedStatus, true, out var parsed)
                    && parsed is CompanyVerificationStatus.Verified
                        or CompanyVerificationStatus.Rejected
                        or CompanyVerificationStatus.MoreInformationRequested
                        or CompanyVerificationStatus.Suspended)
                    return parsed;
                throw new ArgumentException("The verification decision status is invalid.", nameof(requestedStatus));
            }

            return approved ? CompanyVerificationStatus.Verified : CompanyVerificationStatus.Rejected;
        }

        private static int BusinessDaysBetween(DateTimeOffset start, DateTimeOffset end)
        {
            var businessDays = 0;
            for (var date = start.Date.AddDays(1); date <= end.Date; date = date.AddDays(1))
            {
                if (date.DayOfWeek is not DayOfWeek.Saturday and not DayOfWeek.Sunday)
                    businessDays++;
            }
            return businessDays;
        }
    }
}