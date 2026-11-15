using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Configurations;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;
using InternshipManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;
using InternshipManagement.Infrastructure.Persistence.Configurations;

namespace InternshipManagement.Infrastructure.Persistence
{
    public class ApplicationDbContext : IdentityDbContext<ApplicationUser, IdentityRole<int>, int>, IApplicationDbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
            : base(options)
        {
        }

        public DbSet<StudentProfile> StudentProfiles { get; set; }
        public DbSet<Education> Educations { get; set; }
        public DbSet<WorkExperience> WorkExperiences { get; set; }
        public DbSet<Skill> Skills { get; set; }
        public DbSet<StudentCertification> StudentCertifications { get; set; }
        public DbSet<StudentProject> StudentProjects { get; set; }
        public DbSet<CompanyProfile> CompanyProfiles { get; set; }
        public DbSet<CompanyRepresentative> CompanyRepresentatives { get; set; }
        public DbSet<Internship> Internships { get; set; }
        public DbSet<InternshipDraft> InternshipDrafts { get; set; }
        public DbSet<AiJob> AiJobs { get; set; }
        public DbSet<InternshipApplication> InternshipApplications { get; set; }
        public DbSet<ApplicationStatusHistory> ApplicationStatusHistories { get; set; }
        public DbSet<Placement> Placements { get; set; }
        public DbSet<ProgressReport> ProgressReports { get; set; }
        public DbSet<InternshipExtensionRequest> InternshipExtensionRequests { get; set; }
        public DbSet<InternshipWithdrawalRequest> InternshipWithdrawalRequests { get; set; }
        public DbSet<InternshipEvaluation> InternshipEvaluations { get; set; }
        public DbSet<Report> Reports { get; set; }
        public DbSet<AccountSecurityEvent> AccountSecurityEvents { get; set; }
        public DbSet<EmailActionToken> EmailActionTokens { get; set; }
        public DbSet<CompanyVerificationHistory> CompanyVerificationHistories { get; set; }
        public DbSet<PrivateDocument> PrivateDocuments { get; set; }
        public DbSet<DocumentAccessToken> DocumentAccessTokens { get; set; }
        public DbSet<DocumentAccessAudit> DocumentAccessAudits { get; set; }
        public DbSet<Notification> Notifications { get; set; }
        public DbSet<AuditLog> AuditLogs { get; set; }
        public DbSet<SavedInternship> SavedInternships { get; set; }
        public DbSet<CompanyWarning> CompanyWarnings { get; set; }
        public DbSet<InternshipCategory> InternshipCategories { get; set; }
           public DbSet<OfferContract> OfferContracts { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all configurations from the Configurations folder
            modelBuilder.ApplyConfiguration(new StudentProfileConfiguration());
            modelBuilder.ApplyConfiguration(new InternshipManagement.Infrastructure.Persistence.Configurations.NotificationConfiguration());
            modelBuilder.ApplyConfiguration(new CompanyProfileConfiguration());
            modelBuilder.ApplyConfiguration(new InternshipConfiguration());
            modelBuilder.ApplyConfiguration(new ApplicationConfiguration());
            modelBuilder.ApplyConfiguration(new InternshipManagement.Infrastructure.Persistence.Configurations.InternshipDraftConfiguration());
            modelBuilder.ApplyConfiguration(new InternshipManagement.Infrastructure.Persistence.Configurations.AiJobConfiguration());
            modelBuilder.ApplyConfiguration(new InternshipManagement.Infrastructure.Persistence.Configurations.AuditLogConfiguration());
            modelBuilder.ApplyConfiguration(new InternshipManagement.Infrastructure.Persistence.Configurations.SavedInternshipConfiguration());
               modelBuilder.ApplyConfiguration(new InternshipManagement.Infrastructure.Persistence.Configurations.OfferContractConfiguration());
            modelBuilder.ApplyConfiguration(new InternshipManagement.Infrastructure.Persistence.Configurations.CompanyWarningConfiguration());
            modelBuilder.ApplyConfiguration(new InternshipManagement.Infrastructure.Persistence.Configurations.InternshipCategoryConfiguration());
            ConfigureLifecycleEntities(modelBuilder);
            modelBuilder.Entity<AccountSecurityEvent>(builder =>
            {
                builder.HasKey(e => e.Id);
                builder.Property(e => e.EventType).IsRequired().HasMaxLength(100);
                builder.Property(e => e.Details).HasMaxLength(2000);
                builder.Property(e => e.IpAddress).HasMaxLength(64);
                builder.HasIndex(e => e.CreatedAt);
                builder.HasIndex(e => new { e.UserId, e.CreatedAt });
            });
            modelBuilder.Entity<EmailActionToken>(builder =>
            {
                builder.HasKey(e => e.TokenHash);
                builder.Property(e => e.TokenHash).HasMaxLength(64);
                builder.Property(e => e.Purpose).IsRequired().HasMaxLength(40);
                builder.Property(e => e.TargetEmail).HasMaxLength(256);
                builder.HasIndex(e => new { e.UserId, e.Purpose, e.CreatedAt });
                builder.HasIndex(e => e.ExpiresAt);
            });
            modelBuilder.Entity<CompanyVerificationHistory>(builder =>
            {
                builder.HasKey(e => e.Id);
                builder.Property(e => e.ReasonCode).HasMaxLength(100);
                builder.Property(e => e.Notes).HasMaxLength(2000);
                builder.Property(e => e.CriteriaAssessmentJson).HasColumnType("nvarchar(max)");
                builder.Property(e => e.EvidenceDocumentIdsJson).HasColumnType("nvarchar(max)");
                builder.HasIndex(e => new { e.CompanyProfileId, e.CreatedAt });
                builder.HasIndex(e => new { e.ChangedByUserId, e.CreatedAt });
            });
            modelBuilder.Entity<PrivateDocument>(builder =>
            {
                builder.HasKey(e => e.Id);
                builder.Property(e => e.Category).IsRequired().HasMaxLength(50);
                builder.Property(e => e.OriginalFileName).IsRequired().HasMaxLength(255);
                builder.Property(e => e.ContentType).IsRequired().HasMaxLength(100);
                builder.HasIndex(e => new { e.OwnerUserId, e.RetentionState });
            });
            modelBuilder.Entity<DocumentAccessToken>(builder =>
            {
                builder.HasKey(e => e.TokenHash);
                builder.Property(e => e.TokenHash).HasMaxLength(64);
                builder.HasIndex(e => e.ExpiresAt);
                builder.HasIndex(e => new { e.DocumentId, e.RequestedByUserId });
            });
            modelBuilder.Entity<DocumentAccessAudit>(builder =>
            {
                builder.HasKey(e => e.Id);
                builder.Property(e => e.Action).IsRequired().HasMaxLength(30);
                builder.HasIndex(e => new { e.DocumentId, e.AccessedAt });
                builder.HasIndex(e => new { e.UserId, e.AccessedAt });
            });

            // Global query filters (soft delete) - keep these here
            modelBuilder.Entity<StudentProfile>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Education>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<WorkExperience>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Skill>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<StudentCertification>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<StudentProject>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<CompanyProfile>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Internship>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<InternshipDraft>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<AiJob>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<InternshipApplication>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ApplicationStatusHistory>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Placement>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<ProgressReport>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<InternshipExtensionRequest>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<InternshipWithdrawalRequest>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<InternshipEvaluation>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Report>().HasQueryFilter(e => !e.IsDeleted);
        }

        private static void ConfigureLifecycleEntities(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<ApplicationStatusHistory>(builder =>
            {
                builder.ToTable("ApplicationStatusHistories");
                builder.HasKey(h => h.Id);
                builder.Property(h => h.Notes).HasMaxLength(1000);
                builder.HasOne(h => h.InternshipApplication)
                    .WithMany()
                    .HasForeignKey(h => h.InternshipApplicationId)
                    .OnDelete(DeleteBehavior.Restrict);
                builder.HasIndex(h => h.InternshipApplicationId);
                builder.HasIndex(h => h.ChangedAt);
            });

            modelBuilder.Entity<Placement>(builder =>
            {
                builder.ToTable("Placements");
                builder.HasKey(p => p.Id);
                builder.Property(p => p.TerminationReason).HasMaxLength(1000);
                builder.HasOne(p => p.StudentProfile).WithMany().HasForeignKey(p => p.StudentProfileId).OnDelete(DeleteBehavior.Restrict);
                builder.HasOne(p => p.CompanyProfile).WithMany().HasForeignKey(p => p.CompanyProfileId).OnDelete(DeleteBehavior.Restrict);
                builder.HasOne(p => p.Internship).WithMany().HasForeignKey(p => p.InternshipId).OnDelete(DeleteBehavior.Restrict);
                builder.HasOne(p => p.InternshipApplication).WithMany().HasForeignKey(p => p.InternshipApplicationId).OnDelete(DeleteBehavior.Restrict);
                builder.HasIndex(p => p.StudentProfileId);
                builder.HasIndex(p => p.CompanyProfileId);
                builder.HasIndex(p => p.InternshipId);
                builder.HasIndex(p => p.InternshipApplicationId).IsUnique();
                builder.HasIndex(p => p.Status);
            });

            modelBuilder.Entity<ProgressReport>(builder =>
            {
                builder.ToTable("ProgressReports");
                builder.HasKey(r => r.Id);
                builder.Property(r => r.WorkCompleted).IsRequired().HasMaxLength(3000);
                builder.Property(r => r.SkillsLearned).HasMaxLength(2000);
                builder.Property(r => r.Challenges).HasMaxLength(2000);
                builder.Property(r => r.Achievements).HasMaxLength(2000);
                builder.Property(r => r.Comments).HasMaxLength(2000);
                builder.Property(r => r.SupportingDocuments).HasColumnType("nvarchar(max)");
                builder.Property(r => r.CompanyFeedback).HasMaxLength(2000);
                builder.HasOne(r => r.Placement).WithMany(p => p.ProgressReports).HasForeignKey(r => r.PlacementId).OnDelete(DeleteBehavior.Restrict);
                builder.HasIndex(r => r.PlacementId);
                builder.HasIndex(r => r.Status);
            });

            modelBuilder.Entity<InternshipExtensionRequest>(builder =>
            {
                builder.ToTable("InternshipExtensionRequests");
                builder.HasKey(e => e.Id);
                builder.Property(e => e.Reason).HasMaxLength(1000);
                builder.Property(e => e.ResponseNotes).HasMaxLength(1000);
                builder.HasOne(e => e.Placement).WithMany(p => p.ExtensionRequests).HasForeignKey(e => e.PlacementId).OnDelete(DeleteBehavior.Restrict);
                builder.HasIndex(e => e.PlacementId);
                builder.HasIndex(e => e.Status);
            });

            modelBuilder.Entity<InternshipWithdrawalRequest>(builder =>
            {
                builder.ToTable("InternshipWithdrawalRequests");
                builder.HasKey(w => w.Id);
                builder.Property(w => w.Reason).IsRequired().HasMaxLength(1000);
                builder.Property(w => w.CompanyDecisionNotes).HasMaxLength(1000);
                builder.Property(w => w.AdminDecisionNotes).HasMaxLength(1000);
                builder.HasOne(w => w.Placement).WithMany(p => p.WithdrawalRequests).HasForeignKey(w => w.PlacementId).OnDelete(DeleteBehavior.Restrict);
                builder.HasIndex(w => w.PlacementId);
                builder.HasIndex(w => w.Status);
            });

            modelBuilder.Entity<InternshipEvaluation>(builder =>
            {
                builder.ToTable("InternshipEvaluations");
                builder.HasKey(e => e.Id);
                builder.Property(e => e.Comments).HasMaxLength(2000);
                builder.HasOne(e => e.Placement).WithMany(p => p.Evaluations).HasForeignKey(e => e.PlacementId).OnDelete(DeleteBehavior.Restrict);
                builder.HasIndex(e => e.PlacementId);
            });
        }
    }
}