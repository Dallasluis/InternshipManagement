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
        private readonly INotificationService _notifications;

        public InternshipService(IApplicationDbContext context, IConfiguration configuration, INotificationService notifications)
        {
            _context = context;
            _configuration = configuration;
            _notifications = notifications;
        }

        public async Task<InternshipResponse> CreateInternshipAsync(int userId, CreateInternshipRequest request)
        {
            var companyProfile = await _context.CompanyProfiles
                .FirstOrDefaultAsync(c => c.UserId == userId && !c.IsDeleted);

            if (companyProfile == null)
                throw new Exception("Company profile not found.");

            if (companyProfile.VerificationStatus != CompanyVerificationStatus.Verified)
                throw new InvalidOperationException("Company verification is required before posting internships.");

            if (_configuration.GetValue<bool>("Internships:RequireSubscription") && !companyProfile.IsSubscribed)
                throw new Exception("Active subscription required.");

            if (!string.Equals(request.InternshipType, nameof(InternshipType.FullTime), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only full-time internships can be posted.");
            var workArrangement = ParseWorkArrangement(request.WorkArrangement, request.IsRemote);

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
                RequiredQualifications = request.RequiredQualifications,
                PreferredQualifications = request.PreferredQualifications,
                RequiredSkills = request.RequiredSkills,
                PreferredSkills = request.PreferredSkills,
                Industry = request.Industry,
                Location = request.Location,
                IsRemote = workArrangement != InternshipWorkArrangement.OnSite,
                InternshipType = InternshipType.FullTime,
                WorkArrangement = workArrangement,
                Duration = Enum.Parse<InternshipDuration>(request.Duration),
                StartDate = request.StartDate,
                EndDate = request.EndDate,
                ApplicationDeadline = request.ApplicationDeadline,
                NumberOfPositions = request.NumberOfPositions,
                Compensation = request.Compensation,
                StipendAmount = request.StipendAmount,
                Currency = request.Currency,
                EligibleProgrammes = eligibleProgrammesJson,
                Status = InternshipStatus.Published,
                PublishedAt = DateTime.UtcNow,
                ModerationStatus = ModerationStatus.Approved
            };

            _context.Internships.Add(internship);
            await _context.SaveChangesAsync();

            return await MapToResponse(internship);
        }

        public async Task<InternshipResponse?> UpdateInternshipAsync(int userId, int internshipId, UpdateInternshipRequest request)
        {
            var internship = await _context.Internships
                .Include(i => i.CompanyProfile)
                .FirstOrDefaultAsync(i => i.Id == internshipId
                    && !i.IsDeleted
                    && i.CompanyProfile.UserId == userId
                    && !i.CompanyProfile.IsDeleted);

            if (internship == null)
                return null;

            if (internship.Status != InternshipStatus.Draft)
                throw new Exception("Only draft internships can be edited.");

            if (!string.Equals(request.InternshipType, nameof(InternshipType.FullTime), StringComparison.OrdinalIgnoreCase))
                throw new InvalidOperationException("Only full-time internships can be posted.");
            var workArrangement = ParseWorkArrangement(request.WorkArrangement, request.IsRemote);

            var eligibleProgrammesJson = request.EligibleProgrammes != null && request.EligibleProgrammes.Any()
                ? JsonSerializer.Serialize(request.EligibleProgrammes)
                : null;

            internship.Title = request.Title;
            internship.Description = request.Description;
            internship.Responsibilities = request.Responsibilities;
            internship.Requirements = request.Requirements;
            internship.Qualifications = request.Qualifications;
            internship.Skills = request.Skills;
            internship.RequiredQualifications = request.RequiredQualifications;
            internship.PreferredQualifications = request.PreferredQualifications;
            internship.RequiredSkills = request.RequiredSkills;
            internship.PreferredSkills = request.PreferredSkills;
            internship.Industry = request.Industry;
            internship.Location = request.Location;
            internship.IsRemote = workArrangement != InternshipWorkArrangement.OnSite;
            internship.InternshipType = InternshipType.FullTime;
            internship.WorkArrangement = workArrangement;
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

        public async Task<bool> PublishInternshipAsync(int userId, int internshipId)
        {
            var internship = await _context.Internships
                .Include(i => i.CompanyProfile)
                .FirstOrDefaultAsync(i => i.Id == internshipId
                    && !i.IsDeleted
                    && i.CompanyProfile.UserId == userId
                    && !i.CompanyProfile.IsDeleted);
            if (internship == null) return false;

            if (internship.Status != InternshipStatus.Draft)
                throw new Exception("Only draft internships can be published.");

            if (internship.CompanyProfile.VerificationStatus != CompanyVerificationStatus.Verified)
                throw new InvalidOperationException("Company verification is required before publishing internships.");

            if (_configuration.GetValue<bool>("Internships:RequireSubscription") && !internship.CompanyProfile.IsSubscribed)
                throw new InvalidOperationException("An active subscription is required before publishing internships.");

            if (internship.ApplicationDeadline < DateTime.UtcNow)
                throw new Exception("Cannot publish internship with past application deadline.");

            internship.Status = InternshipStatus.Published;
            internship.PublishedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> CloseInternshipAsync(int userId, int internshipId)
        {
            var internship = await _context.Internships
                .FirstOrDefaultAsync(i => i.Id == internshipId
                    && !i.IsDeleted
                    && i.CompanyProfile.UserId == userId
                    && !i.CompanyProfile.IsDeleted);
            if (internship == null) return false;

            if (internship.Status != InternshipStatus.Published)
                throw new Exception("Only published internships can be closed.");

            internship.Status = InternshipStatus.Closed;
            internship.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<InternshipResponse?> GetInternshipByIdAsync(int id, int? requestingUserId = null)
        {
            var internship = await _context.Internships
                .Include(i => i.CompanyProfile)
                .FirstOrDefaultAsync(i => i.Id == id
                    && !i.IsDeleted
                    && ((i.Status == InternshipStatus.Published
                            && i.ModerationStatus != ModerationStatus.Flagged
                            && i.ModerationStatus != ModerationStatus.Removed
                            && i.ModerationStatus != ModerationStatus.Rejected
                            && (i.CompanyProfile.VerificationStatus == CompanyVerificationStatus.Verified
                                || (i.CompanyProfile.VerificationStatus == CompanyVerificationStatus.ReverificationRequired
                                    && i.CompanyProfile.ReverificationRequiredAt >= DateTime.UtcNow.AddDays(-7)))
                            && i.ApplicationDeadline >= DateTime.UtcNow)
                        || (requestingUserId.HasValue && i.CompanyProfile.UserId == requestingUserId.Value)));

            if (internship == null) return null;

            if (internship.Status == InternshipStatus.Published
                && internship.ModerationStatus != ModerationStatus.Flagged
                && internship.ModerationStatus != ModerationStatus.Removed
                && internship.ModerationStatus != ModerationStatus.Rejected
                && (internship.CompanyProfile.VerificationStatus == CompanyVerificationStatus.Verified
                    || (internship.CompanyProfile.VerificationStatus == CompanyVerificationStatus.ReverificationRequired
                        && internship.CompanyProfile.ReverificationRequiredAt >= DateTime.UtcNow.AddDays(-7)))
                && internship.ApplicationDeadline >= DateTime.UtcNow)
            {
                internship.Views++;
                await _context.SaveChangesAsync();
            }

            return await MapToResponse(internship);
        }

        public async Task<(List<InternshipResponse> Items, int TotalCount)> SearchInternshipsAsync(InternshipSearchRequest request)
        {
            var query = _context.Internships
                .Include(i => i.CompanyProfile)
                .Where(i => i.Status == InternshipStatus.Published
                    && i.ModerationStatus != ModerationStatus.Flagged
                    && i.ModerationStatus != ModerationStatus.Removed
                    && i.ModerationStatus != ModerationStatus.Rejected
                    && (i.CompanyProfile.VerificationStatus == CompanyVerificationStatus.Verified
                        || (i.CompanyProfile.VerificationStatus == CompanyVerificationStatus.ReverificationRequired
                            && i.CompanyProfile.ReverificationRequiredAt >= DateTime.UtcNow.AddDays(-7)))
                    && !i.IsDeleted
                    && i.ApplicationDeadline >= DateTime.UtcNow);

            // every word typed must appear somewhere in the posting (in any field)
            var terms = SearchTerms.Parse(request.Keyword);
            foreach (var term in terms)
            {
                var stem = term.Stem;
                query = query.Where(i =>
                    i.Title.Contains(stem) ||
                    i.Description.Contains(stem) ||
                    (i.Skills != null && i.Skills.Contains(stem)) ||
                    (i.RequiredSkills != null && i.RequiredSkills.Contains(stem)) ||
                    (i.PreferredSkills != null && i.PreferredSkills.Contains(stem)) ||
                    (i.RequiredQualifications != null && i.RequiredQualifications.Contains(stem)) ||
                    (i.PreferredQualifications != null && i.PreferredQualifications.Contains(stem)) ||
                    i.Industry.Contains(stem) ||
                    i.CompanyProfile.CompanyName.Contains(stem));
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

            if (!string.IsNullOrWhiteSpace(request.WorkArrangement))
            {
                var arrangement = ParseWorkArrangement(request.WorkArrangement);
                query = query.Where(i => i.WorkArrangement == arrangement);
            }

            if (request.MinStipend.HasValue)
            {
                query = query.Where(i => i.StipendAmount >= request.MinStipend);
            }

            if (request.IsRemote.HasValue)
            {
                query = query.Where(i => (i.WorkArrangement != InternshipWorkArrangement.OnSite) == request.IsRemote);
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

            // with search words and no other sort chosen, the best matches come first
            var byRelevance = terms.Count > 0
                && (string.IsNullOrWhiteSpace(request.SortBy) || request.SortBy == "relevance");

            var items = byRelevance
                ? await GetRelevancePageAsync(query, terms, request)
                : await query
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

        // Ranks the newest matching postings (up to 500) by where the words were found, then returns one page.
        private async Task<List<Internship>> GetRelevancePageAsync(
            IQueryable<Internship> query, List<SearchTerm> terms, InternshipSearchRequest request)
        {
            const int CandidateLimit = 500;

            var candidates = await query
                .OrderByDescending(i => i.CreatedAt)
                .Take(CandidateLimit)
                .Select(i => new
                {
                    i.Id,
                    i.CreatedAt,
                    i.Title,
                    i.Skills,
                    i.RequiredSkills,
                    i.PreferredSkills,
                    i.Industry,
                    Company = i.CompanyProfile.CompanyName
                })
                .ToListAsync();

            static bool Has(string? text, string stem) =>
                !string.IsNullOrEmpty(text) && text.Contains(stem, StringComparison.OrdinalIgnoreCase);

            var phrase = string.Join(' ', terms.Select(t => t.Word));

            var pageIds = candidates
                .Select(c =>
                {
                    var score = Has(c.Title, phrase) ? 5 : 0; // the whole search typed as one phrase, in the title
                    foreach (var t in terms)
                    {
                        if (Has(c.Title, t.Stem)) score += 5;
                        if (Has(c.RequiredSkills, t.Stem) || Has(c.Skills, t.Stem)) score += 4;
                        else if (Has(c.PreferredSkills, t.Stem)) score += 2;
                        if (Has(c.Industry, t.Stem) || Has(c.Company, t.Stem)) score += 2;
                        score += 1; // it matched somewhere, even if only in the description
                    }

                    return new { c.Id, c.CreatedAt, Score = score };
                })
                .OrderByDescending(c => c.Score)
                .ThenByDescending(c => c.CreatedAt)
                .Skip((Math.Max(request.PageNumber, 1) - 1) * request.PageSize)
                .Take(request.PageSize)
                .Select(c => c.Id)
                .ToList();

            var items = await _context.Internships
                .Include(i => i.CompanyProfile)
                .Where(i => pageIds.Contains(i.Id))
                .ToListAsync();

            return items.OrderBy(i => pageIds.IndexOf(i.Id)).ToList();
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

            if (status != ModerationStatus.Pending)
            {
                var companyUserId = await _context.CompanyProfiles
                    .Where(c => c.Id == internship.CompanyProfileId)
                    .Select(c => c.UserId)
                    .FirstOrDefaultAsync();
                var approved = status == ModerationStatus.Approved;
                await _notifications.NotifyAsync(companyUserId, NotificationTypes.Moderation,
                    approved ? "Internship approved" : "Internship needs attention",
                    approved
                        ? $"\"{internship.Title}\" was approved."
                        : $"\"{internship.Title}\" was marked {status}." + (string.IsNullOrWhiteSpace(notes) ? string.Empty : $" Note: {notes}"),
                    "/Company/ManageInternships", sendEmail: !approved);
            }

            return true;
        }

        public async Task<bool> DeleteInternshipAsync(int userId, int internshipId)
        {
            var internship = await _context.Internships
                .FirstOrDefaultAsync(i => i.Id == internshipId
                    && !i.IsDeleted
                    && i.CompanyProfile.UserId == userId
                    && !i.CompanyProfile.IsDeleted);
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
                    i.ModerationStatus != ModerationStatus.Flagged &&
                    i.ModerationStatus != ModerationStatus.Removed &&
                    i.ModerationStatus != ModerationStatus.Rejected &&
                    (i.CompanyProfile.VerificationStatus == CompanyVerificationStatus.Verified ||
                        (i.CompanyProfile.VerificationStatus == CompanyVerificationStatus.ReverificationRequired &&
                            i.CompanyProfile.ReverificationRequiredAt >= DateTime.UtcNow.AddDays(-7))) &&
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
                CompanyIsVerified = internship.CompanyProfile?.VerificationStatus == CompanyVerificationStatus.Verified,
                Title = internship.Title,
                Description = internship.Description,
                Responsibilities = internship.Responsibilities,
                Requirements = internship.Requirements,
                Qualifications = internship.Qualifications,
                Skills = internship.Skills,
                RequiredQualifications = internship.RequiredQualifications,
                PreferredQualifications = internship.PreferredQualifications,
                RequiredSkills = internship.RequiredSkills,
                PreferredSkills = internship.PreferredSkills,
                Industry = internship.Industry,
                Location = internship.Location,
                IsRemote = internship.WorkArrangement != InternshipWorkArrangement.OnSite,
                WorkArrangement = internship.WorkArrangement.ToString(),
                InternshipType = InternshipType.FullTime.ToString(),
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
                UpdatedAt = internship.UpdatedAt,
                Views = internship.Views,
                ApplicationsCount = internship.ApplicationsCount,
                ModerationStatus = internship.ModerationStatus.ToString()
            };
        }

        private static InternshipWorkArrangement ParseWorkArrangement(string? value, bool legacyIsRemote = false)
        {
            if (string.IsNullOrWhiteSpace(value))
                return legacyIsRemote ? InternshipWorkArrangement.Remote : InternshipWorkArrangement.OnSite;

            if (Enum.TryParse<InternshipWorkArrangement>(value, true, out var arrangement)
                && Enum.IsDefined(arrangement))
            {
                return arrangement;
            }

            throw new InvalidOperationException("Work arrangement must be OnSite, Remote, or Hybrid.");
        }
    }
}
