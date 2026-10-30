using System.Net;

namespace InternshipManagement.Web.Services
{
    /// <summary>The API's answer, passed through unchanged so the browser can read it.</summary>
    public sealed record ApiProxyResult(int StatusCode, string Body, string? RetryAfter = null);

    /// <summary>
    /// Talks to the AI endpoints of the API on behalf of the logged-in user. The JWT lives in the server
    /// session, so the browser never calls the API directly: it calls the Web app, which calls this client.
    /// </summary>
    public interface IAiApiClient
    {
        Task<ApiProxyResult> SendAsync(string token, HttpMethod method, string path, HttpContent? content = null, CancellationToken cancellationToken = default);

        /// <summary>Opens a streaming (Server-Sent Events) response. The caller disposes the response.</summary>
        Task<HttpResponseMessage> OpenStreamAsync(string token, string path, string jsonBody, CancellationToken cancellationToken = default);
    }
}
