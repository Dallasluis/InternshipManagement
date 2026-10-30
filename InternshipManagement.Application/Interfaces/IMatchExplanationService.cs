using InternshipManagement.Application.DTOs.Matching;
using System.Threading;
using System.Threading.Tasks;

namespace InternshipManagement.Application.Interfaces
{
    public interface IMatchExplanationService
    {
        /// <summary>
        /// The student's best internship matches (ranked by the existing, deterministic MatchingService)
        /// with a plain-language explanation written by the AI. If the AI is unavailable the matches
        /// are still returned, without explanations.
        /// </summary>
        Task<ExplainedMatchesResponse> GetExplainedMatchesAsync(int userId, int maxResults, CancellationToken cancellationToken = default);
    }
}
