using Microsoft.Extensions.Configuration;

namespace InternshipManagement.Application.Services;

/// <summary>Reads the single signing configuration shared by token issuance and token validation.</summary>
public sealed record JwtConfiguration(byte[] SigningKey, string Issuer, string Audience)
{
    public static JwtConfiguration From(IConfiguration configuration)
    {
        var section = configuration.GetSection("JwtSettings");
        var key = section["Key"];
        var issuer = section["Issuer"];
        var audience = section["Audience"];

        if (string.IsNullOrWhiteSpace(key))
            throw new InvalidOperationException("JwtSettings:Key must be configured through user secrets or a deployment secret provider.");

        var signingKey = System.Text.Encoding.UTF8.GetBytes(key);
        if (signingKey.Length < 32)
            throw new InvalidOperationException("JwtSettings:Key must contain at least 32 bytes.");

        if (string.IsNullOrWhiteSpace(issuer))
            throw new InvalidOperationException("JwtSettings:Issuer must be configured.");

        if (string.IsNullOrWhiteSpace(audience))
            throw new InvalidOperationException("JwtSettings:Audience must be configured.");

        return new JwtConfiguration(signingKey, issuer, audience);
    }
}
