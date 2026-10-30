using System.Collections.Concurrent;
using System.Net;
using System.Text.Json;

namespace InternshipManagement.Web.Services
{
    public enum RefreshOutcome
    {
        /// <summary>Nobody is signed in.</summary>
        NoSession,
        /// <summary>The access token still has time left, nothing was done.</summary>
        StillValid,
        /// <summary>A new access token and refresh token are now in the session.</summary>
        Refreshed,
        /// <summary>The API refused the refresh token (idle too long, signed out, account disabled): the session was ended.</summary>
        Expired,
        /// <summary>The API could not be reached. The session was left as it is.</summary>
        Unavailable
    }

    /// <summary>
    /// Keeps a signed-in user signed in while they are active.
    /// The API's access token lasts only as long as the idle limit (30 minutes for companies and admins,
    /// 2 hours for students). Before it runs out this class swaps the refresh token for a new pair and updates the session.
    /// The API replaces the refresh token on every use, so two requests that arrive together must not both spend the
    /// same one: the first call does the work and the others receive the same result.
    /// </summary>
    public sealed class TokenRefreshCoordinator
    {
        private static readonly TimeSpan RenewWhenWithin = TimeSpan.FromSeconds(60);
        private static readonly TimeSpan RememberResultFor = TimeSpan.FromMinutes(2);

        private readonly IHttpClientFactory _factory;
        private readonly ILogger<TokenRefreshCoordinator> _logger;
        private readonly ConcurrentDictionary<string, Lazy<Task<RefreshResult>>> _calls = new();

        public TokenRefreshCoordinator(IHttpClientFactory factory, ILogger<TokenRefreshCoordinator> logger)
        {
            _factory = factory;
            _logger = logger;
        }

        public async Task<RefreshOutcome> EnsureFreshAsync(ISession session, bool force, CancellationToken cancellationToken)
        {
            var jwt = session.GetString("JwtToken");
            if (string.IsNullOrEmpty(jwt))
                return RefreshOutcome.NoSession;

            if (!force && !ExpiresWithin(jwt, RenewWhenWithin))
                return RefreshOutcome.StillValid;

            var refreshToken = session.GetString("RefreshToken");
            if (string.IsNullOrEmpty(refreshToken))
            {
                // No refresh token (the short two-factor setup token): nothing to renew, the user signs in again once it has run out.
                if (force)
                    return RefreshOutcome.Unavailable;
                if (!ExpiresWithin(jwt, TimeSpan.Zero))
                    return RefreshOutcome.StillValid;

                session.Clear();
                return RefreshOutcome.Expired;
            }

            var result = await RefreshAsync(refreshToken).WaitAsync(cancellationToken);
            switch (result.Status)
            {
                case RefreshStatus.Ok:
                    session.SetString("JwtToken", result.Token!);
                    session.SetString("RefreshToken", result.RefreshToken!);
                    if (result.EmailConfirmed.HasValue)
                        session.SetString("EmailConfirmed", result.EmailConfirmed.Value.ToString());
                    if (result.ProfilePictureUrl != null)
                        session.SetString("ProfilePictureUrl", result.ProfilePictureUrl);
                    return RefreshOutcome.Refreshed;

                case RefreshStatus.Rejected:
                    session.Clear();
                    return RefreshOutcome.Expired;

                default:
                    return RefreshOutcome.Unavailable;
            }
        }

        private Task<RefreshResult> RefreshAsync(string refreshToken)
        {
            Purge();
            var call = _calls.GetOrAdd(refreshToken, key => new Lazy<Task<RefreshResult>>(() => CallApiAsync(key)));
            return call.Value;
        }

        private async Task<RefreshResult> CallApiAsync(string refreshToken)
        {
            try
            {
                using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(15));
                var client = _factory.CreateClient("InternshipApi");
                using var response = await client.PostAsJsonAsync("api/Auth/refresh-token", new { refreshToken }, timeout.Token);

                if (response.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.BadRequest)
                    return RefreshResult.Rejected();

                if (!response.IsSuccessStatusCode)
                    return Unavailable(refreshToken);

                using var doc = await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync(timeout.Token), cancellationToken: timeout.Token);
                var root = doc.RootElement;
                var token = Text(root, "token");
                var next = Text(root, "refreshToken");
                if (string.IsNullOrEmpty(token) || string.IsNullOrEmpty(next))
                    return Unavailable(refreshToken);

                bool? confirmed = null;
                foreach (var property in root.EnumerateObject())
                {
                    if (property.Name.Equals("emailConfirmed", StringComparison.OrdinalIgnoreCase)
                        && property.Value.ValueKind is JsonValueKind.True or JsonValueKind.False)
                        confirmed = property.Value.GetBoolean();
                }

                return RefreshResult.Ok(token, next, confirmed, Text(root, "profilePictureUrl"));
            }
            catch (Exception ex)
            {
                _logger.LogWarning("Token refresh failed: {Type}", ex.GetType().Name);
                return Unavailable(refreshToken);
            }
        }

        // A failed attempt is not remembered, so the next request can try again.
        private RefreshResult Unavailable(string refreshToken)
        {
            _calls.TryRemove(refreshToken, out _);
            return RefreshResult.Unavailable();
        }

        private void Purge()
        {
            var cutoff = DateTime.UtcNow - RememberResultFor;
            foreach (var pair in _calls)
            {
                if (!pair.Value.IsValueCreated || !pair.Value.Value.IsCompleted)
                    continue;

                if (!pair.Value.Value.IsCompletedSuccessfully || pair.Value.Value.Result.At < cutoff)
                    _calls.TryRemove(pair.Key, out _);
            }
        }

        private static string? Text(JsonElement root, string name)
        {
            foreach (var property in root.EnumerateObject())
                if (property.Name.Equals(name, StringComparison.OrdinalIgnoreCase) && property.Value.ValueKind == JsonValueKind.String)
                    return property.Value.GetString();
            return null;
        }

        // The access token is a JWT: read its "exp" claim (no signature check is needed to know when it runs out).
        private static bool ExpiresWithin(string jwt, TimeSpan window)
        {
            var parts = jwt.Split('.');
            if (parts.Length != 3)
                return false;

            try
            {
                var payload = parts[1].Replace('-', '+').Replace('_', '/');
                payload = payload.PadRight(payload.Length + (4 - payload.Length % 4) % 4, '=');
                using var doc = JsonDocument.Parse(Convert.FromBase64String(payload));
                if (!doc.RootElement.TryGetProperty("exp", out var exp))
                    return false;
                return DateTimeOffset.FromUnixTimeSeconds(exp.GetInt64()) - DateTimeOffset.UtcNow <= window;
            }
            catch (Exception ex) when (ex is FormatException or JsonException or InvalidOperationException)
            {
                return false;
            }
        }

        private enum RefreshStatus { Ok, Rejected, Unavailable }

        private sealed record RefreshResult(
            RefreshStatus Status, string? Token, string? RefreshToken, bool? EmailConfirmed, string? ProfilePictureUrl, DateTime At)
        {
            public static RefreshResult Ok(string token, string refreshToken, bool? emailConfirmed, string? picture) =>
                new(RefreshStatus.Ok, token, refreshToken, emailConfirmed, picture, DateTime.UtcNow);
            public static RefreshResult Rejected() => new(RefreshStatus.Rejected, null, null, null, null, DateTime.UtcNow);
            public static RefreshResult Unavailable() => new(RefreshStatus.Unavailable, null, null, null, null, DateTime.UtcNow);
        }
    }
}
