using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;

namespace InternshipManagement.Application.Services
{
    /// <summary>
    /// Finds internships that were published and approved recently, scores the students against them with the same
    /// matching used elsewhere, and notifies the students who fit well. A student is told about an internship once,
    /// never about one they already applied to, and never more than a few times a day.
    /// </summary>
    public class RecommendationNotifier : IRecommendationNotifier
    {
        private const int MaxInternshipsPerRun = 25;

        private readonly IApplicationDbContext _context;
        private readonly IMatchingService _matching;
        private readonly INotificationService _notifications;
        private readonly IConfiguration _configuration;
        private readonly ILogger<RecommendationNotifier> _logger;

        public RecommendationNotifier(
            IApplicationDbContext context,
            IMatchingService matching,
            INotificationService notifications,
            IConfiguration configuration,
            ILogger<RecommendationNotifier> logger)
        {
            _context = context;
            _matching = matching;
            _notifications = notifications;
            _configuration = configuration;
            _logger = logger;
        }

        public async Task<int> NotifyNewMatchesAsync(CancellationToken cancellationToken = default)
        {
            var minScore = Math.Clamp(_configuration.GetValue("Notifications:Recommendations:MinScore", 70), 1, 100);
            var perDay = Math.Clamp(_configuration.GetValue("Notifications:Recommendations:MaxPerStudentPerDay", 3), 1, 20);
            var lookbackDays = Math.Clamp(_configuration.GetValue("Notifications:Recommendations:LookbackDays", 3), 1, 14);

            var now = DateTime.UtcNow;
            var since = now.AddDays(-lookbackDays);
            var dayAgo = now.AddHours(-24);

            var internships = await _context.Internships
                .AsNoTracking()
                .Where(i => !i.IsDeleted
                    && i.Status == InternshipStatus.Published
                    && i.ModerationStatus != ModerationStatus.Flagged
                    && i.ModerationStatus != ModerationStatus.Removed
                    && i.ModerationStatus != ModerationStatus.Rejected
                    && i.PublishedAt != null && i.PublishedAt >= since
                    && i.ApplicationDeadline >= now
                    && !i.CompanyProfile.IsDeleted
                    && i.CompanyProfile.VerificationStatus == CompanyVerificationStatus.Verified)
                .OrderBy(i => i.PublishedAt)
                .Select(i => new { i.Id, i.Title, Company = i.CompanyProfile.CompanyName })
                .Take(MaxInternshipsPerRun)
                .ToListAsync(cancellationToken);

            var created = 0;

            foreach (var internship in internships)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var link = $"/Internships/Details/{internship.Id}";

                var matches = await _matching.ScoreStudentsForInternshipAsync(internship.Id, minScore, cancellationToken);
                if (matches.Count == 0) continue;

                // told already, or already applied: leave them alone
                var skip = (await _context.Notifications.AsNoTracking()
                        .Where(n => n.Type == NotificationTypes.Recommendation && n.LinkPath == link)
                        .Select(n => n.UserId)
                        .ToListAsync(cancellationToken))
                    .ToHashSet();

                skip.UnionWith(await _context.InternshipApplications.AsNoTracking()
                    .Where(a => a.InternshipId == internship.Id && !a.IsDeleted)
                    .Select(a => a.StudentProfile.UserId)
                    .ToListAsync(cancellationToken));

                var candidates = matches
                    .Where(m => !skip.Contains(m.UserId))
                    .OrderByDescending(m => m.Score)
                    .ToList();
                if (candidates.Count == 0) continue;

                var candidateIds = candidates.Select(c => c.UserId).ToList();
                var today = (await _context.Notifications.AsNoTracking()
                        .Where(n => n.Type == NotificationTypes.Recommendation
                            && n.CreatedAt >= dayAgo
                            && candidateIds.Contains(n.UserId))
                        .GroupBy(n => n.UserId)
                        .Select(g => new { UserId = g.Key, Count = g.Count() })
                        .ToListAsync(cancellationToken))
                    .ToDictionary(x => x.UserId, x => x.Count);

                foreach (var candidate in candidates)
                {
                    if (today.GetValueOrDefault(candidate.UserId) >= perDay)
                        continue; // they will be considered again on a later run while the posting is still new

                    var because = candidate.MatchingSkills.Count > 0
                        ? $" Matched on: {string.Join(", ", candidate.MatchingSkills.Take(3))}."
                        : string.Empty;

                    await _notifications.NotifyAsync(
                        candidate.UserId,
                        NotificationTypes.Recommendation,
                        "New internship that fits your profile",
                        $"{internship.Title} at {internship.Company} is a {candidate.Score}% match.{because}",
                        link,
                        sendEmail: false,
                        cancellationToken);

                    today[candidate.UserId] = today.GetValueOrDefault(candidate.UserId) + 1;
                    created++;
                }
            }

            if (created > 0)
                _logger.LogInformation("Created {Count} new-match notifications.", created);

            return created;
        }
    }
}
