using Microsoft.EntityFrameworkCore;
using InternshipManagement.Application.DTOs.Application;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;

namespace InternshipManagement.Application.Services
{
    public class ApplicationService : IApplicationService
    {
        private readonly IApplicationDbContext _context;
        private readonly IInternshipService _internshipService;
        private readonly IInternshipLifecycleService _lifecycleService;
        private readonly INotificationService _notifications;

        public ApplicationService(
            IApplicationDbContext context,
            IInternshipService internshipService,
            IInternshipLifecycleService lifecycleService,
            INotificationService notifications)
        {
            _context = context;
            _internshipService = internshipService;
            _lifecycleService = lifecycleService;
            _notifications = notifications;
        }

        public async Task<ApplicationResponse> ApplyAsync(int studentId, ApplyRequest request)
        {
            var canApply = await _internshipService.CanUserApplyAsync(request.InternshipId, studentId);
            if (!canApply)
                throw new Exception("Cannot apply to this internship.");

            var studentProfile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == studentId);

            if (studentProfile == null)
                throw new Exception("Student profile not found.");

            var documentIds = (request.AdditionalDocumentIds ?? new List<Guid>()).Distinct().ToList();
            if (documentIds.Count > 5)
                throw new InvalidOperationException("Maximum 5 additional documents allowed.");
            var ownedDocuments = await _context.PrivateDocuments.CountAsync(document =>
                documentIds.Contains(document.Id)
                && document.OwnerUserId == studentId
                && document.RetentionState == DocumentRetentionState.Active
                && document.Category != "CompanyEvidence");
            if (ownedDocuments != documentIds.Count)
                throw new InvalidOperationException("Applications can only include active documents uploaded by this student.");

            // ✅ Use InternshipApplication (singular) - NOT InternshipApplications
            var application = new InternshipApplication
            {
                StudentProfileId = studentProfile.Id,
                InternshipId = request.InternshipId,
                CoverLetter = request.CoverLetter,
                AdditionalDocuments = documentIds.Count > 0
                    ? System.Text.Json.JsonSerializer.Serialize(documentIds)
                    : null,
                Status = ApplicationStatus.Applied,
                StatusUpdatedAt = DateTime.UtcNow
            };

            _context.InternshipApplications.Add(application);
            await _context.SaveChangesAsync();
            AddStatusHistory(application, ApplicationStatus.Applied, ApplicationStatus.Applied, "Application submitted", studentId);

            var internship = await _context.Internships.FindAsync(request.InternshipId);
            if (internship != null)
            {
                internship.ApplicationsCount++;
            }

            await _context.SaveChangesAsync();

            if (internship != null)
            {
                var companyUserId = await _context.CompanyProfiles
                    .Where(c => c.Id == internship.CompanyProfileId)
                    .Select(c => c.UserId)
                    .FirstOrDefaultAsync();
                await _notifications.NotifyAsync(companyUserId, NotificationTypes.Application,
                    "New application",
                    $"{studentProfile.FirstName} {studentProfile.LastName} applied for \"{internship.Title}\".",
                    $"/Company/ApplicationReview/{application.Id}");
                await _notifications.NotifyAsync(studentId, NotificationTypes.Application,
                    "Application submitted",
                    $"Your application for \"{internship.Title}\" was submitted. You will be told when its status changes.",
                    $"/Student/ApplicationDetails/{application.Id}");
            }

            return await MapToResponse(application);
        }

        public async Task<ApplicationResponse> GetApplicationByIdAsync(int applicationId)
        {
            // ✅ Use InternshipApplication (singular)
            var application = await _context.InternshipApplications
                .Include(a => a.StudentProfile)
                .Include(a => a.Internship)
                    .ThenInclude(i => i.CompanyProfile)
                .FirstOrDefaultAsync(a => a.Id == applicationId && !a.IsDeleted);

            if (application == null) return null;

            return await MapToResponse(application);
        }

        public async Task<List<ApplicationResponse>> GetStudentApplicationsAsync(int studentId)
        {
            var studentProfile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == studentId);

            if (studentProfile == null) return new List<ApplicationResponse>();

            var applications = await _context.InternshipApplications
                .Include(a => a.Internship)
                    .ThenInclude(i => i.CompanyProfile)
                .Where(a => a.StudentProfileId == studentProfile.Id && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var responses = new List<ApplicationResponse>();
            foreach (var app in applications)
            {
                responses.Add(await MapToResponse(app));
            }

            return responses;
        }

        public async Task<List<ApplicationResponse>> GetInternshipApplicationsAsync(int internshipId)
        {
            var applications = await _context.InternshipApplications
                .Include(a => a.StudentProfile)
                .Where(a => a.InternshipId == internshipId && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .ToListAsync();

            var responses = new List<ApplicationResponse>();
            foreach (var app in applications)
            {
                responses.Add(await MapToResponse(app));
            }

            return responses;
        }

        public async Task<bool> UpdateApplicationStatusAsync(int userId, int applicationId, UpdateApplicationStatusRequest request)
        {
            var application = await FindCompanyApplicationAsync(userId, applicationId);
            if (application == null) return false;

            var newStatus = Enum.Parse<ApplicationStatus>(request.Status);

            if (!IsValidCompanyTransition(application.Status, newStatus))
                throw new Exception($"Cannot transition from {application.Status} to {newStatus}");

            var previousStatus = application.Status;
            application.Status = newStatus;
            application.StatusUpdatedAt = DateTime.UtcNow;
            application.StatusNotes = request.Notes;
            AddStatusHistory(application, previousStatus, newStatus, request.Notes, userId);

            await _context.SaveChangesAsync();

            await NotifyStudentAsync(application, NotificationTypes.Application,
                newStatus == ApplicationStatus.Rejected ? "Update on your application" : "Your application is being reviewed",
                newStatus == ApplicationStatus.Rejected
                    ? $"Your application for \"{application.Internship.Title}\" was not successful."
                    : $"Your application for \"{application.Internship.Title}\" is now under review.",
                sendEmail: newStatus == ApplicationStatus.Rejected);
            return true;
        }

        public async Task<bool> ShortlistApplicationAsync(int userId, int applicationId, string? notes)
        {
            var application = await FindCompanyApplicationAsync(userId, applicationId);
            if (application == null) return false;

            if (application.Status != ApplicationStatus.Applied &&
                application.Status != ApplicationStatus.UnderReview)
                throw new Exception("Only applications that are applied or under review can be shortlisted.");

            application.IsShortlisted = true;
            application.ShortlistedAt = DateTime.UtcNow;
            application.ShortlistNotes = notes;
            var previousStatus = application.Status;
            application.Status = ApplicationStatus.Shortlisted;
            application.StatusUpdatedAt = DateTime.UtcNow;
            AddStatusHistory(application, previousStatus, ApplicationStatus.Shortlisted, notes, userId);

            await _context.SaveChangesAsync();

            await NotifyStudentAsync(application, NotificationTypes.Application,
                "You have been shortlisted",
                $"Good news: you were shortlisted for \"{application.Internship.Title}\".",
                sendEmail: true);
            return true;
        }

        public async Task<bool> ScheduleInterviewAsync(int userId, int applicationId, ScheduleInterviewRequest request)
        {
            var application = await FindCompanyApplicationAsync(userId, applicationId);
            if (application == null) return false;

            if (application.Status != ApplicationStatus.Shortlisted)
                throw new Exception("Only shortlisted applications can be scheduled for interview.");

            application.InterviewDateTime = request.InterviewDateTime;
            application.InterviewType = request.InterviewType;
            application.InterviewLocationOrLink = request.InterviewLocationOrLink;
            application.InterviewNotes = request.InterviewNotes;
            var previousStatus = application.Status;
            application.Status = ApplicationStatus.InterviewScheduled;
            application.StatusUpdatedAt = DateTime.UtcNow;
            AddStatusHistory(application, previousStatus, ApplicationStatus.InterviewScheduled, request.InterviewNotes, userId);

            await _context.SaveChangesAsync();

            await NotifyStudentAsync(application, NotificationTypes.Interview,
                "Interview scheduled",
                $"Your interview for \"{application.Internship.Title}\" is on {request.InterviewDateTime:d MMM yyyy 'at' HH:mm}"
                    + (string.IsNullOrWhiteSpace(request.InterviewLocationOrLink) ? "." : $" ({request.InterviewLocationOrLink}).")
                    + " Check the application for details.",
                sendEmail: true);
            return true;
        }

        public async Task<bool> RescheduleInterviewAsync(int userId, int applicationId, ScheduleInterviewRequest request)
        {
            var application = await FindCompanyApplicationAsync(userId, applicationId);
            if (application == null) return false;

            if (application.Status != ApplicationStatus.InterviewScheduled)
                throw new InvalidOperationException("Only a scheduled interview can be rescheduled.");

            var before = application.InterviewDateTime;
            application.InterviewDateTime = request.InterviewDateTime;
            application.InterviewType = request.InterviewType;
            application.InterviewLocationOrLink = request.InterviewLocationOrLink;
            application.InterviewNotes = request.InterviewNotes;
            application.StatusUpdatedAt = DateTime.UtcNow;

            var note = before.HasValue
                ? $"Interview rescheduled from {before.Value:d MMM yyyy HH:mm} to {request.InterviewDateTime:d MMM yyyy HH:mm}."
                : $"Interview rescheduled to {request.InterviewDateTime:d MMM yyyy HH:mm}.";
            AddStatusHistory(application, ApplicationStatus.InterviewScheduled, ApplicationStatus.InterviewScheduled, note, userId);

            await _context.SaveChangesAsync();

            await NotifyStudentAsync(application, NotificationTypes.Interview,
                "Your interview was rescheduled",
                $"Your interview for \"{application.Internship.Title}\" is now on {request.InterviewDateTime:d MMM yyyy 'at' HH:mm}.",
                sendEmail: true);
            return true;
        }

        public async Task<bool> CancelInterviewAsync(int userId, int applicationId, string? reason)
        {
            var application = await FindCompanyApplicationAsync(userId, applicationId);
            if (application == null) return false;

            if (application.Status != ApplicationStatus.InterviewScheduled)
                throw new InvalidOperationException("Only a scheduled interview can be cancelled.");

            var was = application.InterviewDateTime;
            var why = string.IsNullOrWhiteSpace(reason) ? null : reason.Trim();

            // the candidate goes back to "shortlisted" so a new interview can be set up
            application.InterviewDateTime = null;
            application.InterviewType = null;
            application.InterviewLocationOrLink = null;
            application.InterviewNotes = null;
            application.Status = ApplicationStatus.Shortlisted;
            application.StatusUpdatedAt = DateTime.UtcNow;

            var note = (was.HasValue ? $"Interview on {was.Value:d MMM yyyy HH:mm} cancelled." : "Interview cancelled.")
                + (why == null ? string.Empty : $" Reason: {why}");
            AddStatusHistory(application, ApplicationStatus.InterviewScheduled, ApplicationStatus.Shortlisted, note, userId);

            await _context.SaveChangesAsync();

            await NotifyStudentAsync(application, NotificationTypes.Interview,
                "Your interview was cancelled",
                $"The interview for \"{application.Internship.Title}\" was cancelled." + (why == null ? string.Empty : $" Reason: {why}") + " The company may contact you about a new time.",
                sendEmail: true);
            return true;
        }

        public async Task<bool> MarkInterviewCompletedAsync(int userId, int applicationId)
        {
            var application = await FindCompanyApplicationAsync(userId, applicationId);
            if (application == null) return false;

            if (application.Status != ApplicationStatus.InterviewScheduled)
                throw new Exception("Only scheduled interviews can be marked as completed.");

            var previousStatus = application.Status;
            application.Status = ApplicationStatus.InterviewCompleted;
            application.StatusUpdatedAt = DateTime.UtcNow;
            AddStatusHistory(application, previousStatus, ApplicationStatus.InterviewCompleted, "Interview completed", userId);

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> MakeOfferAsync(int userId, int applicationId, MakeOfferRequest request)
        {
            var application = await FindCompanyApplicationAsync(userId, applicationId);
            if (application == null) return false;

            var isReoffer = application.Status == ApplicationStatus.OfferExpired;
            if (application.Status != ApplicationStatus.InterviewCompleted && !isReoffer)
                throw new Exception("Only interview-completed candidates can receive an offer.");

            if (request.ExpiryDate.Date <= DateTime.UtcNow.Date)
                throw new Exception("The offer expiry date must be in the future.");

            application.OfferStipendAmount = request.StipendAmount;
            application.OfferStartDate = request.StartDate;
            application.OfferDetails = request.OfferDetails;
            application.OfferExpiryDate = request.ExpiryDate;
            var previousStatus = application.Status;
            application.Status = ApplicationStatus.OfferMade;
            application.StatusUpdatedAt = DateTime.UtcNow;
            AddStatusHistory(application, previousStatus, ApplicationStatus.OfferMade, isReoffer ? "Offer sent again" : request.OfferDetails, userId);

            await _context.SaveChangesAsync();

            await NotifyStudentAsync(application, NotificationTypes.Offer,
                isReoffer ? "Your internship offer was sent again" : "You have an internship offer",
                $"{application.Internship.CompanyProfile?.CompanyName ?? "A company"} offered you \"{application.Internship.Title}\". "
                    + (application.OfferStipendAmount.HasValue ? $"Stipend: {application.OfferStipendAmount.Value:N2}. " : "")
                    + (application.OfferStartDate.HasValue ? $"Start date: {application.OfferStartDate.Value:d MMM yyyy}. " : "")
                    + $"Please reply by {request.ExpiryDate:d MMM yyyy}.",
                sendEmail: true,
                linkPath: $"/Offer/Letter/{application.Id}");
            return true;
        }

        public async Task<bool> RespondToOfferAsync(int userId, int applicationId, bool accepted)
        {
            var application = await FindStudentApplicationAsync(userId, applicationId);
            if (application == null) return false;

            if (application.Status != ApplicationStatus.OfferMade)
                throw new Exception("Only offered candidates can accept or decline the offer.");

            if (accepted && application.OfferExpiryDate.HasValue
                && application.OfferExpiryDate.Value.Date < DateTime.UtcNow.Date)
                throw new InvalidOperationException(
                    $"This offer expired on {application.OfferExpiryDate.Value:d MMM yyyy}. Please contact the company.");

            if (accepted)
            {
                var hasAnotherAcceptedOfferAwaitingPlacement = await _context.InternshipApplications
                    .AnyAsync(other => other.StudentProfileId == application.StudentProfileId
                        && other.Id != application.Id
                        && other.Status == ApplicationStatus.Accepted
                        && !_context.Placements.Any(placement =>
                            placement.InternshipApplicationId == other.Id));
                if (hasAnotherAcceptedOfferAwaitingPlacement)
                    throw new InvalidOperationException(
                        "You already have an accepted offer waiting for your current placement to end.");

                application.Status = ApplicationStatus.Accepted;
                application.OfferAcceptedAt = DateTime.UtcNow;
            }
            else
            {
                application.Status = ApplicationStatus.OfferDeclined;
                application.OfferDeclinedAt = DateTime.UtcNow;
            }

            application.StatusUpdatedAt = DateTime.UtcNow;
            AddStatusHistory(application, ApplicationStatus.OfferMade, application.Status, accepted ? "Offer accepted" : "Offer declined", userId);
            await _context.SaveChangesAsync();

            await _notifications.NotifyAsync(application.Internship.CompanyProfile.UserId, NotificationTypes.Offer,
                accepted ? "Offer accepted" : "Offer declined",
                $"{application.StudentProfile.FirstName} {application.StudentProfile.LastName} "
                    + (accepted ? "accepted" : "declined") + $" your offer for \"{application.Internship.Title}\".",
                $"/Company/ApplicationReview/{application.Id}",
                sendEmail: true);

            if (accepted)
            {
                await WithdrawOtherOpenApplicationsAsync(application, userId);
                await _lifecycleService.CreatePlacementFromAcceptedApplicationAsync(application.Id);
            }

            return true;
        }

        public async Task<bool> WithdrawApplicationAsync(int userId, int applicationId)
        {
            var application = await FindStudentApplicationAsync(userId, applicationId);
            if (application == null) return false;

            if (!IsOpenApplication(application.Status))
                throw new Exception("Only open applications can be withdrawn. After accepting an offer, withdraw from the placement instead.");

            var previousStatus = application.Status;
            application.Status = ApplicationStatus.Withdrawn;
            application.StatusUpdatedAt = DateTime.UtcNow;
            AddStatusHistory(application, previousStatus, ApplicationStatus.Withdrawn, "Application withdrawn", userId);

            await _context.SaveChangesAsync();

            await _notifications.NotifyAsync(application.Internship.CompanyProfile.UserId, NotificationTypes.Application,
                "Application withdrawn",
                $"{application.StudentProfile.FirstName} {application.StudentProfile.LastName} withdrew their application for \"{application.Internship.Title}\".",
                $"/Company/ApplicationReview/{application.Id}");
            return true;
        }

        // Tells the student about a change made by the company.
        private async Task NotifyStudentAsync(
            InternshipApplication application, string type, string title, string message, bool sendEmail = false,
            string? linkPath = null)
        {
            var studentUserId = await _context.StudentProfiles
                .Where(s => s.Id == application.StudentProfileId)
                .Select(s => s.UserId)
                .FirstOrDefaultAsync();

            await _notifications.NotifyAsync(studentUserId, type, title, message,
                linkPath ?? $"/Student/ApplicationDetails/{application.Id}", sendEmail);
        }

        private async Task WithdrawOtherOpenApplicationsAsync(InternshipApplication accepted, int userId)
        {
            var others = await _context.InternshipApplications
                .Where(other => other.StudentProfileId == accepted.StudentProfileId
                    && other.Id != accepted.Id
                    && !other.IsDeleted)
                .ToListAsync();

            foreach (var other in others.Where(item => IsOpenApplication(item.Status)))
            {
                var previous = other.Status;
                other.Status = ApplicationStatus.WithdrawnAcceptedElsewhere;
                other.StatusUpdatedAt = DateTime.UtcNow;
                AddStatusHistory(other, previous, ApplicationStatus.WithdrawnAcceptedElsewhere,
                    "The student accepted another offer", userId);
            }

            await _context.SaveChangesAsync();
        }

        private void AddStatusHistory(
            InternshipApplication application,
            ApplicationStatus previousStatus,
            ApplicationStatus newStatus,
            string? notes,
            int changedByUserId)
        {
            _context.ApplicationStatusHistories.Add(new ApplicationStatusHistory
            {
                InternshipApplicationId = application.Id,
                PreviousStatus = previousStatus,
                NewStatus = newStatus,
                ChangedAt = DateTime.UtcNow,
                ChangedByUserId = changedByUserId,
                Notes = notes
            });
        }

        private Task<InternshipApplication?> FindCompanyApplicationAsync(int userId, int applicationId) =>
            _context.InternshipApplications
                .Include(a => a.Internship)
                    .ThenInclude(i => i.CompanyProfile)
                .FirstOrDefaultAsync(a => a.Id == applicationId
                    && !a.IsDeleted
                    && a.Internship.CompanyProfile.UserId == userId
                    && !a.Internship.CompanyProfile.IsDeleted);

        private Task<InternshipApplication?> FindStudentApplicationAsync(int userId, int applicationId) =>
            _context.InternshipApplications
                .Include(a => a.StudentProfile)
                .Include(a => a.Internship)
                    .ThenInclude(i => i.CompanyProfile)
                .FirstOrDefaultAsync(a => a.Id == applicationId
                    && !a.IsDeleted
                    && a.StudentProfile.UserId == userId);

        public async Task<List<ApplicationResponse>> GetShortlistedApplicationsAsync(int internshipId)
        {
            var applications = await _context.InternshipApplications
                .Include(a => a.StudentProfile)
                .Where(a => a.InternshipId == internshipId &&
                           a.IsShortlisted &&
                           !a.IsDeleted)
                .OrderByDescending(a => a.ShortlistedAt)
                .ToListAsync();

            var responses = new List<ApplicationResponse>();
            foreach (var app in applications)
            {
                responses.Add(await MapToResponse(app));
            }

            return responses;
        }

        // What a company may do through the generic status update. Accepting or declining an offer belongs to the student
        // (RespondToOfferAsync) and withdrawing belongs to the student (WithdrawApplicationAsync), so neither is allowed here.
        private bool IsValidCompanyTransition(ApplicationStatus current, ApplicationStatus newStatus) => current switch
        {
            ApplicationStatus.Applied => newStatus is ApplicationStatus.UnderReview or ApplicationStatus.Rejected,
            ApplicationStatus.UnderReview => newStatus is ApplicationStatus.Shortlisted or ApplicationStatus.Rejected,
            ApplicationStatus.Shortlisted => newStatus == ApplicationStatus.Rejected,
            ApplicationStatus.InterviewScheduled => newStatus == ApplicationStatus.Rejected,
            ApplicationStatus.InterviewCompleted => newStatus == ApplicationStatus.Rejected,
            ApplicationStatus.OfferMade => newStatus == ApplicationStatus.Rejected,
            _ => false
        };

        private static bool IsOpenApplication(ApplicationStatus status) => status is
            ApplicationStatus.Applied or ApplicationStatus.UnderReview or ApplicationStatus.Shortlisted or
            ApplicationStatus.InterviewScheduled or ApplicationStatus.InterviewCompleted or ApplicationStatus.OfferMade;

        private bool IsValidStatusTransition(ApplicationStatus current, ApplicationStatus newStatus)
        {
            return current switch
            {
                ApplicationStatus.Applied => newStatus == ApplicationStatus.UnderReview ||
                                             newStatus == ApplicationStatus.Rejected ||
                                             newStatus == ApplicationStatus.Withdrawn,
                ApplicationStatus.UnderReview => newStatus == ApplicationStatus.Shortlisted ||
                                                 newStatus == ApplicationStatus.Rejected,
                ApplicationStatus.Shortlisted => newStatus == ApplicationStatus.InterviewScheduled ||
                                                 newStatus == ApplicationStatus.Rejected,
                ApplicationStatus.InterviewScheduled => newStatus == ApplicationStatus.InterviewCompleted ||
                                                        newStatus == ApplicationStatus.Rejected,
                ApplicationStatus.InterviewCompleted => newStatus == ApplicationStatus.OfferMade ||
                                                        newStatus == ApplicationStatus.Rejected,
                ApplicationStatus.OfferMade => newStatus == ApplicationStatus.Accepted ||
                                              newStatus == ApplicationStatus.OfferDeclined ||
                                              newStatus == ApplicationStatus.Rejected,
                ApplicationStatus.Accepted => false,
                ApplicationStatus.OfferDeclined => false,
                ApplicationStatus.Rejected => false,
                ApplicationStatus.Withdrawn => false,
                _ => false
            };
        }

        // ✅ Use InternshipApplication (singular) - NOT InternshipApplications
        private async Task<ApplicationResponse> MapToResponse(InternshipApplication application)
        {
            return new ApplicationResponse
            {
                Id = application.Id,
                InternshipId = application.InternshipId,
                InternshipTitle = application.Internship?.Title ?? "Unknown",
                CompanyName = application.Internship?.CompanyProfile?.CompanyName ?? "Unknown",
                Status = application.Status.ToString(),
                AppliedAt = application.CreatedAt,
                StatusUpdatedAt = application.StatusUpdatedAt,
                CoverLetter = application.CoverLetter,
                IsShortlisted = application.IsShortlisted,
                InterviewDateTime = application.InterviewDateTime,
                InterviewType = application.InterviewType,
                InterviewLocationOrLink = application.InterviewLocationOrLink,
                InterviewNotes = application.InterviewNotes,
                OfferStipendAmount = application.OfferStipendAmount,
                OfferStartDate = application.OfferStartDate,
                OfferDetails = application.OfferDetails,
                OfferExpiryDate = application.OfferExpiryDate,
                Student = application.StudentProfile != null ? new StudentInfo
                {
                    StudentProfileId = application.StudentProfile.Id,
                    FullName = $"{application.StudentProfile.FirstName} {application.StudentProfile.LastName}",
                    Location = application.StudentProfile.Location,
                    University = application.StudentProfile.University,
                    Programme = application.StudentProfile.Programme,
                    YearOfStudy = application.StudentProfile.YearOfStudy,
                    ProfilePictureUrl = application.StudentProfile.ProfilePictureUrl,
                    ApplicationDocuments = await GetApplicationDocumentsAsync(application)
                } : null
            };
        }

        private async Task<List<ApplicationDocumentInfo>> GetApplicationDocumentsAsync(InternshipApplication application)
        {
            if (string.IsNullOrWhiteSpace(application.AdditionalDocuments))
                return new List<ApplicationDocumentInfo>();

            List<Guid>? documentIds;
            try
            {
                documentIds = System.Text.Json.JsonSerializer.Deserialize<List<Guid>>(application.AdditionalDocuments);
            }
            catch (System.Text.Json.JsonException)
            {
                return new List<ApplicationDocumentInfo>();
            }

            if (documentIds == null || documentIds.Count == 0)
                return new List<ApplicationDocumentInfo>();

            return await _context.PrivateDocuments
                .Where(document => documentIds.Contains(document.Id)
                    && document.OwnerUserId == application.StudentProfile.UserId
                    && document.RetentionState != DocumentRetentionState.Deleted)
                .Select(document => new ApplicationDocumentInfo
                {
                    Id = document.Id,
                    FileName = document.OriginalFileName,
                    Category = document.Category
                })
                .ToListAsync();
        }
    }
}
