using InternshipManagement.Application.DTOs.Lifecycle;

namespace InternshipManagement.Application.Interfaces
{
    public interface IInternshipLifecycleService
    {
        Task<PlacementResponse?> CreatePlacementFromAcceptedApplicationAsync(int applicationId);
        Task<List<PlacementResponse>> GetStudentPlacementsAsync(int userId);
        Task<List<PlacementResponse>> GetCompanyPlacementsAsync(int userId);
        Task<PlacementResponse?> GetPlacementAsync(int placementId);
        Task<List<ApplicationStatusHistoryResponse>> GetApplicationStatusHistoryAsync(int applicationId);
        Task<ProgressReportResponse> SubmitProgressReportAsync(int userId, int placementId, SubmitProgressReportRequest request);
        Task<List<ProgressReportResponse>> GetProgressReportsAsync(int placementId);
        Task<bool> ReviewProgressReportAsync(int userId, int reportId, ReviewProgressReportRequest request);
        Task<ExtensionRequestResponse> ProposeExtensionAsync(int userId, int placementId, ProposeExtensionRequest request);
        Task<bool> RespondToExtensionAsync(int userId, int extensionRequestId, RespondToExtensionRequest request);
        Task<WithdrawalRequestResponse> RequestWithdrawalAsync(int userId, int placementId, RequestWithdrawalRequest request);
        Task<bool> ReviewWithdrawalAsync(int userId, int withdrawalRequestId, ReviewWithdrawalRequest request);
        Task<bool> CompletePlacementAsync(int userId, int placementId, CompletePlacementRequest request);
        Task<bool> TerminatePlacementAsync(int userId, int placementId, TerminatePlacementRequest request);
        Task<EvaluationResponse> SubmitEvaluationAsync(int userId, int placementId, SubmitEvaluationRequest request);
        Task<List<EvaluationResponse>> GetEvaluationsAsync(int placementId);
    }
}
