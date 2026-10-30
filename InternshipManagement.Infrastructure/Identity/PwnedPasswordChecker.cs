using System.Security.Cryptography;
using System.Text;
using InternshipManagement.Application.Interfaces;

namespace InternshipManagement.Infrastructure.Identity
{
    public sealed class PwnedPasswordChecker : IPasswordBreachChecker
    {
        private readonly HttpClient _httpClient;

        public PwnedPasswordChecker(HttpClient httpClient)
        {
            _httpClient = httpClient;
        }

        public async Task<bool> IsCompromisedAsync(string password, CancellationToken cancellationToken = default)
        {
            var hash = Convert.ToHexString(SHA1.HashData(Encoding.UTF8.GetBytes(password)));
            var prefix = hash[..5];
            var suffix = hash[5..];
            using var request = new HttpRequestMessage(HttpMethod.Get, $"range/{prefix}");
            request.Headers.Add("Add-Padding", "true");
            using var response = await _httpClient.SendAsync(request, cancellationToken);
            response.EnsureSuccessStatusCode();

            var responseBody = await response.Content.ReadAsStringAsync(cancellationToken);
            return responseBody
                .Split('\n', StringSplitOptions.RemoveEmptyEntries)
                .Select(line => line.Split(':', 2)[0].Trim())
                .Any(candidate => string.Equals(candidate, suffix, StringComparison.OrdinalIgnoreCase));
        }
    }
}
