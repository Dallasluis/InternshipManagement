using System.Net.Http.Headers;
using System.Net.Http.Json;
using InternshipManagement.Web.Models.Company;

namespace InternshipManagement.Web.Services
{
    public class CompanyApiClient : ICompanyApiClient
    {
        private readonly HttpClient _httpClient;

        public CompanyApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("InternshipApi");
        }

        private void AddAuthorization(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<CompanyProfileResponse?> GetProfileAsync(string token, int userId)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync($"api/Companies/profile?userId={userId}");

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<CompanyProfileResponse>();

            return null;
        }

        public async Task<CompanyProfileResponse?> UpdateProfileAsync(string token, int userId, UpdateCompanyProfileRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PutAsJsonAsync($"api/Companies/profile?userId={userId}", request);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<CompanyProfileResponse>();

            return null;
        }

        public async Task<bool> SubmitVerificationAsync(string token, int userId, SubmitVerificationRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsJsonAsync($"api/Companies/verification?userId={userId}", request);
            return response.IsSuccessStatusCode;
        }

        public async Task<CompanyStatsResponse?> GetCompanyStatsAsync(string token, int userId)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync($"api/Applications/company/stats?userId={userId}");
            
            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<CompanyStatsResponse>();
            
            return new CompanyStatsResponse { 
                TotalInternships = 0, 
                ActiveInternships = 0, 
                TotalApplications = 0, 
                ShortlistedCount = 0 
            };
        }
    }
}