using InternshipManagement.Application.DTOs.Matching;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Application.Services
{
    public class MatchingService : IMatchingService
    {
        private readonly IApplicationDbContext _context;

        public MatchingService(IApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<List<MatchRecommendationResponse>> RecommendInternshipsForStudentAsync(int userId, int maxResults = 10)
        {
            var student = await _context.StudentProfiles
                .Include(s => s.Education)
                .Include(s => s.WorkExperience)
                .Include(s => s.Skills)
                .Include(s => s.Certifications)
                .Include(s => s.Projects)
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted);

            if (student == null)
                return new List<MatchRecommendationResponse>();

            var internships = await _context.Internships
                .Include(i => i.CompanyProfile)
                .Where(i => i.Status == InternshipStatus.Published
                    && i.ModerationStatus == ModerationStatus.Approved
                    && (i.CompanyProfile.VerificationStatus == CompanyVerificationStatus.Verified
                        || (i.CompanyProfile.VerificationStatus == CompanyVerificationStatus.ReverificationRequired
                            && i.CompanyProfile.ReverificationRequiredAt >= DateTime.UtcNow.AddDays(-7)))
                    && i.ApplicationDeadline >= DateTime.UtcNow
                    && !i.IsDeleted)
                .ToListAsync();

            return internships
                .Select(i => ScoreInternshipForStudent(student, i))
                .OrderByDescending(r => r.MatchScore)
                .ThenBy(r => r.Title)
                .Take(Math.Clamp(maxResults, 1, 50))
                .ToList();
        }

        public async Task<List<MatchRecommendationResponse>> RecommendStudentsForInternshipAsync(int userId, int internshipId, int maxResults = 10)
        {
            var companyProfile = await _context.CompanyProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (companyProfile == null)
                return new List<MatchRecommendationResponse>();

            var internship = await _context.Internships
                .FirstOrDefaultAsync(i => i.Id == internshipId
                    && i.CompanyProfileId == companyProfile.Id
                    && !i.IsDeleted);

            if (internship == null)
                return new List<MatchRecommendationResponse>();

            var students = await _context.StudentProfiles
                .Include(s => s.Education)
                .Include(s => s.WorkExperience)
                .Include(s => s.Skills)
                .Include(s => s.Certifications)
                .Include(s => s.Projects)
                .Where(s => !s.IsDeleted)
                .ToListAsync();

            return students
                .Select(s => ScoreStudentForInternship(s, internship))
                .OrderByDescending(r => r.MatchScore)
                .ThenBy(r => r.Title)
                .Take(Math.Clamp(maxResults, 1, 50))
                .ToList();
        }

        private static MatchRecommendationResponse ScoreInternshipForStudent(StudentProfile student, Internship internship)
        {
            var score = CalculateScore(student, internship, out var matchingSkills, out var matchingQualifications, out var missingPreferredSkills);
            var reasons = BuildReasons(score, matchingSkills, matchingQualifications, missingPreferredSkills);

            return new MatchRecommendationResponse
            {
                TargetId = internship.Id,
                TargetType = "Internship",
                Title = internship.Title,
                Subtitle = internship.CompanyProfile?.CompanyName,
                MatchScore = score,
                MatchingSkills = matchingSkills,
                MatchingQualifications = matchingQualifications,
                MissingPreferredSkills = missingPreferredSkills,
                Explanation = reasons
            };
        }

        private static MatchRecommendationResponse ScoreStudentForInternship(StudentProfile student, Internship internship)
        {
            var score = CalculateScore(student, internship, out var matchingSkills, out var matchingQualifications, out var missingPreferredSkills);
            var reasons = BuildReasons(score, matchingSkills, matchingQualifications, missingPreferredSkills);

            return new MatchRecommendationResponse
            {
                TargetId = student.Id,
                TargetType = "Student",
                Title = $"{student.FirstName} {student.LastName}",
                Subtitle = student.Programme ?? student.Location,
                MatchScore = score,
                MatchingSkills = matchingSkills,
                MatchingQualifications = matchingQualifications,
                MissingPreferredSkills = missingPreferredSkills,
                Explanation = reasons
            };
        }

        private static int CalculateScore(
            StudentProfile student,
            Internship internship,
            out List<string> matchingSkills,
            out List<string> matchingQualifications,
            out List<string> missingPreferredSkills)
        {
            var studentSkills = Terms(student.Skills?.Select(s => s.Name));
            var studentQualifications = Terms(student.Education?.SelectMany(e => new[] { e.Degree, e.FieldOfStudy, e.Institution })
                .Concat(student.Certifications?.Select(c => c.Name) ?? Enumerable.Empty<string>())
                .Concat(string.IsNullOrWhiteSpace(student.Programme) ? Enumerable.Empty<string>() : new[] { student.Programme }));
            var studentExperience = Terms(student.WorkExperience?.SelectMany(w => new[] { w.Position, w.Description, w.Company })
                .Concat(student.Projects?.SelectMany(p => new[] { p.Title, p.Description, p.Technologies }) ?? Enumerable.Empty<string>()));

            var requiredSkills = Terms(internship.RequiredSkills, internship.Skills);
            var preferredSkills = Terms(internship.PreferredSkills);
            var requiredQualifications = Terms(internship.RequiredQualifications, internship.Qualifications, internship.EligibleProgrammes);
            var preferredQualifications = Terms(internship.PreferredQualifications);

            matchingSkills = requiredSkills.Union(preferredSkills).Where(studentSkills.Contains).OrderBy(s => s).ToList();
            matchingQualifications = requiredQualifications.Union(preferredQualifications).Where(studentQualifications.Contains).OrderBy(q => q).ToList();
            missingPreferredSkills = preferredSkills.Where(s => !studentSkills.Contains(s)).OrderBy(s => s).ToList();

            var score = 0;
            score += Coverage(requiredSkills, studentSkills) * 40 / 100;
            score += Coverage(requiredQualifications, studentQualifications) * 25 / 100;
            score += Coverage(preferredSkills, studentSkills) * 15 / 100;
            score += Coverage(preferredQualifications, studentQualifications) * 10 / 100;
            score += Coverage(requiredSkills.Union(preferredSkills).ToHashSet(), studentExperience) * 10 / 100;

            return Math.Clamp(score, 0, 100);
        }

        private static int Coverage(HashSet<string> expected, HashSet<string> actual)
        {
            if (expected.Count == 0)
                return 100;

            return expected.Count(actual.Contains) * 100 / expected.Count;
        }

        private static HashSet<string> Terms(params string?[] values)
        {
            return Terms(values.AsEnumerable());
        }

        private static HashSet<string> Terms(IEnumerable<string?>? values)
        {
            var separators = new[] { ',', ';', '\n', '\r', '|', '/', '\\', '.', ':', '-', '(', ')', '[', ']', '"', '\'' };

            return (values ?? Enumerable.Empty<string?>())
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .SelectMany(v => v!.Split(separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Select(Normalize)
                .Where(v => v.Length >= 2)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static string Normalize(string value)
        {
            return value.Trim().ToLowerInvariant();
        }

        private static string BuildReasons(
            int score,
            List<string> matchingSkills,
            List<string> matchingQualifications,
            List<string> missingPreferredSkills)
        {
            var parts = new List<string>();

            if (matchingSkills.Any())
                parts.Add($"Matched skills: {string.Join(", ", matchingSkills)}.");

            if (matchingQualifications.Any())
                parts.Add($"Matched qualifications: {string.Join(", ", matchingQualifications)}.");

            if (missingPreferredSkills.Any())
                parts.Add($"Missing preferred skills: {string.Join(", ", missingPreferredSkills)}.");

            if (!parts.Any())
                parts.Add("No strong structured matches were found yet; the score is based on limited profile and posting data.");

            parts.Add($"Match score is {score}%. This recommendation does not affect application eligibility.");
            return string.Join(" ", parts);
        }
    }
}
