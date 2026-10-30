using System.Text.RegularExpressions;
using InternshipManagement.Application.Interfaces;
using InternshipManagement.Domain.Enums;
using Microsoft.EntityFrameworkCore;

namespace InternshipManagement.Application.Services
{
    public class AgentToolsService : IAgentToolsService
    {
        private const int MaxSkills = 120;
        private const int MaxProgrammes = 40;
        private const int MaxListedTerms = 15;
        private static readonly char[] ListSeparators = { ',', ';', '\n', '|' };

        private static readonly Regex HtmlTag = new("<[^>]+>", RegexOptions.Compiled);
        private static readonly Regex MarkdownMarks = new(@"(\*\*|__|`|^\s{0,3}#{1,6}\s)", RegexOptions.Multiline | RegexOptions.Compiled);
        private static readonly Regex WordSplit = new("[^a-z0-9#+]+", RegexOptions.Compiled);

        // Words that say nothing about which role a title describes.
        private static readonly HashSet<string> TitleStopWords = new(StringComparer.Ordinal)
        {
            "intern", "interns", "internship", "internships", "trainee", "programme", "program",
            "the", "an", "and", "for", "of", "in", "at", "to", "with"
        };

        private readonly IApplicationDbContext _context;

        public AgentToolsService(IApplicationDbContext context)
        {
            _context = context;
        }

        // ==== Step 1: reference data =================================================================

        public async Task<PostingOptionsResponse> GetPostingOptionsAsync(CancellationToken cancellationToken = default)
        {
            var programmeText = await _context.Internships
                .AsNoTracking()
                .Where(i => i.EligibleProgrammes != null && i.EligibleProgrammes != "")
                .OrderBy(i => i.Id)
                .Select(i => i.EligibleProgrammes!)
                .Take(500)
                .ToListAsync(cancellationToken);

            var fields = await _context.Educations
                .AsNoTracking()
                .Where(e => e.FieldOfStudy != null && e.FieldOfStudy != "")
                .OrderBy(e => e.Id)
                .Select(e => e.FieldOfStudy!)
                .Take(500)
                .ToListAsync(cancellationToken);

            return new PostingOptionsResponse
            {
                InternshipTypes = Enum.GetNames<InternshipType>(),
                Durations = Enum.GetNames<InternshipDuration>(),
                KnownProgrammes = RankTerms(programmeText.Concat(fields), MaxProgrammes)
            };
        }

        public async Task<SkillVocabularyResponse> GetSkillVocabularyAsync(CancellationToken cancellationToken = default)
        {
            var studentSkills = await _context.Skills
                .AsNoTracking()
                .OrderBy(s => s.Id)
                .Select(s => s.Name)
                .Take(2000)
                .ToListAsync(cancellationToken);

            var postings = await _context.Internships
                .AsNoTracking()
                .OrderBy(i => i.Id)
                .Select(i => new { i.RequiredSkills, i.PreferredSkills, i.Skills })
                .Take(500)
                .ToListAsync(cancellationToken);

            var all = studentSkills
                .Concat(postings.SelectMany(p => new[] { p.RequiredSkills, p.PreferredSkills, p.Skills }));

            return new SkillVocabularyResponse { Skills = RankTerms(all, MaxSkills) };
        }

        // ==== Step 2a: self-check of a draft ============================================================

        /// <summary>
        /// Checks a draft against the same rules the API applies afterwards (see InternshipDraftService.Shape),
        /// so the agent can fix problems before answering instead of having its draft rejected.
        /// </summary>
        public ValidateInternshipResponse ValidateInternship(ValidateInternshipRequest request)
        {
            var issues = new List<DraftIssue>();

            var title = Cap(request.Title, 500);
            var description = Cap(request.Description, 5000);

            if (string.IsNullOrWhiteSpace(title))
                issues.Add(Issue("title", "Title is empty.", "Write a short, specific job title."));
            else if (title.Trim().Length > 100)
                issues.Add(Issue("title", $"Title is {title.Trim().Length} characters.", "Shorten it to 100 characters or fewer."));

            if (string.IsNullOrWhiteSpace(description))
            {
                issues.Add(Issue("description", "Description is empty.", "Write two short paragraphs about the role and the company's needs."));
            }
            else
            {
                var text = description.Trim();
                if (text.Length < 50)
                    issues.Add(Issue("description", $"Description is only {text.Length} characters.", "Write at least 50 characters, ideally two short paragraphs."));
                if (text.Length > 2000)
                    issues.Add(Issue("description", $"Description is {text.Length} characters.", "Shorten it to 2000 characters or fewer."));
                if (HtmlTag.IsMatch(text))
                    issues.Add(Issue("description", "Description contains HTML tags.", "Use plain text only."));
                if (MarkdownMarks.IsMatch(text))
                    issues.Add(Issue("description", "Description contains markdown formatting.", "Use plain text only, no **, backticks or # headings."));
            }

            var required = SplitTerms(Cap(request.RequiredSkills, 3000));
            var preferred = SplitTerms(Cap(request.PreferredSkills, 3000));
            CheckSkillList("requiredSkills", required, issues);
            CheckSkillList("preferredSkills", preferred, issues);

            var both = required.Intersect(preferred, StringComparer.OrdinalIgnoreCase).Take(5).ToList();
            if (both.Count > 0)
                issues.Add(Issue("preferredSkills", $"Listed as both required and preferred: {string.Join(", ", both)}.",
                    "Keep each term in one list only. If it is required, remove it from preferred."));

            AddEnumIssue<InternshipType>("internshipType", request.InternshipType, issues);
            AddEnumIssue<InternshipDuration>("duration", request.Duration, issues);

            return new ValidateInternshipResponse { IsValid = issues.Count == 0, Issues = issues };
        }

        private static void CheckSkillList(string field, List<string> terms, List<DraftIssue> issues)
        {
            if (terms.Count > MaxListedTerms)
                issues.Add(Issue(field, $"{terms.Count} terms listed.", $"Keep the {MaxListedTerms} most important terms."));

            var tooLong = terms
                .Where(t => t.Length > 40 || t.Split(' ', StringSplitOptions.RemoveEmptyEntries).Length > 4)
                .Take(3)
                .ToList();
            if (tooLong.Count > 0)
                issues.Add(Issue(field, $"These are sentences, not terms: {string.Join(" | ", tooLong.Select(t => t.Length > 50 ? t[..50] + "..." : t))}.",
                    "Use short terms of 1 to 4 words, such as \"SQL Server\" or \"Teamwork\"."));
        }

        private static void AddEnumIssue<T>(string field, string? value, List<DraftIssue> issues) where T : struct, Enum
        {
            var text = Cap(value, 100)?.Trim();
            if (string.IsNullOrEmpty(text) || text.Equals("null", StringComparison.OrdinalIgnoreCase))
                return;   // empty is allowed: the company picks it

            // A number such as "1" would parse as an enum value, so it is rejected explicitly.
            var valid = !int.TryParse(text, out _) && Enum.TryParse<T>(text, true, out var parsed) && Enum.IsDefined(parsed);
            if (!valid)
                issues.Add(Issue(field, $"'{text}' is not an allowed value.",
                    $"Use exactly one of: {string.Join(", ", Enum.GetNames<T>())}, or null if the company did not say."));
        }

        // ==== Step 2b: duplicate check (own company only) ===============================================

        public async Task<DuplicateCheckResponse> CheckDuplicateAsync(
            int userId, string? title, string? requiredSkills, CancellationToken cancellationToken = default)
        {
            var result = new DuplicateCheckResponse();

            var titleTokens = TitleTokens(Cap(title, 300));
            if (titleTokens.Count == 0) return result;

            var companyId = await GetCompanyIdAsync(userId, cancellationToken);
            if (companyId == null) return result;

            var skillTokens = SplitTerms(Cap(requiredSkills, 3000))
                .Select(s => s.ToLowerInvariant()).ToHashSet();

            var postings = await _context.Internships
                .AsNoTracking()
                .Where(i => i.CompanyProfileId == companyId)
                .OrderByDescending(i => i.Id)
                .Select(i => new { i.Id, i.Title, i.Status, i.RequiredSkills })
                .Take(200)
                .ToListAsync(cancellationToken);

            var drafts = await _context.InternshipDrafts
                .AsNoTracking()
                .Where(d => d.CompanyProfileId == companyId)
                .OrderByDescending(d => d.Id)
                .Select(d => new { d.Id, d.Title })
                .Take(50)
                .ToListAsync(cancellationToken);

            var matches = new List<DuplicateMatch>();

            foreach (var p in postings)
            {
                var score = Score(titleTokens, skillTokens, p.Title, p.RequiredSkills);
                if (score >= 0.55)
                    matches.Add(new DuplicateMatch
                    {
                        Kind = "internship",
                        Id = p.Id,
                        Title = p.Title,
                        Status = p.Status.ToString(),
                        SimilarityPercent = (int)Math.Round(score * 100)
                    });
            }

            foreach (var d in drafts)
            {
                var score = Score(titleTokens, skillTokens, d.Title, null);
                if (score >= 0.55)
                    matches.Add(new DuplicateMatch
                    {
                        Kind = "draft",
                        Id = d.Id,
                        Title = d.Title,
                        Status = "Saved draft",
                        SimilarityPercent = (int)Math.Round(score * 100)
                    });
            }

            result.Matches = matches.OrderByDescending(m => m.SimilarityPercent).Take(5).ToList();
            result.HasPossibleDuplicates = result.Matches.Count > 0;
            return result;
        }

        /// <summary>Title similarity, mixed with required-skill overlap when both sides have skills.</summary>
        private static double Score(HashSet<string> titleTokens, HashSet<string> skillTokens, string? otherTitle, string? otherSkills)
        {
            var titleScore = Jaccard(titleTokens, TitleTokens(otherTitle));
            var otherSkillTokens = SplitTerms(otherSkills).Select(s => s.ToLowerInvariant()).ToHashSet();

            if (skillTokens.Count == 0 || otherSkillTokens.Count == 0)
                return titleScore;

            return 0.7 * titleScore + 0.3 * Jaccard(skillTokens, otherSkillTokens);
        }

        // ==== Step 2c: suggestions ======================================================================

        public async Task<PostingSuggestionsResponse> GetPostingSuggestionsAsync(
            int userId, string? role, string? industry, CancellationToken cancellationToken = default)
        {
            var response = new PostingSuggestionsResponse();

            var company = await _context.CompanyProfiles
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .Select(c => new { c.Id, c.Industry })
                .FirstOrDefaultAsync(cancellationToken);

            if (company != null)
            {
                var recent = await _context.Internships
                    .AsNoTracking()
                    .Where(i => i.CompanyProfileId == company.Id)
                    .OrderByDescending(i => i.Id)
                    .Select(i => new { i.InternshipType, i.Duration, i.Location, i.IsRemote })
                    .Take(10)
                    .ToListAsync(cancellationToken);

                if (recent.Count > 0)
                {
                    response.CompanyDefaults = new CompanyPostingDefaults
                    {
                        InternshipType = recent.GroupBy(x => x.InternshipType).OrderByDescending(g => g.Count()).First().Key.ToString(),
                        Duration = recent.GroupBy(x => x.Duration).OrderByDescending(g => g.Count()).First().Key.ToString(),
                        Location = recent.Where(x => !string.IsNullOrWhiteSpace(x.Location))
                            .GroupBy(x => x.Location!.Trim(), StringComparer.OrdinalIgnoreCase)
                            .OrderByDescending(g => g.Count()).Select(g => g.Key).FirstOrDefault(),
                        IsRemote = recent.Count(x => x.IsRemote) * 2 > recent.Count
                    };
                }
            }

            var industryText = Cap(industry, 100)?.Trim();
            if (string.IsNullOrEmpty(industryText)) industryText = company?.Industry;

            var roleTokens = TitleTokens(Cap(role, 200)).Take(6).ToHashSet();

            var pool = await _context.Internships
                .AsNoTracking()
                .OrderByDescending(i => i.Id)
                .Select(i => new { i.Title, i.Industry, i.RequiredSkills, i.PreferredSkills })
                .Take(500)
                .ToListAsync(cancellationToken);

            var roleMatches = pool
                .Where(p => roleTokens.Count > 0 && TitleTokens(p.Title).Overlaps(roleTokens))
                .ToList();

            var industryMatches = pool
                .Where(p => !string.IsNullOrEmpty(industryText)
                            && string.Equals(p.Industry?.Trim(), industryText, StringComparison.OrdinalIgnoreCase))
                .ToList();

            response.RelatedSkills = TopSharedTerms(roleMatches.Select(p => new[] { p.RequiredSkills, p.PreferredSkills }));
            response.IndustrySkills = TopSharedTerms(industryMatches.Select(p => new[] { p.RequiredSkills, p.PreferredSkills }));

            if (response.RelatedSkills.Length == 0 && response.IndustrySkills.Length == 0)
                response.Note = "Not enough similar postings on the platform yet. Rely on the company's own description.";

            return response;
        }

        /// <summary>Terms used by at least two different postings, so one company's posting is never exposed on its own.</summary>
        private static string[] TopSharedTerms(IEnumerable<string?[]> postings)
        {
            var counts = new Dictionary<string, (string Display, int Count)>(StringComparer.OrdinalIgnoreCase);

            foreach (var posting in postings)
            {
                var terms = posting.SelectMany(field => SplitTerms(field)).Distinct(StringComparer.OrdinalIgnoreCase);
                foreach (var term in terms)
                {
                    counts[term] = counts.TryGetValue(term, out var existing)
                        ? (existing.Display, existing.Count + 1)
                        : (term, 1);
                }
            }

            return counts.Values
                .Where(v => v.Count >= 2)
                .OrderByDescending(v => v.Count)
                .ThenBy(v => v.Display, StringComparer.OrdinalIgnoreCase)
                .Take(MaxListedTerms)
                .Select(v => v.Display)
                .ToArray();
        }

        // ==== Helpers ===================================================================================

        private async Task<int?> GetCompanyIdAsync(int userId, CancellationToken cancellationToken)
        {
            return await _context.CompanyProfiles
                .AsNoTracking()
                .Where(c => c.UserId == userId)
                .Select(c => (int?)c.Id)
                .FirstOrDefaultAsync(cancellationToken);
        }

        private static DraftIssue Issue(string field, string problem, string fix)
            => new() { Field = field, Problem = problem, Fix = fix };

        private static string? Cap(string? value, int max)
            => value is { Length: > 0 } && value.Length > max ? value[..max] : value;

        private static HashSet<string> TitleTokens(string? title)
        {
            return WordSplit.Split((title ?? string.Empty).ToLowerInvariant())
                .Where(w => w.Length >= 2 && !TitleStopWords.Contains(w))
                .ToHashSet(StringComparer.Ordinal);
        }

        private static double Jaccard(HashSet<string> a, HashSet<string> b)
        {
            if (a.Count == 0 || b.Count == 0) return 0;
            var shared = a.Count(b.Contains);
            return (double)shared / (a.Count + b.Count - shared);
        }

        private static List<string> SplitTerms(string? value)
        {
            if (string.IsNullOrWhiteSpace(value)) return new List<string>();
            return value.Split(ListSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Where(t => t.Length >= 2)
                .ToList();
        }

        /// <summary>Splits comma/semicolon lists into short terms and ranks them by how often they appear.</summary>
        private static string[] RankTerms(IEnumerable<string?> values, int max)
        {
            return values
                .Where(v => !string.IsNullOrWhiteSpace(v))
                .SelectMany(v => v!.Split(ListSeparators, StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
                .Where(t => t.Length is >= 2 and <= 40)
                .GroupBy(t => t.ToLowerInvariant())
                .OrderByDescending(g => g.Count())
                .ThenBy(g => g.Key)
                .Take(max)
                .Select(g => g.GroupBy(x => x).OrderByDescending(x => x.Count()).First().Key)   // most common casing
                .ToArray();
        }
    }
}
