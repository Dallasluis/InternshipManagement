using Microsoft.EntityFrameworkCore;
using InternshipManagement.Domain.Entities;

namespace InternshipManagement.Application.Interfaces
{
    public interface IApplicationDbContext
    {
        DbSet<StudentProfile> StudentProfiles { get; }
        DbSet<Education> Educations { get; }
        DbSet<WorkExperience> WorkExperiences { get; }
        DbSet<Skill> Skills { get; }
        DbSet<CompanyProfile> CompanyProfiles { get; }
        DbSet<CompanyRepresentative> CompanyRepresentatives { get; }
        DbSet<Internship> Internships { get; }
        DbSet<InternshipApplication> InternshipApplications { get; }  // Changed
        DbSet<Report> Reports { get; }

        Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
    }
}