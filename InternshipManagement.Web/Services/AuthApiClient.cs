using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http.Headers;
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
            try
            {
                var payload = new
                {
                    email = model.Email,
                    password = model.Password
                };

                var response = await _httpClient.PostAsJsonAsync("api/Auth/login", payload);
                return await ReadResultAsync(response);
            }
            catch (HttpRequestException)
            {
                return new AuthApiResult
                {
                    Success = false,
                    Errors = new List<string> { "The API is not available. Start InternshipManagement.Api on http://localhost:5003 and try again." }
                };
            }
        }

        public async Task<AuthApiResult> ChangePasswordAsync(string token, ChangePasswordViewModel model)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsJsonAsync("api/Auth/change-password", new { model.CurrentPassword, model.NewPassword });
            return await ReadResultAsync(response);
        }

        public async Task<AuthApiResult> ChangeEmailAsync(string token, ChangeEmailViewModel model)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsJsonAsync("api/Auth/change-email", model);
            return await ReadResultAsync(response);
        }

        public async Task<AccountPreferencesViewModel?> GetPreferencesAsync(string token)
        {
            AddAuthorization(token);
            var response = await _httpClient.GetAsync("api/Auth/preferences");
            return response.IsSuccessStatusCode
                ? await response.Content.ReadFromJsonAsync<AccountPreferencesViewModel>()
                : null;
        }

        public async Task<AuthApiResult> UpdatePreferencesAsync(string token, AccountPreferencesViewModel model)
        {
            AddAuthorization(token);
            var response = await _httpClient.PutAsJsonAsync("api/Auth/preferences", model);
            return await ReadResultAsync(response);
        }

        public async Task<AuthApiResult> DeactivateAsync(string token)
        {
            AddAuthorization(token);
            var response = await _httpClient.PostAsync("api/Auth/deactivate", null);
            return await ReadResultAsync(response);
        }

        private void AddAuthorization(string token)
        {
            _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        private static async Task<AuthApiResult> ReadResultAsync(HttpResponseMessage response)
        {
            await using var stream = await response.Content.ReadAsStreamAsync();
            JsonDocument document;

            try
            {
                document = await JsonDocument.ParseAsync(stream);
            }
            catch (JsonException)
            {
                return new AuthApiResult
                {
                    Success = false,
                    Errors = new List<string> { "The API returned an invalid response. Make sure the API is running and try again." }
                };
            }

            using (document)
            {
            var root = document.RootElement;

            var result = new AuthApiResult
            {
                Success = GetBoolean(root, "success") ?? response.IsSuccessStatusCode,
                Token = GetString(root, "token"),
                RefreshToken = GetString(root, "refreshToken"),
                UserId = GetString(root, "userId"),
                UserType = GetString(root, "userType"),
                Email = GetString(root, "email"),
                FirstName = GetString(root, "firstName"),
                LastName = GetString(root, "lastName"),
                Message = GetString(root, "message"),
                Errors = ReadErrors(root)
            };

            if (!response.IsSuccessStatusCode && result.Errors?.Any() != true)
                result.Errors = new List<string> { result.Message ?? "The request failed. Please check your details and try again." };

            return result;
            }
        }

        private static string? GetString(JsonElement root, string propertyName)
        {
            return root.TryGetProperty(propertyName, out var value) && value.ValueKind != JsonValueKind.Null
                ? value.ToString()
                : null;
        }

        private static bool? GetBoolean(JsonElement root, string propertyName)
        {
            return root.TryGetProperty(propertyName, out var value) && value.ValueKind is JsonValueKind.True or JsonValueKind.False
                ? value.GetBoolean()
                : null;
        }

        private static List<string>? ReadErrors(JsonElement root)
        {
            if (!root.TryGetProperty("errors", out var errors) || errors.ValueKind == JsonValueKind.Null)
                return null;

            if (errors.ValueKind == JsonValueKind.Array)
                return errors.EnumerateArray().Select(error => error.ToString()).Where(error => !string.IsNullOrWhiteSpace(error)).ToList();

            if (errors.ValueKind == JsonValueKind.Object)
            {
                var messages = new List<string>();
                foreach (var property in errors.EnumerateObject())
                {
                    if (property.Value.ValueKind == JsonValueKind.Array)
                        messages.AddRange(property.Value.EnumerateArray().Select(error => error.ToString()));
                    else
                        messages.Add(property.Value.ToString());
                }

                return messages.Where(error => !string.IsNullOrWhiteSpace(error)).ToList();
            }

            return new List<string> { errors.ToString() };
        }
    }
}
