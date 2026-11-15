using InternshipManagement.Application.DTOs.Company;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Application.Services
{
    /// <summary>What the company assistant may read. Always scoped to the company that owns the run token.</summary>
    public class CompanyAgentToolsService : ICompanyAgentToolsService
    {
        private const int MaxInternships = 20;
        private const int MaxApplicants = 25;

        private readonly IApplicationDbContext _context;
        private readonly IMatchingService _matching;

        public CompanyAgentToolsService(IApplicationDbContext context, IMatchingService matching)
        {
            _context = context;
            _matching = matching;
        }

        public async Task<CompanyOverview> GetOverviewAsync(int userId, CancellationToken cancellationToken = default)
        {
            var company = await _context.CompanyProfiles.AsNoTracking()
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .Select(c => new { c.Id, c.CompanyName, c.Industry, c.VerificationStatus })
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("Company profile not found.");

            var internships = await _context.Internships.AsNoTracking()
                .Where(i => i.CompanyProfileId == company.Id && !i.IsDeleted)
                .GroupBy(i => i.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var applications = await _context.InternshipApplications.AsNoTracking()
                .Where(a => a.Internship.CompanyProfileId == company.Id && !a.IsDeleted)
                .GroupBy(a => a.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            var placements = await _context.Placements.AsNoTracking()
                .Where(p => p.CompanyProfileId == company.Id && !p.IsDeleted)
                .GroupBy(p => p.Status)
                .Select(g => new { Status = g.Key, Count = g.Count() })
                .ToListAsync(cancellationToken);

            return new CompanyOverview
            {
                CompanyName = TextCleaner.Clean(company.CompanyName, 120) ?? string.Empty,
                Industry = TextCleaner.Clean(company.Industry, 80),
                VerificationStatus = company.VerificationStatus.ToString(),
                InternshipsByStatus = internships.ToDictionary(x => x.Status.ToString(), x => x.Count),
                ApplicationsByStatus = applications.ToDictionary(x => x.Status.ToString(), x => x.Count),
                PlacementsByStatus = placements.ToDictionary(x => x.Status.ToString(), x => x.Count)
            };
        }

        public async Task<List<CompanyInternshipSummary>> GetInternshipsAsync(int userId, CancellationToken cancellationToken = default)
        {
            var companyId = await GetCompanyIdAsync(userId, cancellationToken);

            var rows = await _context.Internships.AsNoTracking()
                .Where(i => i.CompanyProfileId == companyId && !i.IsDeleted)
                .OrderByDescending(i => i.CreatedAt)
                .Take(MaxInternships)
                .Select(i => new
                {
                    i.Id,
                    i.Title,
                    i.Status,
                    i.ApplicationDeadline,
                    i.NumberOfPositions,
                    i.Views,
                    Total = _context.InternshipApplications.Count(a => a.InternshipId == i.Id && !a.IsDeleted),
                    New = _context.InternshipApplications.Count(a => a.InternshipId == i.Id && !a.IsDeleted && a.Status == ApplicationStatus.Applied)
                })
                .ToListAsync(cancellationToken);

            return rows.Select(r => new CompanyInternshipSummary
            {
                InternshipId = r.Id,
                Title = TextCleaner.Clean(r.Title, 120) ?? string.Empty,
                Status = r.Status.ToString(),
                ApplicationDeadline = r.ApplicationDeadline.ToString("yyyy-MM-dd"),
                Positions = r.NumberOfPositions,
                Views = r.Views,
                TotalApplications = r.Total,
                NewApplications = r.New
            }).ToList();
        }

        public async Task<CompanyApplicantsResult> GetApplicantsAsync(int userId, int internshipId, CancellationToken cancellationToken = default)
        {
            var companyId = await GetCompanyIdAsync(userId, cancellationToken);

            // The internship must belong to this company: the model can never ask about someone else's.
            var title = await _context.Internships.AsNoTracking()
                .Where(i => i.Id == internshipId && i.CompanyProfileId == companyId && !i.IsDeleted)
                .Select(i => i.Title)
                .FirstOrDefaultAsync(cancellationToken)
                ?? throw new InvalidOperationException("Internship not found.");

            var total = await _context.InternshipApplications.AsNoTracking()
                .CountAsync(a => a.InternshipId == internshipId && !a.IsDeleted, cancellationToken);

            var rows = await _context.InternshipApplications.AsNoTracking()
                .Where(a => a.InternshipId == internshipId && !a.IsDeleted)
                .OrderByDescending(a => a.CreatedAt)
                .Take(MaxApplicants)
                .Select(a => new
                {
                    a.Id,
                    a.Status,
                    a.CreatedAt,
                    StudentUserId = a.StudentProfile.UserId,
                    a.StudentProfile.Programme,
                    a.StudentProfile.YearOfStudy,
                    Skills = a.StudentProfile.Skills.Where(s => !s.IsDeleted).Select(s => s.Name).Take(15).ToList()
                })
                .ToListAsync(cancellationToken);

            // The platform's own score. If it cannot be worked out, the applicants are still listed, without a score.
            var scores = new Dictionary<int, InternshipManagement.Application.DTOs.Matching.StudentMatchScore>();
            try
            {
                foreach (var score in await _matching.ScoreStudentsForInternshipAsync(internshipId, 0, cancellationToken))
                    scores[score.UserId] = score;
            }
            catch (Exception)
            {
                scores.Clear();
            }

            var applicants = rows.Select(r =>
            {
                scores.TryGetValue(r.StudentUserId, out var score);
                return new CompanyApplicantSummary
                {
                    ApplicationId = r.Id,
                    Status = r.Status.ToString(),
                    AppliedOn = r.CreatedAt.ToString("yyyy-MM-dd"),
                    Programme = TextCleaner.Clean(r.Programme, 100),
                    YearOfStudy = TextCleaner.Clean(r.YearOfStudy, 40),
                    Skills = r.Skills.Select(s => TextCleaner.Clean(s, 60) ?? string.Empty).Where(s => s.Length > 0).ToList(),
                    MatchScore = score?.Score,
                    MatchingSkills = score?.MatchingSkills.Take(10).ToList() ?? new List<string>()
                };
            })
            .OrderByDescending(a => a.MatchScore ?? -1)
            .ThenByDescending(a => a.AppliedOn)
            .ToList();

            return new CompanyApplicantsResult
            {
                InternshipId = internshipId,
                InternshipTitle = TextCleaner.Clean(title, 120) ?? string.Empty,
                TotalApplications = total,
                ShowingNewestOnly = total > MaxApplicants,
                Applicants = applicants
            };
        }

        private async Task<int> GetCompanyIdAsync(int userId, CancellationToken cancellationToken)
        {
            var companyId = await _context.CompanyProfiles.AsNoTracking()
                .Where(c => c.UserId == userId && !c.IsDeleted)
                .Select(c => c.Id)
                .FirstOrDefaultAsync(cancellationToken);

            if (companyId <= 0)
                throw new InvalidOperationException("Company profile not found.");
            return companyId;
        }
    }
}
