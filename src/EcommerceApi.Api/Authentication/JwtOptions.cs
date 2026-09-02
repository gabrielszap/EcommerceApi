using System.Text;
using Microsoft.Extensions.Configuration;

namespace EcommerceApi.Api.Authentication;

public sealed record JwtOptions(
    string Issuer,
    string Audience,
    int LifetimeMinutes,
    string SigningKey)
{
    public static JwtOptions FromConfiguration(IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var lifetimeMinutes = configuration.GetValue<int?>("Jwt:LifetimeMinutes");
        var signingKey = configuration["Jwt:SigningKey"];

        if (string.IsNullOrWhiteSpace(issuer))
        {
            throw new InvalidOperationException("Jwt:Issuer must be configured.");
        }

        if (string.IsNullOrWhiteSpace(audience))
        {
            throw new InvalidOperationException("Jwt:Audience must be configured.");
        }

        if (lifetimeMinutes is not > 0)
        {
            throw new InvalidOperationException("Jwt:LifetimeMinutes must be greater than zero.");
        }

        if (string.IsNullOrWhiteSpace(signingKey) || Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException("JWT signing key must contain at least 32 bytes (Jwt:SigningKey).");
        }

        return new JwtOptions(issuer, audience, lifetimeMinutes.Value, signingKey);
    }
}
