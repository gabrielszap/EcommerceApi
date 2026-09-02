using EcommerceApi.Application;
using EcommerceApi.Application.Authentication;
using EcommerceApi.Application.Authentication.Login;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceApi.Tests.Application.Authentication;

public sealed class LoginValidationPipelineTests
{
    [Fact]
    public async Task Send_WithMissingCredentials_ThrowsValidationExceptionBeforeTheHandler()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication();
        services.AddSingleton<IAccessTokenGenerator>(new StubAccessTokenGenerator());
        await using var provider = services.BuildServiceProvider();

        var sender = provider.GetRequiredService<ISender>();

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            sender.Send(new LoginCommand(null, null), CancellationToken.None));

        Assert.Contains(exception.Errors, error => error.PropertyName == "Email");
        Assert.Contains(exception.Errors, error => error.PropertyName == "Password");
    }

    private sealed class StubAccessTokenGenerator : IAccessTokenGenerator
    {
        public AccessToken Generate(string email) =>
            new("access-token", DateTime.UtcNow.AddHours(1));
    }
}
