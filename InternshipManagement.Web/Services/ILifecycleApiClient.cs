using InternshipManagement.Web.Models.Lifecycle;

namespace InternshipManagement.Web.Services
{
    public interface ILifecycleApiClient
    {
        Task<List<PlacementResponse>> GetStudentPlacementsAsync(string token);
        Task<List<PlacementResponse>> GetCompanyPlacementsAsync(string token);
        Task<PlacementResponse?> GetPlacementAsync(string token, int id);
        Task<List<ApplicationStatusHistoryResponse>> GetApplicationStatusHistoryAsync(string token, int applicationId);
        Task<ProgressReportResponse?> SubmitProgressReportAsync(string token, int placementId, SubmitProgressReportRequest request);
        Task<List<ProgressReportResponse>> GetProgressReportsAsync(string token, int placementId);
        Task<bool> ReviewProgressReportAsync(string token, int reportId, ReviewProgressReportRequest request);
        Task<bool> ProposeExtensionAsync(string token, int placementId, ProposeExtensionRequest request);
        Task<bool> RespondToExtensionAsync(string token, int extensionRequestId, RespondToExtensionRequest request);
        Task<bool> RequestWithdrawalAsync(string token, int placementId, RequestWithdrawalRequest request);
        Task<bool> ReviewWithdrawalAsync(string token, int withdrawalRequestId, ReviewWithdrawalRequest request);
        Task<bool> CompletePlacementAsync(string token, int placementId, CompletePlacementRequest request);
        Task<bool> TerminatePlacementAsync(string token, int placementId, TerminatePlacementRequest request);
        Task<EvaluationResponse?> SubmitEvaluationAsync(string token, int placementId, SubmitEvaluationRequest request);
        Task<List<EvaluationResponse>> GetEvaluationsAsync(string token, int placementId);
    }
}
