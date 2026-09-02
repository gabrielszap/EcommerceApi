using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using EcommerceApi.Application.Authentication;
using Microsoft.IdentityModel.Tokens;

namespace EcommerceApi.Infrastructure.Authentication;

internal sealed class JwtAccessTokenGenerator(JwtTokenOptions options) : IAccessTokenGenerator
{
    public AccessToken Generate(string email)
    {
        var expiresAtUtc = DateTime.UtcNow.AddMinutes(options.LifetimeMinutes);
        var token = new JwtSecurityToken(
            issuer: options.Issuer,
            audience: options.Audience,
            claims:
            [
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString("N"))
            ],
            notBefore: DateTime.UtcNow,
            expires: expiresAtUtc,
            signingCredentials: new SigningCredentials(
                new SymmetricSecurityKey(Encoding.UTF8.GetBytes(options.SigningKey)),
                SecurityAlgorithms.HmacSha256));

        return new AccessToken(new JwtSecurityTokenHandler().WriteToken(token), expiresAtUtc);
    }
}
