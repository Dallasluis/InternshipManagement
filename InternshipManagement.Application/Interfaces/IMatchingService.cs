using InternshipManagement.Application.DTOs.Matching;

namespace InternshipManagement.Application.Interfaces
{
    public interface IMatchingService
    {
        Task<List<MatchRecommendationResponse>> RecommendInternshipsForStudentAsync(int userId, int maxResults = 10);
        Task<List<MatchRecommendationResponse>> RecommendStudentsForInternshipAsync(int userId, int internshipId, int maxResults = 10);

        /// <summary>
        /// Scores every student against one internship and returns those at or above <paramref name="minScore"/>
        /// that match at least one skill or qualification. Used for "new match" notifications.
        /// </summary>
        Task<List<StudentMatchScore>> ScoreStudentsForInternshipAsync(int internshipId, int minScore, CancellationToken cancellationToken = default);
    }
}
