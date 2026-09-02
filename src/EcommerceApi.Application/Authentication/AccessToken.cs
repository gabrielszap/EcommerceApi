namespace EcommerceApi.Application.Authentication;

public sealed record AccessToken(string Value, DateTime ExpiresAtUtc);
