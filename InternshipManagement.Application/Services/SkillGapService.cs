using InternshipManagement.Application.DTOs.Student;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Entities;
using InternshipManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Services
{
    /// <summary>
    /// Skill gaps and certificate suggestions. The gaps and the suggested certifications are worked out by code
    /// (same term rules as MatchingService, so a gap here is exactly a term that scored nothing there). The AI only
    /// writes the plain-language text around them and can refer only to catalogue ids it was given.
    /// </summary>
    public class SkillGapService : ISkillGapService
    {
        // Must match the Webhook node path in the n8n "skill gaps" workflow.
        private const string WorkflowPath = "skills/gaps";

        private const int MaxTargets = 5;
        private const int MaxRecommendations = 6;
        private const int MaxGapsSentToAi = 30;
        private const int MaxGapsListed = 60;

        private const int MinWhyChars = 20;
        private const int MaxWhyChars = 300;
        private const int MinSummaryChars = 30;
        private const int MaxSummaryChars = 600;

        // Same separators as MatchingService, so both always agree on what a "term" is.
        private static readonly char[] Separators =
            { ',', ';', '\n', '\r', '|', '/', '\\', '.', ':', '-', '(', ')', '[', ']', '"', '\'' };

        private static readonly Regex NonWordChars = new(@"[^\p{L}\p{N}#+]+", RegexOptions.Compiled);

        private readonly IApplicationDbContext _context;
        private readonly IMatchingService _matching;
        private readonly IN8nClient _n8n;

        public SkillGapService(IApplicationDbContext context, IMatchingService matching, IN8nClient n8n)
        {
            _context = context;
            _matching = matching;
            _n8n = n8n;
        }

        // =====================================================================================
        // Deterministic part
        // =====================================================================================

        public async Task<SkillGapResponse> GetGapsAsync(
            int userId, int? internshipId, CancellationToken cancellationToken = default)
        {
            var student = await _context.StudentProfiles
                .AsNoTracking()
                .Include(s => s.Education)
                .Include(s => s.Skills)
                .Include(s => s.Certifications)
                .FirstOrDefaultAsync(s => s.UserId == userId && !s.IsDeleted, cancellationToken)
                ?? throw new InvalidOperationException("Student profile not found.");

            var (internships, scores) = await LoadTargetsAsync(userId, internshipId, cancellationToken);

            var response = new SkillGapResponse();
            if (internships.Count == 0)
            {
                response.Notes.Add("There are no open internships to compare with yet.");
                return response;
            }

            var studentSkills = Keys(student.Skills?.Select(s => s.Name));
            var studentQualifications = Keys(
                (student.Education?.SelectMany(e => new[] { e.Degree, e.FieldOfStudy, e.Institution }) ?? Enumerable.Empty<string?>())
                .Concat(student.Certifications?.Select(c => c.Name) ?? Enumerable.Empty<string?>())
                .Concat(string.IsNullOrWhiteSpace(student.Programme) ? Enumerable.Empty<string?>() : new[] { student.Programme }));

            var aggregate = new Dictionary<(string Key, string Kind), GapAccumulator>();

            foreach (var internship in internships)
            {
                var requiredSkills = Parse(internship.RequiredSkills, internship.Skills);
                var preferredSkills = Parse(internship.PreferredSkills);
                var requiredQualifications = Parse(internship.RequiredQualifications, internship.Qualifications, internship.EligibleProgrammes);
                var preferredQualifications = Parse(internship.PreferredQualifications);

                // A term listed as both required and preferred counts as required.
                foreach (var key in requiredSkills.Keys) preferredSkills.Remove(key);
                foreach (var key in requiredQualifications.Keys) preferredQualifications.Remove(key);

                var target = new SkillGapTarget
                {
                    InternshipId = internship.Id,
                    Title = internship.Title,
                    Company = internship.CompanyProfile?.CompanyName,
                    MatchScore = scores.TryGetValue(internship.Id, out var score) ? score : null,
                    MissingRequiredSkills = Missing(requiredSkills, studentSkills),
                    MissingPreferredSkills = Missing(preferredSkills, studentSkills),
                    MissingRequiredQualifications = Missing(requiredQualifications, studentQualifications),
                    MissingPreferredQualifications = Missing(preferredQualifications, studentQualifications)
                };
                response.Targets.Add(target);

                Add(aggregate, requiredSkills, studentSkills, "Skill", required: true);
                Add(aggregate, preferredSkills, studentSkills, "Skill", required: false);
                Add(aggregate, requiredQualifications, studentQualifications, "Qualification", required: true);
                Add(aggregate, preferredQualifications, studentQualifications, "Qualification", required: false);
            }

            response.Gaps = aggregate
                .Select(kv => new SkillGapItem
                {
                    Term = kv.Value.Display,
                    Kind = kv.Key.Kind,
                    Importance = kv.Value.Required ? "Required" : "Preferred",
                    InternshipCount = kv.Value.Count
                })
                .OrderBy(g => g.Importance == "Required" ? 0 : 1)
                .ThenByDescending(g => g.InternshipCount)
                .ThenBy(g => g.Term, StringComparer.OrdinalIgnoreCase)
                .Take(MaxGapsListed)
                .ToList();

            response.Recommendations = Recommend(response.Gaps, student.Certifications?.Select(c => c.Name) ?? Enumerable.Empty<string>());

            if (response.Gaps.Count == 0)
                response.Notes.Add("Nothing is missing from your profile for these internships. Well done.");
            else
                response.Notes.Add("Gaps are skills or qualifications the internship lists that are not on your profile. " +
                                   "If you have one under a different spelling (for example \"C Sharp\" instead of \"C#\"), rename it on your profile.");

            if (response.Gaps.Count > 0 && response.Recommendations.Count == 0)
                response.Notes.Add("None of the certifications in our list cover these gaps. Practice, projects or a course may be the better route.");

            if (response.Recommendations.Count > 0)
                response.Notes.Add("Certifications are suggestions from a fixed list. Check the provider's website for current content, cost and exam details.");

            return response;
        }

        private async Task<(List<Internship> Internships, Dictionary<int, int> Scores)> LoadTargetsAsync(
            int userId, int? internshipId, CancellationToken cancellationToken)
        {
            var matches = await _matching.RecommendInternshipsForStudentAsync(userId, 50);
            var scores = matches.GroupBy(m => m.TargetId).ToDictionary(g => g.Key, g => g.First().MatchScore);

            List<int> ids;
            if (internshipId.HasValue)
                ids = new List<int> { internshipId.Value };
            else
                ids = matches.Take(MaxTargets).Select(m => m.TargetId).ToList();

            if (ids.Count == 0)
                return (new List<Internship>(), scores);

            var now = DateTime.UtcNow;
            var found = await _context.Internships
                .AsNoTracking()
                .Include(i => i.CompanyProfile)
                .Where(i => ids.Contains(i.Id)
                    && i.Status == InternshipStatus.Published
                    && i.ModerationStatus == ModerationStatus.Approved
                    && (i.CompanyProfile.VerificationStatus == CompanyVerificationStatus.Verified
                        || (i.CompanyProfile.VerificationStatus == CompanyVerificationStatus.ReverificationRequired
                            && i.CompanyProfile.ReverificationRequiredAt >= DateTime.UtcNow.AddDays(-7)))
                    && i.ApplicationDeadline >= now
                    && !i.IsDeleted)
                .ToListAsync(cancellationToken);

            if (internshipId.HasValue && found.Count == 0)
                throw new InvalidOperationException("That internship was not found or is no longer open.");

            // keep the match order
            var ordered = ids.Select(id => found.FirstOrDefault(i => i.Id == id)).Where(i => i != null).Select(i => i!).ToList();
            return (ordered, scores);
        }

        // ---- terms --------------------------------------------------------------------------

        /// <summary>Key (lower case) to display text (as written), split exactly like MatchingService.Terms.</summary>
        private static Dictionary<string, string> Parse(params string?[] values)
        {
            var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            foreach (var value in values)
            {
                if (string.IsNullOrWhiteSpace(value)) continue;
                foreach (var token in value.Split(Separators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                {
                    var key = token.Trim().ToLowerInvariant();
                    if (key.Length >= 2) result.TryAdd(key, token.Trim());
                }
            }
            return result;
        }

        private static HashSet<string> Keys(IEnumerable<string?>? values)
        {
            return Parse((values ?? Enumerable.Empty<string?>()).ToArray()).Keys.ToHashSet(StringComparer.OrdinalIgnoreCase);
        }

        private static List<string> Missing(Dictionary<string, string> expected, HashSet<string> actual)
        {
            return expected
                .Where(kv => !actual.Contains(kv.Key))
                .Select(kv => kv.Value)
                .OrderBy(v => v, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private sealed class GapAccumulator
        {
            public string Display { get; init; } = string.Empty;
            public bool Required { get; set; }
            public int Count { get; set; }
        }

        private static void Add(
            Dictionary<(string Key, string Kind), GapAccumulator> aggregate,
            Dictionary<string, string> expected, HashSet<string> actual, string kind, bool required)
        {
            foreach (var (key, display) in expected)
            {
                if (actual.Contains(key)) continue;

                if (!aggregate.TryGetValue((key, kind), out var item))
                {
                    item = new GapAccumulator { Display = display };
                    aggregate[(key, kind)] = item;
                }

                item.Count++;
                if (required) item.Required = true;
            }
        }

        // ---- certification suggestions -------------------------------------------------------

        private static string[] Words(string value)
        {
            return NonWordChars.Replace(value.ToLowerInvariant(), " ")
                .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        }

        /// <summary>True when one phrase appears inside the other as whole words ("cloud" and "cloud computing").</summary>
        private static bool PhraseOverlap(string[] a, string[] b)
        {
            if (a.Length == 0 || b.Length == 0) return false;
            return Contains(a, b) || Contains(b, a);

            static bool Contains(string[] whole, string[] part)
            {
                if (part.Length > whole.Length) return false;
                for (var i = 0; i <= whole.Length - part.Length; i++)
                {
                    var same = true;
                    for (var j = 0; j < part.Length; j++)
                    {
                        if (!string.Equals(whole[i + j], part[j], StringComparison.Ordinal)) { same = false; break; }
                    }
                    if (same) return true;
                }
                return false;
            }
        }

        private static List<CertificationRecommendation> Recommend(IEnumerable<SkillGapItem> gaps, IEnumerable<string> heldCertificationNames)
        {
            var held = heldCertificationNames.Select(Words).Where(w => w.Length > 0).ToList();
            var gapList = gaps.Select(g => (Gap: g, Words: Words(g.Term))).Where(x => x.Words.Length > 0).ToList();

            var scored = new List<(CatalogueCertification Cert, double Score, List<string> Covers)>();

            foreach (var cert in CertificationCatalogue.All)
            {
                if (AlreadyHeld(cert, held)) continue;

                var topicWords = cert.Topics.Select(Words).ToList();
                var covers = new List<string>();
                double score = 0;

                foreach (var (gap, words) in gapList)
                {
                    if (!topicWords.Any(t => PhraseOverlap(t, words))) continue;

                    covers.Add(gap.Term);
                    score += (gap.Importance == "Required" ? 3.0 : 1.0) * gap.InternshipCount;
                }

                if (covers.Count > 0)
                    scored.Add((cert, score, covers));
            }

            return scored
                .OrderByDescending(s => s.Score)
                .ThenByDescending(s => s.Covers.Count)
                .ThenBy(s => LevelRank(s.Cert.Level))      // easier ones first when equal
                .ThenBy(s => s.Cert.Name, StringComparer.OrdinalIgnoreCase)
                .Take(MaxRecommendations)
                .Select(s => new CertificationRecommendation
                {
                    Id = s.Cert.Id,
                    Name = s.Cert.Name,
                    Provider = s.Cert.Provider,
                    Level = s.Cert.Level,
                    Covers = s.Covers.Take(8).ToList()
                })
                .ToList();
        }

        private static bool AlreadyHeld(CatalogueCertification cert, List<string[]> held)
        {
            if (held.Count == 0) return false;

            var names = new List<string[]> { Words(cert.Name) };
            names.AddRange(cert.Aliases.Where(a => a.Length >= 3).Select(Words));

            return held.Any(h => names.Any(n => PhraseOverlap(h, n)));
        }

        private static int LevelRank(string level) => level switch
        {
            "Beginner" => 0,
            "Intermediate" => 1,
            _ => 2
        };

        // =====================================================================================
        // AI part: text around the deterministic result
        // =====================================================================================

        public async Task<SkillGapResponse> AnalyzeAsync(
            int userId, int? internshipId, CancellationToken cancellationToken = default)
        {
            var response = await GetGapsAsync(userId, internshipId, cancellationToken);

            if (response.Recommendations.Count == 0)
                return response;   // nothing for the AI to explain

            try
            {
                var request = new N8nSkillGapRequest
                {
                    Targets = response.Targets.Select(t => t.Title).Take(MaxTargets).ToList(),
                    Gaps = response.Gaps.Take(MaxGapsSentToAi).Select(g => new N8nGapTerm
                    {
                        Term = g.Term,
                        Importance = g.Importance,
                        Internships = g.InternshipCount
                    }).ToList(),
                    Certifications = response.Recommendations.Select(r => new N8nCertOption
                    {
                        Id = r.Id,
                        Name = r.Name,
                        Provider = r.Provider,
                        Level = r.Level,
                        Covers = r.Covers
                    }).ToList()
                };

                var raw = await _n8n.RunWorkflowAsync<N8nSkillGapRequest, N8nSkillGapPayload>(
                    WorkflowPath, request, cancellationToken);

                var summary = TextCleaner.Clean(raw.Summary, MaxSummaryChars);
                if (IsAcceptable(summary, MinSummaryChars))
                    response.AiSummary = summary;

                var byId = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
                foreach (var item in raw.Items ?? new List<N8nSkillGapItem>())
                {
                    if (item == null || string.IsNullOrWhiteSpace(item.Id)) continue;

                    var why = TextCleaner.Clean(item.Why, MaxWhyChars);
                    if (!IsAcceptable(why, MinWhyChars)) continue;

                    byId.TryAdd(item.Id.Trim(), why!);
                }

                // Only ids that really are in this student's list are used.
                foreach (var recommendation in response.Recommendations)
                {
                    if (byId.TryGetValue(recommendation.Id, out var text))
                        recommendation.Why = text;
                }

                response.AiAvailable = response.AiSummary != null || response.Recommendations.Any(r => r.Why != null);
            }
            catch (N8nException)
            {
                // The gaps and certifications are still useful without the AI text.
            }

            return response;
        }

        private static bool IsAcceptable(string? text, int minLength)
        {
            if (text == null || text.Length < minLength) return false;

            return !text.Contains("http", StringComparison.OrdinalIgnoreCase)
                && !text.Contains("runToken", StringComparison.OrdinalIgnoreCase);
        }
    }
}
