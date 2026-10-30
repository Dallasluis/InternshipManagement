using System.Net.Http.Headers;
using System.Net.Http.Json;
using InternshipManagement.Web.Models.Admin;

namespace InternshipManagement.Web.Services
{
    public class AdminApiClient : IAdminApiClient
    {
        private readonly HttpClient _httpClient;

        public AdminApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("InternshipApi");
        }

        private void AddAuthorization(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<AdminStatsResponse?> GetStatsAsync(string token)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync("api/Admin/stats");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<AdminStatsResponse>();
        }

        public async Task<List<CompanyListResponse>> GetAllCompaniesAsync(string token)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync("api/Admin/companies");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<CompanyListResponse>>() ?? new List<CompanyListResponse>();
        }

        public async Task<List<CompanySummaryResponse>> GetRecentCompaniesAsync(string token, int count)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync($"api/Admin/companies/recent?count={count}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<CompanySummaryResponse>>() ?? new List<CompanySummaryResponse>();
        }

        public async Task<bool> ReviewVerificationAsync(
            string token,
            int companyId,
            string status,
            string reasonCode,
            string notes,
            Dictionary<string, bool> criteriaAssessment,
            bool isAppeal = false)
        {
            AddAuthorization(token);
            var request = new { Approved = status == "Verified", Status = status, ReasonCode = reasonCode, Notes = notes, CriteriaAssessment = criteriaAssessment };
            var route = isAppeal
                ? $"api/Admin/companies/{companyId}/verification/appeal"
                : $"api/Admin/companies/{companyId}/verification";
            var response = await _httpClient.PutAsJsonAsync(route, request);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<CompanyVerificationHistoryResponse>> GetVerificationHistoryAsync(string token, int companyId)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync($"api/Admin/companies/{companyId}/verification/history");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<CompanyVerificationHistoryResponse>>() ?? new();
        }

        public async Task<List<InternshipManagement.Web.Models.Documents.PrivateDocumentResponse>> GetVerificationEvidenceAsync(string token, int companyId)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync($"api/Admin/companies/{companyId}/verification/evidence");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<InternshipManagement.Web.Models.Documents.PrivateDocumentResponse>>() ?? new();
        }

        public async Task<List<InternshipListResponse>> GetAllInternshipsAsync(string token)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync("api/Admin/internships");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<InternshipListResponse>>() ?? new List<InternshipListResponse>();
        }

        public async Task<bool> ModerateInternshipAsync(string token, int id, string status, string? notes)
        {
            AddAuthorization(token);
            var request = new { Status = status, Notes = notes ?? string.Empty };
            var response = await _httpClient.PostAsJsonAsync($"api/Admin/internships/{id}/moderate", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<ReportListResponse>> GetAllReportsAsync(string token)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync("api/Admin/reports");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<ReportListResponse>>() ?? new List<ReportListResponse>();
        }

        public async Task<List<ReportSummaryResponse>> GetRecentReportsAsync(string token, int count)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync($"api/Admin/reports/recent?count={count}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<ReportSummaryResponse>>() ?? new List<ReportSummaryResponse>();
        }

        public async Task<bool> ResolveReportAsync(
            string token,
            int id,
            string response,
            bool resolved,
            string? status = null,
            bool fraudUpheld = false)
        {
            AddAuthorization(token);
            var request = new { Response = response, Resolved = resolved, Status = status, FraudUpheld = fraudUpheld };
            var responseMessage = await _httpClient.PutAsJsonAsync($"api/Admin/reports/{id}/resolve", request);
            return responseMessage.IsSuccessStatusCode;
        }

        public async Task<List<UserListResponse>> GetAllUsersAsync(string token)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync("api/Admin/users");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<UserListResponse>>() ?? new List<UserListResponse>();
        }

        public async Task<bool> SuspendUserAsync(string token, int id, bool suspend)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsync($"api/Admin/users/{id}/suspend?suspend={suspend}", null);
            return response.IsSuccessStatusCode;
        }
    }
}