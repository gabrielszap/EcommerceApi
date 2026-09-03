using MediatR;

namespace EcommerceApi.Application.Authentication.Login;

public sealed class LoginCommandHandler(
    ITestCredentialsValidator credentialsValidator,
    IAccessTokenGenerator accessTokenGenerator) : IRequestHandler<LoginCommand, LoginResult>
{
    public Task<LoginResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        if (!credentialsValidator.AreValid(request.Email!, request.Password!))
        {
            return Task.FromResult(LoginResult.InvalidCredentials());
        }

        var accessToken = accessTokenGenerator.Generate(request.Email!);
        return Task.FromResult(LoginResult.Authenticated(accessToken));
    }
}
