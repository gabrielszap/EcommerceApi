namespace EcommerceApi.Api.Authentication;

public sealed record LoginResponse(string AccessToken, DateTime ExpiresAtUtc);
