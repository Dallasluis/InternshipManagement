using System.Net.Http.Json;
using InternshipManagement.Web.Models.Auth;

namespace InternshipManagement.Web.Services
{
    public class AuthApiClient : IAuthApiClient
    {
        private readonly HttpClient _httpClient;

        public AuthApiClient(IHttpClientFactory httpClientFactory)
        {
            _httpClient = httpClientFactory.CreateClient("InternshipApi");
        }

        public async Task<AuthApiResult> RegisterAsync(RegisterViewModel model)
        {
            var payload = new
            {
                email = model.Email,
                password = model.Password,
                confirmPassword = model.ConfirmPassword,
                firstName = model.FirstName,
                lastName = model.LastName,
                userType = model.UserType,
                companyName = model.CompanyName ?? string.Empty,
                industry = model.Industry ?? string.Empty
            };

            var response = await _httpClient.PostAsJsonAsync("api/Auth/register", payload);
            return await ReadResultAsync(response);
        }

        public async Task<AuthApiResult> LoginAsync(LoginViewModel model)
        {
            var payload = new
            {
                email = model.Email,
                password = model.Password
            };

            var response = await _httpClient.PostAsJsonAsync("api/Auth/login", payload);
            return await ReadResultAsync(response);
        }

        private static async Task<AuthApiResult> ReadResultAsync(HttpResponseMessage response)
        {
            var result = await response.Content.ReadFromJsonAsync<AuthApiResult>();

            if (result != null)
                return result;

            return new AuthApiResult
            {
                Success = false,
                Errors = new List<string> { "Unexpected response from the server. Please try again." }
            };
        }
    }
}