namespace EcommerceApi.Application.Authentication.Login;

public sealed record LoginResult(bool IsAuthenticated, AccessToken? AccessToken)
{
    public static LoginResult Authenticated(AccessToken accessToken) => new(true, accessToken);

    public static LoginResult InvalidCredentials() => new(false, null);
}
