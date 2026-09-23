using System.Net.Http.Headers;
using System.Net.Http.Json;
using InternshipManagement.Web.Models.Lifecycle;

namespace InternshipManagement.Web.Services
{
    public class LifecycleApiClient : ILifecycleApiClient
    {
        private readonly HttpClient _httpClient;

        public LifecycleApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("InternshipApi");
        }

        private void AddAuthorization(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<List<PlacementResponse>> GetStudentPlacementsAsync(string token)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync("api/Lifecycle/placements/student");
            if (!response.IsSuccessStatusCode) return new List<PlacementResponse>();
            return await response.Content.ReadFromJsonAsync<List<PlacementResponse>>() ?? new List<PlacementResponse>();
        }

        public async Task<List<PlacementResponse>> GetCompanyPlacementsAsync(string token)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync("api/Lifecycle/placements/company");
            if (!response.IsSuccessStatusCode) return new List<PlacementResponse>();
            return await response.Content.ReadFromJsonAsync<List<PlacementResponse>>() ?? new List<PlacementResponse>();
        }

        public async Task<PlacementResponse?> GetPlacementAsync(string token, int id)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync($"api/Lifecycle/placements/{id}");
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<PlacementResponse>();
        }

        public async Task<List<ApplicationStatusHistoryResponse>> GetApplicationStatusHistoryAsync(string token, int applicationId)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync($"api/Applications/{applicationId}/status-history");
            if (!response.IsSuccessStatusCode) return new List<ApplicationStatusHistoryResponse>();
            return await response.Content.ReadFromJsonAsync<List<ApplicationStatusHistoryResponse>>() ?? new List<ApplicationStatusHistoryResponse>();
        }

        public async Task<ProgressReportResponse?> SubmitProgressReportAsync(string token, int placementId, SubmitProgressReportRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsJsonAsync($"api/Lifecycle/placements/{placementId}/progress-reports", request);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<ProgressReportResponse>();
        }

        public async Task<List<ProgressReportResponse>> GetProgressReportsAsync(string token, int placementId)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync($"api/Lifecycle/placements/{placementId}/progress-reports");
            if (!response.IsSuccessStatusCode) return new List<ProgressReportResponse>();
            return await response.Content.ReadFromJsonAsync<List<ProgressReportResponse>>() ?? new List<ProgressReportResponse>();
        }

        public async Task<bool> ReviewProgressReportAsync(string token, int reportId, ReviewProgressReportRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PutAsJsonAsync($"api/Lifecycle/progress-reports/{reportId}/review", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ProposeExtensionAsync(string token, int placementId, ProposeExtensionRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsJsonAsync($"api/Lifecycle/placements/{placementId}/extensions", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RespondToExtensionAsync(string token, int extensionRequestId, RespondToExtensionRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PutAsJsonAsync($"api/Lifecycle/extensions/{extensionRequestId}/respond", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RequestWithdrawalAsync(string token, int placementId, RequestWithdrawalRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsJsonAsync($"api/Lifecycle/placements/{placementId}/withdrawals", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ReviewWithdrawalAsync(string token, int withdrawalRequestId, ReviewWithdrawalRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PutAsJsonAsync($"api/Lifecycle/withdrawals/{withdrawalRequestId}/review", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> CompletePlacementAsync(string token, int placementId, CompletePlacementRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PutAsJsonAsync($"api/Lifecycle/placements/{placementId}/complete", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> TerminatePlacementAsync(string token, int placementId, TerminatePlacementRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PutAsJsonAsync($"api/Lifecycle/placements/{placementId}/terminate", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<EvaluationResponse?> SubmitEvaluationAsync(string token, int placementId, SubmitEvaluationRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsJsonAsync($"api/Lifecycle/placements/{placementId}/evaluations", request);
            if (!response.IsSuccessStatusCode) return null;
            return await response.Content.ReadFromJsonAsync<EvaluationResponse>();
        }

        public async Task<List<EvaluationResponse>> GetEvaluationsAsync(string token, int placementId)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync($"api/Lifecycle/placements/{placementId}/evaluations");
            if (!response.IsSuccessStatusCode) return new List<EvaluationResponse>();
            return await response.Content.ReadFromJsonAsync<List<EvaluationResponse>>() ?? new List<EvaluationResponse>();
        }
    }
}
