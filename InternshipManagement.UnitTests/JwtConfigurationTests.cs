using InternshipManagement.Application.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace InternshipManagement.UnitTests;

public sealed class JwtConfigurationTests
{
    [Fact]
    public void From_RequiresSigningKey()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["JwtSettings:Issuer"] = "Interlane.Api",
            ["JwtSettings:Audience"] = "Interlane.Web"
        });

        var error = Assert.Throws<InvalidOperationException>(() => JwtConfiguration.From(configuration));

        Assert.Contains("JwtSettings:Key", error.Message);
    }

    [Fact]
    public void From_RejectsSigningKeyShorterThan256Bits()
    {
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["JwtSettings:Key"] = "too-short",
            ["JwtSettings:Issuer"] = "Interlane.Api",
            ["JwtSettings:Audience"] = "Interlane.Web"
        });

        Assert.Throws<InvalidOperationException>(() => JwtConfiguration.From(configuration));
    }

    [Fact]
    public void From_UsesConfiguredSecretAndIssuerAndAudience()
    {
        var key = new string('k', 48);
        var configuration = BuildConfiguration(new Dictionary<string, string?>
        {
            ["JwtSettings:Key"] = key,
            ["JwtSettings:Issuer"] = "Interlane.Api",
            ["JwtSettings:Audience"] = "Interlane.Web"
        });

        var result = JwtConfiguration.From(configuration);

        Assert.Equal(System.Text.Encoding.UTF8.GetBytes(key), result.SigningKey);
        Assert.Equal("Interlane.Api", result.Issuer);
        Assert.Equal("Interlane.Web", result.Audience);
    }

    private static IConfiguration BuildConfiguration(IDictionary<string, string?> values) =>
        new ConfigurationBuilder().AddInMemoryCollection(values).Build();
}
