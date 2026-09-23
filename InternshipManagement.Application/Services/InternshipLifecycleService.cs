using System.Text.Json;
using InternshipManagement.Application.DTOs.Lifecycle;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Application.Services
{
    public class InternshipLifecycleService : IInternshipLifecycleService
    {
        private readonly IApplicationDbContext _context;

        public InternshipLifecycleService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<PlacementResponse?> CreatePlacementFromAcceptedApplicationAsync(int applicationId)
        {
            var application = await _context.InternshipApplications
                .Include(a => a.StudentProfile)
                .Include(a => a.Internship)
                    .ThenInclude(i => i.CompanyProfile)
                .FirstOrDefaultAsync(a => a.Id == applicationId);

            if (application == null || application.Status != ApplicationStatus.Accepted)
                return null;

            var existingPlacement = await _context.Placements
                .Include(p => p.StudentProfile)
                .Include(p => p.CompanyProfile)
                .Include(p => p.Internship)
                .FirstOrDefaultAsync(p => p.InternshipApplicationId == applicationId);

            if (existingPlacement != null)
                return MapPlacement(existingPlacement);

            var hasActivePlacement = await _context.Placements.AnyAsync(p =>
                p.StudentProfileId == application.StudentProfileId &&
                (p.Status == PlacementStatus.Pending || p.Status == PlacementStatus.Active || p.Status == PlacementStatus.Extended));

            if (hasActivePlacement)
                throw new Exception("Student already has an active or pending placement. Terminate or complete it before creating another placement.");

            var startDate = application.OfferStartDate ?? application.Internship.StartDate;
            var placement = new Placement
            {
                StudentProfileId = application.StudentProfileId,
                CompanyProfileId = application.Internship.CompanyProfileId,
                InternshipId = application.InternshipId,
                InternshipApplicationId = application.Id,
                Status = PlacementStatus.Active,
                OriginalStartDate = startDate,
                OriginalEndDate = application.Internship.EndDate,
                CurrentStartDate = startDate,
                CurrentEndDate = application.Internship.EndDate,
                ActivatedAt = DateTime.UtcNow
            };

            _context.Placements.Add(placement);
            await _context.SaveChangesAsync();

            return MapPlacement(placement);
        }

        public async Task<List<PlacementResponse>> GetStudentPlacementsAsync(int userId)
        {
            var studentProfile = await _context.StudentProfiles.FirstOrDefaultAsync(s => s.UserId == userId);
            if (studentProfile == null) return new List<PlacementResponse>();

            var placements = await PlacementQuery()
                .Where(p => p.StudentProfileId == studentProfile.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return placements.Select(MapPlacement).ToList();
        }

        public async Task<List<PlacementResponse>> GetCompanyPlacementsAsync(int userId)
        {
            var companyProfile = await _context.CompanyProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
            if (companyProfile == null) return new List<PlacementResponse>();

            var placements = await PlacementQuery()
                .Where(p => p.CompanyProfileId == companyProfile.Id)
                .OrderByDescending(p => p.CreatedAt)
                .ToListAsync();

            return placements.Select(MapPlacement).ToList();
        }

        public async Task<PlacementResponse?> GetPlacementAsync(int placementId)
        {
            var placement = await PlacementQuery().FirstOrDefaultAsync(p => p.Id == placementId);
            return placement == null ? null : MapPlacement(placement);
        }

        public async Task<List<ApplicationStatusHistoryResponse>> GetApplicationStatusHistoryAsync(int applicationId)
        {
            var history = await _context.ApplicationStatusHistories
                .Where(h => h.InternshipApplicationId == applicationId)
                .OrderBy(h => h.ChangedAt)
                .ToListAsync();

            return history.Select(h => new ApplicationStatusHistoryResponse
            {
                Id = h.Id,
                InternshipApplicationId = h.InternshipApplicationId,
                PreviousStatus = h.PreviousStatus.ToString(),
                NewStatus = h.NewStatus.ToString(),
                ChangedAt = h.ChangedAt,
                ChangedByUserId = h.ChangedByUserId,
                Notes = h.Notes
            }).ToList();
        }

        public async Task<ProgressReportResponse> SubmitProgressReportAsync(int userId, int placementId, SubmitProgressReportRequest request)
        {
            var placement = await GetStudentPlacementForUser(userId, placementId);
            if (placement == null)
                throw new Exception("Placement not found for this student.");

            if (placement.Status != PlacementStatus.Active && placement.Status != PlacementStatus.Extended)
                throw new Exception("Progress reports can only be submitted for active placements.");

            if (request.PeriodEnd < request.PeriodStart)
                throw new Exception("Report period end date cannot be before the start date.");

            var report = new ProgressReport
            {
                PlacementId = placementId,
                PeriodStart = request.PeriodStart,
                PeriodEnd = request.PeriodEnd,
                WorkCompleted = request.WorkCompleted,
                SkillsLearned = request.SkillsLearned,
                Challenges = request.Challenges,
                Achievements = request.Achievements,
                Comments = request.Comments,
                SupportingDocuments = request.SupportingDocumentUrls != null ? JsonSerializer.Serialize(request.SupportingDocumentUrls) : null,
                Status = ProgressReportStatus.Submitted
            };

            _context.ProgressReports.Add(report);
            await _context.SaveChangesAsync();
            return MapProgressReport(report);
        }

        public async Task<List<ProgressReportResponse>> GetProgressReportsAsync(int placementId)
        {
            var reports = await _context.ProgressReports
                .Where(r => r.PlacementId == placementId)
                .OrderByDescending(r => r.PeriodEnd)
                .ToListAsync();

            return reports.Select(MapProgressReport).ToList();
        }

        public async Task<bool> ReviewProgressReportAsync(int userId, int reportId, ReviewProgressReportRequest request)
        {
            var report = await _context.ProgressReports.Include(r => r.Placement).FirstOrDefaultAsync(r => r.Id == reportId);
            if (report == null) return false;

            await EnsureCompanyOwnsPlacement(userId, report.PlacementId);

            report.CompanyFeedback = request.Feedback;
            report.Status = ProgressReportStatus.Reviewed;
            report.ReviewedAt = DateTime.UtcNow;
            report.ReviewedByUserId = userId;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<ExtensionRequestResponse> ProposeExtensionAsync(int userId, int placementId, ProposeExtensionRequest request)
        {
            var placement = await _context.Placements.FirstOrDefaultAsync(p => p.Id == placementId);
            if (placement == null)
                throw new Exception("Placement not found.");

            await EnsureCompanyOwnsPlacement(userId, placementId);

            if (placement.Status != PlacementStatus.Active && placement.Status != PlacementStatus.Extended)
                throw new Exception("Only active placements can be extended.");

            if (placement.CurrentEndDate.HasValue && request.ProposedEndDate <= placement.CurrentEndDate.Value)
                throw new Exception("Proposed end date must be after the current end date.");

            var extension = new InternshipExtensionRequest
            {
                PlacementId = placementId,
                ProposedEndDate = request.ProposedEndDate,
                Reason = request.Reason,
                ProposedByUserId = userId,
                Status = ExtensionStatus.Proposed
            };

            _context.InternshipExtensionRequests.Add(extension);
            await _context.SaveChangesAsync();
            return MapExtension(extension);
        }

        public async Task<bool> RespondToExtensionAsync(int userId, int extensionRequestId, RespondToExtensionRequest request)
        {
            var extension = await _context.InternshipExtensionRequests
                .Include(e => e.Placement)
                .FirstOrDefaultAsync(e => e.Id == extensionRequestId);

            if (extension == null) return false;

            var studentPlacement = await GetStudentPlacementForUser(userId, extension.PlacementId);
            if (studentPlacement == null)
                throw new Exception("Only the assigned student can respond to an extension request.");

            if (extension.Status != ExtensionStatus.Proposed)
                throw new Exception("This extension request has already been answered.");

            extension.Status = request.Accepted ? ExtensionStatus.Accepted : ExtensionStatus.Rejected;
            extension.RespondedAt = DateTime.UtcNow;
            extension.RespondedByUserId = userId;
            extension.ResponseNotes = request.Notes;

            if (request.Accepted)
            {
                extension.Placement.CurrentEndDate = extension.ProposedEndDate;
                extension.Placement.Status = PlacementStatus.Extended;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<WithdrawalRequestResponse> RequestWithdrawalAsync(int userId, int placementId, RequestWithdrawalRequest request)
        {
            var placement = await GetStudentPlacementForUser(userId, placementId);
            if (placement == null)
                throw new Exception("Placement not found for this student.");

            if (placement.Status != PlacementStatus.Active && placement.Status != PlacementStatus.Extended)
                throw new Exception("Only active placements can be withdrawn.");

            var existingOpenRequest = await _context.InternshipWithdrawalRequests
                .AnyAsync(w => w.PlacementId == placementId && w.Status == WithdrawalStatus.Requested);

            if (existingOpenRequest)
                throw new Exception("There is already an open withdrawal request for this placement.");

            var withdrawal = new InternshipWithdrawalRequest
            {
                PlacementId = placementId,
                RequestedByUserId = userId,
                Reason = request.Reason,
                Status = WithdrawalStatus.Requested
            };

            _context.InternshipWithdrawalRequests.Add(withdrawal);
            await _context.SaveChangesAsync();
            return MapWithdrawal(withdrawal);
        }

        public async Task<bool> ReviewWithdrawalAsync(int userId, int withdrawalRequestId, ReviewWithdrawalRequest request)
        {
            var withdrawal = await _context.InternshipWithdrawalRequests
                .Include(w => w.Placement)
                .FirstOrDefaultAsync(w => w.Id == withdrawalRequestId);

            if (withdrawal == null) return false;

            await EnsureCompanyOwnsPlacement(userId, withdrawal.PlacementId);

            if (withdrawal.Status != WithdrawalStatus.Requested)
                throw new Exception("This withdrawal request has already been reviewed.");

            withdrawal.Status = request.Approved ? WithdrawalStatus.Approved : WithdrawalStatus.Rejected;
            withdrawal.ReviewedAt = DateTime.UtcNow;
            withdrawal.ReviewedByUserId = userId;
            withdrawal.CompanyDecisionNotes = request.Notes;

            if (request.Approved)
            {
                withdrawal.EffectiveTerminationDate = request.EffectiveTerminationDate ?? DateTime.UtcNow;
                withdrawal.Placement.Status = PlacementStatus.Terminated;
                withdrawal.Placement.TerminatedAt = withdrawal.EffectiveTerminationDate;
                withdrawal.Placement.TerminationReason = withdrawal.Reason;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CompletePlacementAsync(int userId, int placementId, CompletePlacementRequest request)
        {
            var placement = await _context.Placements.FirstOrDefaultAsync(p => p.Id == placementId);
            if (placement == null) return false;

            await EnsureCompanyOwnsPlacement(userId, placementId);

            if (placement.Status != PlacementStatus.Active && placement.Status != PlacementStatus.Extended)
                throw new Exception("Only active placements can be completed.");

            placement.Status = PlacementStatus.Completed;
            placement.CompletedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> TerminatePlacementAsync(int userId, int placementId, TerminatePlacementRequest request)
        {
            var placement = await _context.Placements.FirstOrDefaultAsync(p => p.Id == placementId);
            if (placement == null) return false;

            await EnsureCompanyOwnsPlacement(userId, placementId);

            if (placement.Status == PlacementStatus.Completed || placement.Status == PlacementStatus.Terminated)
                throw new Exception("Placement is already closed.");

            placement.Status = PlacementStatus.Terminated;
            placement.TerminatedAt = request.EffectiveTerminationDate ?? DateTime.UtcNow;
            placement.TerminationReason = request.Reason;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<EvaluationResponse> SubmitEvaluationAsync(int userId, int placementId, SubmitEvaluationRequest request)
        {
            await EnsureCompanyOwnsPlacement(userId, placementId);
            ValidateRatings(request);

            var placement = await _context.Placements.FirstOrDefaultAsync(p => p.Id == placementId);
            if (placement == null)
                throw new Exception("Placement not found.");

            if (placement.Status != PlacementStatus.Completed && placement.Status != PlacementStatus.Terminated)
                throw new Exception("Evaluations can only be submitted after completion or termination.");

            var evaluation = new InternshipEvaluation
            {
                PlacementId = placementId,
                EvaluatorUserId = userId,
                TechnicalSkillsRating = request.TechnicalSkillsRating,
                CommunicationRating = request.CommunicationRating,
                TeamworkRating = request.TeamworkRating,
                ProblemSolvingRating = request.ProblemSolvingRating,
                ProfessionalismRating = request.ProfessionalismRating,
                ReliabilityRating = request.ReliabilityRating,
                OverallPerformanceRating = request.OverallPerformanceRating,
                Comments = request.Comments
            };

            _context.InternshipEvaluations.Add(evaluation);
            await _context.SaveChangesAsync();
            return MapEvaluation(evaluation);
        }

        public async Task<List<EvaluationResponse>> GetEvaluationsAsync(int placementId)
        {
            var evaluations = await _context.InternshipEvaluations
                .Where(e => e.PlacementId == placementId)
                .OrderByDescending(e => e.SubmittedAt)
                .ToListAsync();

            return evaluations.Select(MapEvaluation).ToList();
        }

        private IQueryable<Placement> PlacementQuery()
        {
            return _context.Placements
                .Include(p => p.StudentProfile)
                .Include(p => p.CompanyProfile)
                .Include(p => p.Internship);
        }

        private async Task<Placement?> GetStudentPlacementForUser(int userId, int placementId)
        {
            var studentProfile = await _context.StudentProfiles.FirstOrDefaultAsync(s => s.UserId == userId);
            if (studentProfile == null) return null;

            return await _context.Placements
                .FirstOrDefaultAsync(p => p.Id == placementId && p.StudentProfileId == studentProfile.Id);
        }

        private async Task EnsureCompanyOwnsPlacement(int userId, int placementId)
        {
            var companyProfile = await _context.CompanyProfiles.FirstOrDefaultAsync(c => c.UserId == userId);
            if (companyProfile == null)
                throw new Exception("Company profile not found.");

            var ownsPlacement = await _context.Placements.AnyAsync(p => p.Id == placementId && p.CompanyProfileId == companyProfile.Id);
            if (!ownsPlacement)
                throw new Exception("Placement not found for this company.");
        }

        private static void ValidateRatings(SubmitEvaluationRequest request)
        {
            var ratings = new[]
            {
                request.TechnicalSkillsRating,
                request.CommunicationRating,
                request.TeamworkRating,
                request.ProblemSolvingRating,
                request.ProfessionalismRating,
                request.ReliabilityRating,
                request.OverallPerformanceRating
            };

            if (ratings.Any(r => r < 1 || r > 5))
                throw new Exception("Evaluation ratings must be between 1 and 5.");
        }

        private static PlacementResponse MapPlacement(Placement placement)
        {
            return new PlacementResponse
            {
                Id = placement.Id,
                StudentProfileId = placement.StudentProfileId,
                CompanyProfileId = placement.CompanyProfileId,
                InternshipId = placement.InternshipId,
                InternshipApplicationId = placement.InternshipApplicationId,
                InternshipTitle = placement.Internship?.Title ?? string.Empty,
                CompanyName = placement.CompanyProfile?.CompanyName ?? string.Empty,
                StudentName = placement.StudentProfile == null ? string.Empty : $"{placement.StudentProfile.FirstName} {placement.StudentProfile.LastName}".Trim(),
                Status = placement.Status.ToString(),
                OriginalStartDate = placement.OriginalStartDate,
                OriginalEndDate = placement.OriginalEndDate,
                CurrentStartDate = placement.CurrentStartDate,
                CurrentEndDate = placement.CurrentEndDate,
                ActivatedAt = placement.ActivatedAt,
                CompletedAt = placement.CompletedAt,
                TerminatedAt = placement.TerminatedAt,
                TerminationReason = placement.TerminationReason
            };
        }

        private static ProgressReportResponse MapProgressReport(ProgressReport report)
        {
            return new ProgressReportResponse
            {
                Id = report.Id,
                PlacementId = report.PlacementId,
                PeriodStart = report.PeriodStart,
                PeriodEnd = report.PeriodEnd,
                WorkCompleted = report.WorkCompleted,
                SkillsLearned = report.SkillsLearned,
                Challenges = report.Challenges,
                Achievements = report.Achievements,
                Comments = report.Comments,
                SupportingDocumentUrls = string.IsNullOrWhiteSpace(report.SupportingDocuments)
                    ? new List<string>()
                    : JsonSerializer.Deserialize<List<string>>(report.SupportingDocuments) ?? new List<string>(),
                Status = report.Status.ToString(),
                CompanyFeedback = report.CompanyFeedback,
                ReviewedAt = report.ReviewedAt
            };
        }

        private static ExtensionRequestResponse MapExtension(InternshipExtensionRequest extension)
        {
            return new ExtensionRequestResponse
            {
                Id = extension.Id,
                PlacementId = extension.PlacementId,
                ProposedEndDate = extension.ProposedEndDate,
                Reason = extension.Reason,
                Status = extension.Status.ToString(),
                ProposedByUserId = extension.ProposedByUserId,
                RespondedAt = extension.RespondedAt,
                ResponseNotes = extension.ResponseNotes
            };
        }

        private static WithdrawalRequestResponse MapWithdrawal(InternshipWithdrawalRequest withdrawal)
        {
            return new WithdrawalRequestResponse
            {
                Id = withdrawal.Id,
                PlacementId = withdrawal.PlacementId,
                Reason = withdrawal.Reason,
                Status = withdrawal.Status.ToString(),
                EffectiveTerminationDate = withdrawal.EffectiveTerminationDate,
                CompanyDecisionNotes = withdrawal.CompanyDecisionNotes,
                ReviewedAt = withdrawal.ReviewedAt
            };
        }

        private static EvaluationResponse MapEvaluation(InternshipEvaluation evaluation)
        {
            return new EvaluationResponse
            {
                Id = evaluation.Id,
                PlacementId = evaluation.PlacementId,
                EvaluatorUserId = evaluation.EvaluatorUserId,
                TechnicalSkillsRating = evaluation.TechnicalSkillsRating,
                CommunicationRating = evaluation.CommunicationRating,
                TeamworkRating = evaluation.TeamworkRating,
                ProblemSolvingRating = evaluation.ProblemSolvingRating,
                ProfessionalismRating = evaluation.ProfessionalismRating,
                ReliabilityRating = evaluation.ReliabilityRating,
                OverallPerformanceRating = evaluation.OverallPerformanceRating,
                Comments = evaluation.Comments,
                SubmittedAt = evaluation.SubmittedAt
            };
        }
    }
}
