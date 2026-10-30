using System.Net;
using System.Net.Http.Headers;
using System.Text;

namespace InternshipManagement.Web.Services
{
    public sealed class AiApiClient : IAiApiClient
    {
        private readonly HttpClient _http;
        private readonly ILogger<AiApiClient> _logger;

        public AiApiClient(IHttpClientFactory factory, ILogger<AiApiClient> logger)
        {
            _http = factory.CreateClient("InternshipApi");
            _logger = logger;
        }

        public async Task<ApiProxyResult> SendAsync(
            string token, HttpMethod method, string path, HttpContent? content = null, CancellationToken cancellationToken = default)
        {
            using var request = new HttpRequestMessage(method, path) { Content = content };
            if (!string.IsNullOrEmpty(token))   // anonymous calls (forgot password, email links) pass an empty token
                request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            try
            {
                using var response = await _http.SendAsync(request, cancellationToken);
                var body = await response.Content.ReadAsStringAsync(cancellationToken);
                var retryAfter = response.Headers.RetryAfter?.Delta?.TotalSeconds.ToString("0");
                return new ApiProxyResult((int)response.StatusCode, body, retryAfter);
            }
            catch (HttpRequestException ex)
            {
                _logger.LogWarning("API request to {Path} failed: {Type}", path, ex.GetType().Name);
                return Unavailable();
            }
            catch (TaskCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                return new ApiProxyResult((int)HttpStatusCode.GatewayTimeout,
                    "{\"error\":\"The server took too long to answer. Please try again.\"}");
            }
        }

        public async Task<HttpResponseMessage> OpenStreamAsync(
            string token, string path, string jsonBody, CancellationToken cancellationToken = default)
        {
            var request = new HttpRequestMessage(HttpMethod.Post, path)
            {
                Content = new StringContent(jsonBody, Encoding.UTF8, "application/json")
            };
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/event-stream"));

            try
            {
                // ResponseHeadersRead: hand the response over as soon as the headers arrive, so text can be forwarded as it is written.
                return await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead, cancellationToken);
            }
            finally
            {
                request.Dispose();
            }
        }

        private static ApiProxyResult Unavailable() =>
            new((int)HttpStatusCode.ServiceUnavailable, "{\"error\":\"The service is not available right now. Please try again shortly.\"}");
    }
}
