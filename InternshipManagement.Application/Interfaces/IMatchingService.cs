using InternshipManagement.Application.DTOs.Matching;

namespace InternshipManagement.Application.Interfaces
{
    public interface IMatchingService
    {
        Task<List<MatchRecommendationResponse>> RecommendInternshipsForStudentAsync(int userId, int maxResults = 10);
        Task<List<MatchRecommendationResponse>> RecommendStudentsForInternshipAsync(int userId, int internshipId, int maxResults = 10);
    }
}
