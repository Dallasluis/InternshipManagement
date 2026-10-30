using System;

namespace InternshipManagement.Application.Interfaces
{
    /// <summary>What a valid run token says about its caller.</summary>
    public sealed record RunTokenClaims(int UserId, string Role, string Purpose);

    /// <summary>
    /// Mints and checks short-lived "run tokens". A run token is NOT a login token: the API hands one to
    /// n8n for a single AI job, and the AI's tools send it back so the API knows which user the job is
    /// for. It is bound to one user, one role and one purpose, expires in minutes, and only opens the
    /// read-only /api/agent-tools endpoints.
    /// </summary>
    public interface IRunTokenService
    {
        string Create(int userId, string role, string purpose, TimeSpan? lifetime = null);

        /// <summary>Null when the token is malformed, tampered with, expired or for another purpose.</summary>
        RunTokenClaims? Validate(string token, string expectedPurpose);
    }
}
