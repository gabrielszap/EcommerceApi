using System.Text;
using Microsoft.Extensions.Configuration;

namespace EcommerceApi.Infrastructure.Authentication;

internal sealed record JwtTokenOptions(string Issuer, string Audience, int LifetimeMinutes, string SigningKey)
{
    public static JwtTokenOptions FromConfiguration(IConfiguration configuration)
    {
        var issuer = configuration["Jwt:Issuer"];
        var audience = configuration["Jwt:Audience"];
        var lifetimeMinutesValue = configuration["Jwt:LifetimeMinutes"];
        var signingKey = configuration["Jwt:SigningKey"];

        if (string.IsNullOrWhiteSpace(issuer) || string.IsNullOrWhiteSpace(audience) ||
            !int.TryParse(lifetimeMinutesValue, out var lifetimeMinutes) || lifetimeMinutes <= 0 ||
            string.IsNullOrWhiteSpace(signingKey) ||
            Encoding.UTF8.GetByteCount(signingKey) < 32)
        {
            throw new InvalidOperationException("JWT configuration is invalid.");
        }

        return new JwtTokenOptions(issuer, audience, lifetimeMinutes, signingKey);
    }
}
