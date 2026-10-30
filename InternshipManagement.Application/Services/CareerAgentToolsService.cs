using InternshipManagement.Application.DTOs.Student;
using InternshipManagement.Application.Interfaces;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Services
{
    public class CareerAgentToolsService : ICareerAgentToolsService
    {
        private readonly IApplicationDbContext _context;

        public CareerAgentToolsService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<StudentCareerSummary> GetProfileSummaryAsync(int userId, CancellationToken cancellationToken = default)
        {
            var p = await _context.StudentProfiles
                .AsNoTracking()
                .Include(s => s.Education)
                .Include(s => s.WorkExperience)
                .Include(s => s.Skills)
                .Include(s => s.Certifications)
                .Include(s => s.Projects)
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException("Student profile not found.");

            // Only what a career conversation needs. No name, phone, email, address, links or document URLs.
            return new StudentCareerSummary
            {
                Programme = TextCleaner.Clean(p.Programme, 120),
                YearOfStudy = TextCleaner.Clean(p.YearOfStudy, 40),
                ExpectedGraduationYear = TextCleaner.Clean(p.ExpectedGraduationYear, 10),
                Skills = (p.Skills ?? Array.Empty<Domain.Entities.Skill>())
                    .Where(s => !s.IsDeleted)
                    .Take(40)
                    .Select(s => new CareerSkill { Name = TextCleaner.Clean(s.Name, 60) ?? "", Level = TextCleaner.Clean(s.ProficiencyLevel, 20) })
                    .Where(s => s.Name.Length > 0)
                    .ToList(),
                Education = (p.Education ?? Array.Empty<Domain.Entities.Education>())
                    .Where(e => !e.IsDeleted)
                    .Take(6)
                    .Select(e => new CareerEducation
                    {
                        Degree = TextCleaner.Clean(e.Degree, 80),
                        FieldOfStudy = TextCleaner.Clean(e.FieldOfStudy, 80),
                        Status = e.IsCurrent ? "In progress" : (TextCleaner.Clean(e.CompletionStatus, 30) ?? "Completed")
                    })
                    .ToList(),
                Certifications = (p.Certifications ?? Array.Empty<Domain.Entities.StudentCertification>())
                    .Where(c => !c.IsDeleted)
                    .Take(15)
                    .Select(c => new CareerCertification { Name = TextCleaner.Clean(c.Name, 100) ?? "", Issuer = TextCleaner.Clean(c.IssuingOrganization, 80) })
                    .Where(c => c.Name.Length > 0)
                    .ToList(),
                WorkExperience = (p.WorkExperience ?? Array.Empty<Domain.Entities.WorkExperience>())
                    .Where(w => !w.IsDeleted)
                    .Take(8)
                    .Select(w => new CareerWork { Position = TextCleaner.Clean(w.Position, 80) ?? "", Company = TextCleaner.Clean(w.Company, 80) })
                    .Where(w => w.Position.Length > 0)
                    .ToList(),
                Projects = (p.Projects ?? Array.Empty<Domain.Entities.StudentProject>())
                    .Where(x => !x.IsDeleted)
                    .Take(8)
                    .Select(x => TextCleaner.Clean(x.Title, 100) ?? "")
                    .Where(t => t.Length > 0)
                    .ToList()
            };
        }
    }
}
