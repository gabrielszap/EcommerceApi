using EcommerceApi.Application.Authentication;
using EcommerceApi.Application.Authentication.Login;

namespace EcommerceApi.Tests.Application.Authentication;

public sealed class LoginCommandHandlerTests
{
    [Fact]
    public async Task Handle_WithFixedCredentials_ReturnsGeneratedAccessToken()
    {
        var expectedToken = new AccessToken("access-token", DateTime.UtcNow.AddHours(1));
        var handler = CreateHandler(expectedToken);

        var result = await handler.Handle(
            new LoginCommand("dev@martech.com", "Senha@123"),
            CancellationToken.None);

        Assert.True(result.IsAuthenticated);
        Assert.Same(expectedToken, result.AccessToken);
    }

    [Fact]
    public async Task Handle_WithInvalidEmail_ReturnsInvalidCredentialsWithoutToken()
    {
        var handler = CreateHandler(new AccessToken("should-not-be-issued", DateTime.UtcNow.AddHours(1)));

        var result = await handler.Handle(
            new LoginCommand("other@example.com", "Senha@123"),
            CancellationToken.None);

        Assert.False(result.IsAuthenticated);
        Assert.Null(result.AccessToken);
    }

    [Fact]
    public async Task Handle_WithInvalidPassword_ReturnsInvalidCredentialsWithoutToken()
    {
        var handler = CreateHandler(new AccessToken("should-not-be-issued", DateTime.UtcNow.AddHours(1)));

        var result = await handler.Handle(
            new LoginCommand("dev@martech.com", "wrong-password"),
            CancellationToken.None);

        Assert.False(result.IsAuthenticated);
        Assert.Null(result.AccessToken);
    }

    private static LoginCommandHandler CreateHandler(AccessToken token) =>
        new(new InMemoryTestCredentialsValidator(), new StubAccessTokenGenerator(token));

    private sealed class StubAccessTokenGenerator(AccessToken token) : IAccessTokenGenerator
    {
        public AccessToken Generate(string email) => token;
    }
}
