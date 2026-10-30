using InternshipManagement.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using System;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace InternshipManagement.Application.Services
{
    /// <summary>
    /// Token format: base64url(payloadJson) + "." + base64url(HMACSHA256(payload)).
    /// The signing key is derived from JwtSettings:Key with a fixed label, so it is never the same key
    /// that signs login JWTs: a login token is not a run token and a run token is not a login token.
    /// </summary>
    public sealed class RunTokenService : IRunTokenService
    {
        private static readonly TimeSpan DefaultLifetime = TimeSpan.FromMinutes(5);
        private readonly byte[] _key;

        public RunTokenService(IConfiguration configuration)
        {
            var secret = configuration["JwtSettings:Key"];
            if (string.IsNullOrWhiteSpace(secret) || secret.Length < 32)
                throw new InvalidOperationException("JwtSettings:Key must be set (32+ characters) to issue run tokens.");

            using var hmac = new HMACSHA256(Encoding.UTF8.GetBytes(secret));
            _key = hmac.ComputeHash(Encoding.UTF8.GetBytes("n8n-run-token-v1"));
        }

        public string Create(int userId, string role, string purpose, TimeSpan? lifetime = null)
        {
            var payload = JsonSerializer.SerializeToUtf8Bytes(new Payload
            {
                U = userId,
                R = role,
                P = purpose,
                E = DateTimeOffset.UtcNow.Add(lifetime ?? DefaultLifetime).ToUnixTimeSeconds()
            });

            var body = B64(payload);
            return body + "." + B64(Sign(body));
        }

        public RunTokenClaims? Validate(string token, string expectedPurpose)
        {
            if (string.IsNullOrWhiteSpace(token) || token.Length > 2000) return null;

            var parts = token.Split('.');
            if (parts.Length != 2) return null;

            try
            {
                var expected = Sign(parts[0]);
                var given = FromB64(parts[1]);
                if (!CryptographicOperations.FixedTimeEquals(expected, given)) return null;

                var p = JsonSerializer.Deserialize<Payload>(FromB64(parts[0]));
                if (p == null || p.U <= 0 || string.IsNullOrEmpty(p.R)) return null;
                if (p.E < DateTimeOffset.UtcNow.ToUnixTimeSeconds()) return null;
                if (!string.Equals(p.P, expectedPurpose, StringComparison.Ordinal)) return null;

                return new RunTokenClaims(p.U, p.R, p.P);
            }
            catch (Exception)   // bad base64 or JSON: just an invalid token
            {
                return null;
            }
        }

        private byte[] Sign(string body)
        {
            using var hmac = new HMACSHA256(_key);
            return hmac.ComputeHash(Encoding.UTF8.GetBytes(body));
        }

        private static string B64(byte[] bytes) =>
            Convert.ToBase64String(bytes).TrimEnd('=').Replace('+', '-').Replace('/', '_');

        private static byte[] FromB64(string s)
        {
            s = s.Replace('-', '+').Replace('_', '/');
            s = s.PadRight(s.Length + (4 - s.Length % 4) % 4, '=');
            return Convert.FromBase64String(s);
        }

        private sealed class Payload
        {
            public int U { get; set; }
            public string R { get; set; } = "";
            public string P { get; set; } = "";
            public long E { get; set; }
        }
    }
}
