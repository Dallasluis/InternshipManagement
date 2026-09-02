using System.Net.Http.Headers;
using System.Net.Http.Json;
using InternshipManagement.Web.Models.Internship;

namespace InternshipManagement.Web.Services
{
    public class InternshipApiClient : IInternshipApiClient
    {
        private readonly HttpClient _httpClient;

        public InternshipApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("InternshipApi");
        }

        private void AddAuthorization(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        public async Task<PagedInternshipResult> SearchAsync(InternshipSearchViewModel filters)
        {
            var query = new List<string>
            {
                $"PageNumber={filters.PageNumber}",
                $"PageSize={filters.PageSize}"
            };

            if (!string.IsNullOrWhiteSpace(filters.Keyword))
                query.Add($"Keyword={Uri.EscapeDataString(filters.Keyword)}");
            if (!string.IsNullOrWhiteSpace(filters.Location))
                query.Add($"Location={Uri.EscapeDataString(filters.Location)}");
            if (!string.IsNullOrWhiteSpace(filters.Industry))
                query.Add($"Industry={Uri.EscapeDataString(filters.Industry)}");
            if (!string.IsNullOrWhiteSpace(filters.InternshipType))
                query.Add($"InternshipType={Uri.EscapeDataString(filters.InternshipType)}");
            if (filters.MinStipend.HasValue)
                query.Add($"MinStipend={filters.MinStipend.Value}");
            if (filters.IsRemote.HasValue)
                query.Add($"IsRemote={filters.IsRemote.Value}");

            var url = "api/Internships?" + string.Join("&", query);

            var result = await _httpClient.GetFromJsonAsync<PagedInternshipResult>(url);
            return result ?? new PagedInternshipResult { PageNumber = filters.PageNumber, PageSize = filters.PageSize };
        }

        public async Task<InternshipResponse?> GetByIdAsync(int id)
        {
            var response = await _httpClient.GetAsync($"api/Internships/{id}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<InternshipResponse>();
        }

        public async Task<List<InternshipResponse>> GetCompanyInternshipsAsync(string token, int userId)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync($"api/Internships/company?userId={userId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<List<InternshipResponse>>() ?? new List<InternshipResponse>();
        }

        public async Task<InternshipResponse?> CreateInternshipAsync(string token, int userId, CreateInternshipRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsJsonAsync($"api/Internships?userId={userId}", request);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<InternshipResponse>();

            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                ? $"The API returned {(int)response.StatusCode} ({response.ReasonPhrase})."
                : error);
        }

        public async Task<InternshipResponse?> UpdateInternshipAsync(string token, int id, UpdateInternshipRequest request)
        {
            AddAuthorization(token);
            var response = await _httpClient.PutAsJsonAsync($"api/Internships/{id}", request);

            if (response.IsSuccessStatusCode)
                return await response.Content.ReadFromJsonAsync<InternshipResponse>();

            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                ? $"The API returned {(int)response.StatusCode} ({response.ReasonPhrase})."
                : error);
        }

        public async Task<bool> PublishInternshipAsync(string token, int id)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsync($"api/Internships/{id}/publish", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> CloseInternshipAsync(string token, int id)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsync($"api/Internships/{id}/close", null);
            return response.IsSuccessStatusCode;
        }

        public async Task<bool> DeleteInternshipAsync(string token, int id)
        {
            AddAuthorization(token);
            var response = await _httpClient.DeleteAsync($"api/Internships/{id}");
            return response.IsSuccessStatusCode;
        }
    }
}