using System.Net.Http.Headers;
using System.Net.Http.Json;
using InternshipManagement.Web.Models.Internship;

namespace InternshipManagement.Web.Services
{
    public class ReportApiClient : IReportApiClient
    {
        private readonly HttpClient _httpClient;

        public ReportApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("InternshipApi");
        }

        public async Task SubmitInternshipReportAsync(string token, SubmitInternshipReportRequest request)
        {
            using var apiRequest = new HttpRequestMessage(HttpMethod.Post, "api/reports")
            {
                Content = JsonContent.Create(request)
            };
            apiRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            using var response = await _httpClient.SendAsync(apiRequest);
            if (response.IsSuccessStatusCode)
                return;

            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException(string.IsNullOrWhiteSpace(error)
                ? $"The API returned {(int)response.StatusCode} ({response.ReasonPhrase})."
                : error);
        }
    }
}
