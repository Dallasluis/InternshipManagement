using InternshipManagement.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<StudentProfile> StudentProfiles { get; }
        DbSet<Education> Educations { get; }
        DbSet<WorkExperience> WorkExperiences { get; }
        DbSet<Skill> Skills { get; }
        DbSet<StudentCertification> StudentCertifications { get; }
        DbSet<StudentProject> StudentProjects { get; }
        DbSet<CompanyProfile> CompanyProfiles { get; }
        DbSet<CompanyRepresentative> CompanyRepresentatives { get; }
        DbSet<Internship> Internships { get; }
        DbSet<InternshipDraft> InternshipDrafts { get; }
        DbSet<AiJob> AiJobs { get; }
        DbSet<InternshipApplication> InternshipApplications { get; }  // Changed
        DbSet<ApplicationStatusHistory> ApplicationStatusHistories { get; }
        DbSet<Placement> Placements { get; }
        DbSet<ProgressReport> ProgressReports { get; }
        DbSet<InternshipExtensionRequest> InternshipExtensionRequests { get; }
        DbSet<InternshipWithdrawalRequest> InternshipWithdrawalRequests { get; }
        DbSet<InternshipEvaluation> InternshipEvaluations { get; }
        DbSet<Report> Reports { get; }
        DbSet<AccountSecurityEvent> AccountSecurityEvents { get; }
        DbSet<EmailActionToken> EmailActionTokens { get; }
        DbSet<CompanyVerificationHistory> CompanyVerificationHistories { get; }
        DbSet<PrivateDocument> PrivateDocuments { get; }
        DbSet<DocumentAccessToken> DocumentAccessTokens { get; }
        DbSet<DocumentAccessAudit> DocumentAccessAudits { get; }
           DbSet<Notification> Notifications { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}
