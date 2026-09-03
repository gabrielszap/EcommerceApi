using System.IdentityModel.Tokens.Jwt;
using System.Text;
using EcommerceApi.Application.Authentication;
using EcommerceApi.Infrastructure;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.IdentityModel.Tokens;

namespace EcommerceApi.Tests.Infrastructure;

public sealed class JwtAccessTokenGeneratorTests
{
    [Fact]
    public void Generate_CreatesTokenAcceptedForConfiguredIssuerAudienceSignatureAndLifetime()
    {
        const string signingKey = "test-signing-key-with-at-least-32-bytes";
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["ConnectionStrings:Orders"] = "Data Source=:memory:",
            ["Jwt:Issuer"] = "EcommerceApi.Tests",
            ["Jwt:Audience"] = "EcommerceApi.Tests.Client",
            ["Jwt:LifetimeMinutes"] = "60",
            ["Jwt:SigningKey"] = signingKey
        }).Build();
        var services = new ServiceCollection();
        services.AddJwtAccessTokenGeneration(configuration);
        using var provider = services.BuildServiceProvider();
        var generator = provider.GetRequiredService<IAccessTokenGenerator>();

        var accessToken = generator.Generate("dev@martech.com");
        var principal = new JwtSecurityTokenHandler().ValidateToken(accessToken.Value, new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "EcommerceApi.Tests",
            ValidateAudience = true,
            ValidAudience = "EcommerceApi.Tests.Client",
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
            ValidateLifetime = true,
            ClockSkew = TimeSpan.Zero
        }, out _);

        var rawToken = new JwtSecurityTokenHandler().ReadJwtToken(accessToken.Value);
        Assert.Equal("dev@martech.com", rawToken.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Sub).Value);
        Assert.Equal("dev@martech.com", rawToken.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Email).Value);
        Assert.False(string.IsNullOrWhiteSpace(rawToken.Claims.Single(claim => claim.Type == JwtRegisteredClaimNames.Jti).Value));
        Assert.True(accessToken.ExpiresAtUtc > DateTime.UtcNow);
    }
}
