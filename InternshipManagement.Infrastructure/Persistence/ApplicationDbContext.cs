using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Configurations; // Add this using
using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;
using InternshipManagement.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

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
        public DbSet<CompanyProfile> CompanyProfiles { get; set; }
        public DbSet<CompanyRepresentative> CompanyRepresentatives { get; set; }
        public DbSet<Internship> Internships { get; set; }
        public DbSet<InternshipApplication> InternshipApplications { get; set; }
        public DbSet<ApplicationStatusHistory> ApplicationStatusHistories { get; set; }
        public DbSet<Placement> Placements { get; set; }
        public DbSet<ProgressReport> ProgressReports { get; set; }
        public DbSet<InternshipExtensionRequest> InternshipExtensionRequests { get; set; }
        public DbSet<InternshipWithdrawalRequest> InternshipWithdrawalRequests { get; set; }
        public DbSet<InternshipEvaluation> InternshipEvaluations { get; set; }
        public DbSet<Report> Reports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all configurations from the Configurations folder
            modelBuilder.ApplyConfiguration(new StudentProfileConfiguration());
            modelBuilder.ApplyConfiguration(new CompanyProfileConfiguration());
            modelBuilder.ApplyConfiguration(new InternshipConfiguration());
            modelBuilder.ApplyConfiguration(new ApplicationConfiguration());
            ConfigureLifecycleEntities(modelBuilder);

            // Global query filters (soft delete) - keep these here
            modelBuilder.Entity<StudentProfile>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<CompanyProfile>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Internship>().HasQueryFilter(e => !e.IsDeleted);
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
