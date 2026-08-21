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
        public DbSet<Report> Reports { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Apply all configurations from the Configurations folder
            modelBuilder.ApplyConfiguration(new StudentProfileConfiguration());
            modelBuilder.ApplyConfiguration(new CompanyProfileConfiguration());
            modelBuilder.ApplyConfiguration(new InternshipConfiguration());
            modelBuilder.ApplyConfiguration(new ApplicationConfiguration());

            // Global query filters (soft delete) - keep these here
            modelBuilder.Entity<StudentProfile>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<CompanyProfile>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Internship>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<InternshipApplication>().HasQueryFilter(e => !e.IsDeleted);
            modelBuilder.Entity<Report>().HasQueryFilter(e => !e.IsDeleted);
        }
    }
}