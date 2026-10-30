using InternshipManagement.Application.DTOs.Matching;
using InternshipManagement.Application.Interfaces;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Services
{
    public class MatchExplanationService : IMatchExplanationService
    {
        // Must match the Webhook node path in the n8n "match explain" workflow.
        private const string WorkflowPath = "matching/explain";
        private const string RunTokenPurpose = "match";

        private const int MaxMatches = 10;
        private const int MinExplanationChars = 20;
        private const int MaxExplanationChars = 400;

        private readonly IMatchingService _matching;
        private readonly IN8nClient _n8n;
        private readonly IRunTokenService _runTokens;

        public MatchExplanationService(IMatchingService matching, IN8nClient n8n, IRunTokenService runTokens)
        {
            _matching = matching;
            _n8n = n8n;
            _runTokens = runTokens;
        }

        public async Task<ExplainedMatchesResponse> GetExplainedMatchesAsync(
            int userId, int maxResults, CancellationToken cancellationToken = default)
        {
            var max = Math.Clamp(maxResults, 1, MaxMatches);

            // The ranking, scores and skill lists come from code, never from the AI.
            var matches = await _matching.RecommendInternshipsForStudentAsync(userId, max);

            var response = new ExplainedMatchesResponse
            {
                Matches = matches.Select(ToExplained).ToList()
            };

            if (response.Matches.Count == 0)
                return response;

            try
            {
                var raw = await _n8n.RunWorkflowAsync<N8nMatchExplainRequest, N8nMatchExplainPayload>(
                    WorkflowPath,
                    new N8nMatchExplainRequest
                    {
                        MaxResults = max,
                        RunToken = _runTokens.Create(userId, "Student", RunTokenPurpose)
                    },
                    cancellationToken);

                var byId = new Dictionary<int, string>();
                foreach (var item in raw.Explanations ?? new List<N8nMatchExplanation>())
                {
                    if (item == null) continue;
                    if (!int.TryParse(item.TargetId, NumberStyles.Integer, CultureInfo.InvariantCulture, out var id)) continue;

                    var text = TextCleaner.Clean(item.Explanation, MaxExplanationChars);
                    if (!IsAcceptable(text)) continue;

                    byId.TryAdd(id, text!);
                }

                // Only explanations for internships that really are in this student's list are used.
                foreach (var match in response.Matches)
                {
                    if (byId.TryGetValue(match.TargetId, out var text))
                        match.AiExplanation = text;
                }

                response.AiExplanationsAvailable = response.Matches.Any(m => m.AiExplanation != null);
            }
            catch (N8nException)
            {
                // The matches are still useful without the AI text.
            }

            return response;
        }

        private static bool IsAcceptable(string? text)
        {
            if (text == null || text.Length < MinExplanationChars) return false;

            // Plain sentences only: no links, no token-like leftovers.
            return !text.Contains("http", StringComparison.OrdinalIgnoreCase)
                && !text.Contains("runToken", StringComparison.OrdinalIgnoreCase);
        }

        private static ExplainedMatchResponse ToExplained(MatchRecommendationResponse m) => new()
        {
            TargetId = m.TargetId,
            TargetType = m.TargetType,
            Title = m.Title,
            Subtitle = m.Subtitle,
            MatchScore = m.MatchScore,
            MatchingSkills = m.MatchingSkills,
            MatchingQualifications = m.MatchingQualifications,
            MissingPreferredSkills = m.MissingPreferredSkills,
            Explanation = m.Explanation
        };
    }
}
