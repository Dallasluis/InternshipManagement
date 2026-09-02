using Microsoft.EntityFrameworkCore;
using InternshipManagement.Application.DTOs.Internship;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;
using Microsoft.Extensions.Configuration;
using System.Text.Json;

namespace InternshipManagement.Application.Services
{
    public class InternshipService : IInternshipService
    {
        private readonly IApplicationDbContext _context;
        private readonly IConfiguration _configuration;

        public InternshipService(IApplicationDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        public async Task<InternshipResponse> CreateInternshipAsync(int userId, CreateInternshipRequest request)
        {
            var companyProfile = await _context.CompanyProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId);

            if (companyProfile == null)
                throw new Exception("Company profile not found.");

            if (companyProfile.VerificationStatus != CompanyVerificationStatus.Verified)
                throw new Exception("Company must be verified to create internships.");

            if (_configuration.GetValue<bool>("Internships:RequireSubscription") && !companyProfile.IsSubscribed)
                throw new Exception("Active subscription required.");

            var eligibleProgrammesJson = request.EligibleProgrammes != null && request.EligibleProgrammes.Any()
                ? JsonSerializer.Serialize(request.EligibleProgrammes)
                : null;

            var internship = new Internship
            {
                CompanyProfileId = companyProfile.Id,
                Title = request.Title,
                Description = request.Description,
                Responsibilities = request.Responsibilities,
                Requirements = request.Requirements,
                Qualifications = request.Qualifications,
                Skills = request.Skills,
                Industry = request.Industry,
                Location = request.Location,
                IsRemote = request.IsRemote,
                InternshipType = Enum.Parse<InternshipType>(request.InternshipType),
                Duration = Enum.Parse<InternshipDuration>(request.Duration),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ApplicationDeadline = request.ApplicationDeadline,
                NumberOfPositions = request.NumberOfPositions,
                Compensation = request.Compensation,
                StipendAmount = request.StipendAmount,
                Currency = request.Currency,
                EligibleProgrammes = eligibleProgrammesJson,
                Status = InternshipStatus.Draft,
                ModerationStatus = ModerationStatus.Pending
            };

            _context.Internships.Add(internship);
            await _context.SaveChangesAsync();

            return await MapToResponse(internship);
        }

        public async Task<InternshipResponse> UpdateInternshipAsync(int internshipId, UpdateInternshipRequest request)
        {
            var internship = await _context.Internships
                .Include(i => i.CompanyProfile)
                .FirstOrDefaultAsync(i => i.Id == internshipId);

            if (internship == null)
                throw new Exception("Internship not found");

            if (internship.Status != InternshipStatus.Draft)
                throw new Exception("Only draft internships can be edited.");

            var eligibleProgrammesJson = request.EligibleProgrammes != null && request.EligibleProgrammes.Any()
                ? JsonSerializer.Serialize(request.EligibleProgrammes)
                : null;

            internship.Title = request.Title;
            internship.Description = request.Description;
            internship.Responsibilities = request.Responsibilities;
            internship.Requirements = request.Requirements;
            internship.Qualifications = request.Qualifications;
            internship.Skills = request.Skills;
            internship.Industry = request.Industry;
            internship.Location = request.Location;
            internship.IsRemote = request.IsRemote;
            internship.InternshipType = Enum.Parse<InternshipType>(request.InternshipType);
            internship.Duration = Enum.Parse<InternshipDuration>(request.Duration);
            internship.StartDate = request.StartDate;
            internship.EndDate = request.EndDate;
            internship.ApplicationDeadline = request.ApplicationDeadline;
            internship.NumberOfPositions = request.NumberOfPositions;
            internship.Compensation = request.Compensation;
            internship.StipendAmount = request.StipendAmount;
            internship.Currency = request.Currency;
            internship.EligibleProgrammes = eligibleProgrammesJson;
            internship.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();

            return await MapToResponse(internship);
        }

        public async Task<bool> PublishInternshipAsync(int internshipId)
        {
            var internship = await _context.Internships.FindAsync(internshipId);
            if (internship == null) return false;

            if (internship.Status != InternshipStatus.Draft)
                throw new Exception("Only draft internships can be published.");

            if (internship.ApplicationDeadline < DateTime.UtcNow)
                throw new Exception("Cannot publish internship with past application deadline.");

            internship.Status = InternshipStatus.Published;
            internship.PublishedAt = DateTime.UtcNow;
            internship.ModerationStatus = ModerationStatus.Approved;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CloseInternshipAsync(int internshipId)
        {
            var internship = await _context.Internships.FindAsync(internshipId);
            if (internship == null) return false;

            internship.Status = InternshipStatus.Closed;
            internship.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<InternshipResponse> GetInternshipByIdAsync(int id)
        {
            var internship = await _context.Internships
                .Include(i => i.CompanyProfile)
                .FirstOrDefaultAsync(i => i.Id == id && !i.IsDeleted);

            if (internship == null) return null;

            internship.Views++;
            await _context.SaveChangesAsync();

            return await MapToResponse(internship);
        }

        public async Task<(List<InternshipResponse> Items, int TotalCount)> SearchInternshipsAsync(InternshipSearchRequest request)
        {
            var query = _context.Internships
                .Include(i => i.CompanyProfile)
                .Where(i => i.Status == InternshipStatus.Published
                    && i.ModerationStatus == ModerationStatus.Approved
                    && !i.IsDeleted
                    && i.ApplicationDeadline >= DateTime.UtcNow);

            if (!string.IsNullOrEmpty(request.Keyword))
            {
                query = query.Where(i =>
                    i.Title.Contains(request.Keyword) ||
                    i.Description.Contains(request.Keyword) ||
                    (i.Skills != null && i.Skills.Contains(request.Keyword)) ||
                    i.Industry.Contains(request.Keyword));
            }

            if (!string.IsNullOrEmpty(request.Location))
            {
                query = query.Where(i =>
                    (i.Location != null && i.Location.Contains(request.Location)) ||
                    i.IsRemote);
            }

            if (!string.IsNullOrEmpty(request.Industry))
            {
                query = query.Where(i => i.Industry == request.Industry);
            }

            if (!string.IsNullOrEmpty(request.InternshipType))
            {
                var type = Enum.Parse<InternshipType>(request.InternshipType);
                query = query.Where(i => i.InternshipType == type);
            }

            if (request.MinStipend.HasValue)
            {
                query = query.Where(i => i.StipendAmount >= request.MinStipend);
            }

            if (request.IsRemote.HasValue)
            {
                query = query.Where(i => i.IsRemote == request.IsRemote);
            }

            if (!string.IsNullOrEmpty(request.Programme) && request.ShowOnlyMatchingProgrammes)
            {
                query = query.Where(i =>
                    i.EligibleProgrammes != null &&
                    i.EligibleProgrammes.Contains(request.Programme));
            }

            query = request.SortBy switch
            {
                "oldest" => query.OrderBy(i => i.CreatedAt),
                "popular" => query.OrderByDescending(i => i.Views),
                "match" when !string.IsNullOrEmpty(request.Programme) =>
                    query.OrderByDescending(i =>
                        i.EligibleProgrammes != null && i.EligibleProgrammes.Contains(request.Programme) ? 1 : 0),
                _ => query.OrderByDescending(i => i.CreatedAt)
            };

            var totalCount = await query.CountAsync();

            var items = await query
                .Skip((request.PageNumber - 1) * request.PageSize)
                .Take(request.PageSize)
                .ToListAsync();

            var responses = new List<InternshipResponse>();
            foreach (var item in items)
            {
                var response = await MapToResponse(item);

                if (!string.IsNullOrEmpty(request.Programme))
                {
                    response.MatchScore = CalculateMatchScore(item, request.Programme);
                }

                responses.Add(response);
            }

            if (request.SortBy == "match" && !string.IsNullOrEmpty(request.Programme))
            {
                responses = responses.OrderByDescending(r => r.MatchScore).ToList();
            }

            return (responses, totalCount);
        }

        public async Task<List<InternshipResponse>> GetCompanyInternshipsAsync(int companyProfileId)
        {
            var internships = await _context.Internships
                .Include(i => i.CompanyProfile)
                .Where(i => i.CompanyProfileId == companyProfileId && !i.IsDeleted)
                .OrderByDescending(i => i.CreatedAt)
                .ToListAsync();

            var responses = new List<InternshipResponse>();
            foreach (var item in internships)
            {
                responses.Add(await MapToResponse(item));
            }

            return responses;
        }

        public async Task<bool> ModerateInternshipAsync(int internshipId, ModerationStatus status, string? notes)
        {
            var internship = await _context.Internships.FindAsync(internshipId);
            if (internship == null) return false;

            internship.ModerationStatus = status;
            internship.ModerationNotes = notes;
            internship.LastModeratedAt = DateTime.UtcNow;
            internship.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteInternshipAsync(int internshipId)
        {
            var internship = await _context.Internships.FindAsync(internshipId);
            if (internship == null) return false;

            internship.IsDeleted = true;
            internship.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CanUserApplyAsync(int internshipId, int userId)
        {
            var internship = await _context.Internships
                .FirstOrDefaultAsync(i => i.Id == internshipId &&
                    i.Status == InternshipStatus.Published &&
                    i.ModerationStatus == ModerationStatus.Approved &&
                    !i.IsDeleted);

            if (internship == null) return false;

            if (internship.ApplicationDeadline < DateTime.UtcNow) return false;

            var studentProfile = await _context.StudentProfiles
                .FirstOrDefaultAsync(s => s.UserId == userId);

            if (studentProfile == null) return false;

            var existingApplication = await _context.InternshipApplications
                .AnyAsync(a => a.StudentProfileId == studentProfile.Id &&
                               a.InternshipId == internshipId);

            return !existingApplication;
        }

        private int CalculateMatchScore(Internship internship, string studentProgramme)
        {
            if (string.IsNullOrEmpty(internship.EligibleProgrammes) || string.IsNullOrEmpty(studentProgramme))
                return 0;

            try
            {
                var eligibleProgrammes = JsonSerializer.Deserialize<List<string>>(internship.EligibleProgrammes);
                if (eligibleProgrammes == null || !eligibleProgrammes.Any())
                    return 0;

                if (eligibleProgrammes.Contains(studentProgramme, StringComparer.OrdinalIgnoreCase))
                    return 100;

                foreach (var programme in eligibleProgrammes)
                {
                    if (programme.Contains(studentProgramme, StringComparison.OrdinalIgnoreCase) ||
                        studentProgramme.Contains(programme, StringComparison.OrdinalIgnoreCase))
                        return 50;
                }

                return 0;
            }
            catch
            {
                return 0;
            }
        }

        private async Task<InternshipResponse> MapToResponse(Internship internship)
        {
            List<string>? eligibleProgrammes = null;
            if (!string.IsNullOrEmpty(internship.EligibleProgrammes))
            {
                try
                {
                    eligibleProgrammes = JsonSerializer.Deserialize<List<string>>(internship.EligibleProgrammes);
                }
                catch
                {
                    eligibleProgrammes = new List<string>();
                }
            }

            return new InternshipResponse
            {
                Id = internship.Id,
                CompanyProfileId = internship.CompanyProfileId,
                CompanyName = internship.CompanyProfile?.CompanyName ?? "Unknown Company",
                CompanyLogoUrl = internship.CompanyProfile?.LogoUrl,
                Title = internship.Title,
                Description = internship.Description,
                Responsibilities = internship.Responsibilities,
                Requirements = internship.Requirements,
                Qualifications = internship.Qualifications,
                Skills = internship.Skills,
                Industry = internship.Industry,
                Location = internship.Location,
                IsRemote = internship.IsRemote,
                InternshipType = internship.InternshipType.ToString(),
                Duration = internship.Duration.ToString(),
                StartDate = internship.StartDate,
                EndDate = internship.EndDate,
                ApplicationDeadline = internship.ApplicationDeadline,
                NumberOfPositions = internship.NumberOfPositions,
                Compensation = internship.Compensation,
                StipendAmount = internship.StipendAmount,
                Currency = internship.Currency,
                EligibleProgrammes = eligibleProgrammes ?? new List<string>(),
                Status = internship.Status.ToString(),
                PublishedAt = internship.PublishedAt,
                Views = internship.Views,
                ApplicationsCount = internship.ApplicationsCount,
                ModerationStatus = internship.ModerationStatus.ToString()
            };
        }
    }
}