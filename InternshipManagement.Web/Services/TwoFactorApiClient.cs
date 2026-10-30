using System.Net.Http.Headers;
using System.Net.Http.Json;
using InternshipManagement.Web.Models.Auth;

namespace InternshipManagement.Web.Services
{
    public sealed class TwoFactorApiClient : ITwoFactorApiClient
    {
        private readonly HttpClient _httpClient;

        public TwoFactorApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("InternshipApi");
        }

        public async Task<TwoFactorSetupViewModel?> GetSetupAsync(string token)
        {
            SetAuthorization(token);
            return await _httpClient.GetFromJsonAsync<TwoFactorSetupViewModel>("api/Auth/two-factor/setup");
        }

        public async Task<AuthApiResult> EnableAsync(string token, string code)
        {
            SetAuthorization(token);
            var response = await _httpClient.PostAsJsonAsync("api/Auth/two-factor/enable", new { Code = code });
            var body = await response.Content.ReadFromJsonAsync<AuthApiResult>();
            if (body != null)
                body.Success = response.IsSuccessStatusCode && body.Success;
            return body ?? new AuthApiResult { Success = false, Errors = new List<string> { "The API returned an empty response." } };
        }

        private void SetAuthorization(string token) =>
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
    }
}
