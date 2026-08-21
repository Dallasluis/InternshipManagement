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

        public ApplicationService(IApplicationDbContext context, IInternshipService internshipService)
        {
            _context = context;
            _internshipService = internshipService;
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

            // ✅ Use InternshipApplication (singular) - NOT InternshipApplications
            var application = new InternshipApplication
            {
                StudentProfileId = studentProfile.Id,
                InternshipId = request.InternshipId,
                CoverLetter = request.CoverLetter,
                AdditionalDocuments = request.AdditionalDocumentUrls != null
                    ? System.Text.Json.JsonSerializer.Serialize(request.AdditionalDocumentUrls)
                    : null,
                Status = ApplicationStatus.Applied,
                StatusUpdatedAt = DateTime.UtcNow
            };

            _context.InternshipApplications.Add(application);
            await _context.SaveChangesAsync();

            var internship = await _context.Internships.FindAsync(request.InternshipId);
            if (internship != null)
            {
                internship.ApplicationsCount++;
                await _context.SaveChangesAsync();
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

        public async Task<bool> UpdateApplicationStatusAsync(int applicationId, UpdateApplicationStatusRequest request)
        {
            var application = await _context.InternshipApplications.FindAsync(applicationId);
            if (application == null) return false;

            var newStatus = Enum.Parse<ApplicationStatus>(request.Status);

            if (!IsValidStatusTransition(application.Status, newStatus))
                throw new Exception($"Cannot transition from {application.Status} to {newStatus}");

            application.Status = newStatus;
            application.StatusUpdatedAt = DateTime.UtcNow;
            application.StatusNotes = request.Notes;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> ShortlistApplicationAsync(int applicationId, string? notes)
        {
            var application = await _context.InternshipApplications.FindAsync(applicationId);
            if (application == null) return false;

            if (application.Status == ApplicationStatus.Rejected ||
                application.Status == ApplicationStatus.Withdrawn)
                throw new Exception("Cannot shortlist a rejected or withdrawn application.");

            application.IsShortlisted = true;
            application.ShortlistedAt = DateTime.UtcNow;
            application.ShortlistNotes = notes;
            application.Status = ApplicationStatus.Shortlisted;
            application.StatusUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> WithdrawApplicationAsync(int applicationId)
        {
            var application = await _context.InternshipApplications.FindAsync(applicationId);
            if (application == null) return false;

            if (application.Status == ApplicationStatus.Rejected)
                throw new Exception("Cannot withdraw a rejected application.");

            application.Status = ApplicationStatus.Withdrawn;
            application.StatusUpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

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

        private bool IsValidStatusTransition(ApplicationStatus current, ApplicationStatus newStatus)
        {
            return current switch
            {
                ApplicationStatus.Applied => newStatus == ApplicationStatus.UnderReview ||
                                             newStatus == ApplicationStatus.Rejected ||
                                             newStatus == ApplicationStatus.Withdrawn,
                ApplicationStatus.UnderReview => newStatus == ApplicationStatus.Shortlisted ||
                                                 newStatus == ApplicationStatus.Rejected,
                ApplicationStatus.Shortlisted => newStatus == ApplicationStatus.Rejected,
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
                Student = application.StudentProfile != null ? new StudentInfo
                {
                    StudentProfileId = application.StudentProfile.Id,
                    FullName = $"{application.StudentProfile.FirstName} {application.StudentProfile.LastName}",
                    Location = application.StudentProfile.Location,
                    University = application.StudentProfile.University,
                    Programme = application.StudentProfile.Programme,
                    YearOfStudy = application.StudentProfile.YearOfStudy,
                    ResumeUrl = application.StudentProfile.ResumeUrl,
                    ProfilePictureUrl = application.StudentProfile.ProfilePictureUrl
                } : null
            };
        }
    }
}