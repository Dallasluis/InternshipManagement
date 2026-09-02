using System.Net.Http.Headers;
using System.Net.Http.Json;
using InternshipManagement.Web.Models.Application;

namespace InternshipManagement.Web.Services
{
    public class ApplicationApiClient : IApplicationApiClient
    {
        private readonly HttpClient _httpClient;

        public ApplicationApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("InternshipApi");
        }

        private void AddAuthorization(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<ApplicationResponse?> ApplyAsync(string token, int studentId, ApplyRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsJsonAsync($"api/Applications?studentId={studentId}", request);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<ApplicationResponse>();

            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                ? $"The API returned {(int)response.StatusCode} ({response.ReasonPhrase})."
                : error);
        }

        public async Task<List<ApplicationResponse>> GetStudentApplicationsAsync(string token, int studentId)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync("api/Applications/student");
            if (!response.IsSuccessStatusCode)
                return new List<ApplicationResponse>();

            return await response.Content.ReadFromJsonAsync<List<ApplicationResponse>>() ?? new List<ApplicationResponse>();
        }

        public async Task<List<ApplicationResponse>> GetInternshipApplicationsAsync(string token, int internshipId)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync($"api/Applications/internship/{internshipId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<ApplicationResponse>>() ?? new List<ApplicationResponse>();
        }

        public async Task<ApplicationResponse?> GetApplicationByIdAsync(string token, int id)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync($"api/Applications/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ApplicationResponse>();
        }

        public async Task<bool> UpdateStatusAsync(string token, int id, UpdateApplicationStatusRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PutAsJsonAsync($"api/Applications/{id}/status", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ShortlistAsync(string token, int id, string? notes)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsJsonAsync($"api/Applications/{id}/shortlist", new { Notes = notes ?? string.Empty });
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> ScheduleInterviewAsync(string token, int id, ScheduleInterviewRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsJsonAsync($"api/Applications/{id}/schedule-interview", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> MarkInterviewCompletedAsync(string token, int id)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsync($"api/Applications/{id}/mark-interview-completed", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> MakeOfferAsync(string token, int id, MakeOfferRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsJsonAsync($"api/Applications/{id}/make-offer", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> RespondToOfferAsync(string token, int id, RespondToOfferRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsJsonAsync($"api/Applications/{id}/respond-to-offer", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> WithdrawAsync(string token, int id)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsync($"api/Applications/{id}/withdraw", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<List<ApplicationResponse>> GetShortlistedAsync(string token, int internshipId)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync($"api/Applications/internship/{internshipId}/shortlisted");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<ApplicationResponse>>() ?? new List<ApplicationResponse>();
        }

        public async Task<StudentStatsResponse?> GetStudentStatsAsync(string token, int studentId)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync("api/Applications/student/stats");
            if (!response.IsSuccessStatusCode)
                return new StudentStatsResponse();

            return await response.Content.ReadFromJsonAsync<StudentStatsResponse>();
        }

        public async Task<CompanyStatsResponse?> GetCompanyStatsAsync(string token, int userId)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync("api/Applications/company/stats");
            if (!response.IsSuccessStatusCode)
                return new CompanyStatsResponse();

            return await response.Content.ReadFromJsonAsync<CompanyStatsResponse>();
        }

        public async Task<List<ApplicationResponse>> GetCompanyApplicationsAsync(string token, int userId)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync("api/Applications/company");
            if (!response.IsSuccessStatusCode)
                return new List<ApplicationResponse>();

            return await response.Content.ReadFromJsonAsync<List<ApplicationResponse>>() ?? new List<ApplicationResponse>();
        }
    }
}
