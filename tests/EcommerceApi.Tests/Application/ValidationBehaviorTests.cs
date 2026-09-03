using EcommerceApi.Application;
using EcommerceApi.Application.Common.Behaviors;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceApi.Tests.Application;

public sealed class ValidationBehaviorTests
{
    [Fact]
    public async Task Handle_WithValidationFailure_ThrowsAndDoesNotInvokeNext()
    {
        var nextWasInvoked = false;
        var behavior = new ValidationBehavior<TestRequest, string>([new TestRequestValidator()]);

        async Task<string> Next(CancellationToken _)
        {
            nextWasInvoked = true;
            return await Task.FromResult("handled");
        }

        var exception = await Assert.ThrowsAsync<ValidationException>(() =>
            behavior.Handle(new TestRequest(string.Empty), Next, CancellationToken.None));

        Assert.False(nextWasInvoked);
        Assert.Single(exception.Errors);
        Assert.Equal(nameof(TestRequest.Value), exception.Errors.Single().PropertyName);
    }

    [Fact]
    public async Task Handle_WithValidRequest_InvokesNext()
    {
        var behavior = new ValidationBehavior<TestRequest, string>([new TestRequestValidator()]);

        var result = await behavior.Handle(
            new TestRequest("valid"),
            _ => Task.FromResult("handled"),
            CancellationToken.None);

        Assert.Equal("handled", result);
    }

    [Fact]
    public void AddApplication_RegistersValidationPipelineBehavior()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        Assert.Contains(services, descriptor =>
            descriptor.ServiceType == typeof(IPipelineBehavior<,>) &&
            descriptor.ImplementationType == typeof(ValidationBehavior<,>));
    }

    [Fact]
    public void AddApplication_RegistersLoggingBeforeValidationPipelineBehavior()
    {
        var services = new ServiceCollection();

        services.AddApplication();

        var pipelineBehaviors = services
            .Where(descriptor => descriptor.ServiceType == typeof(IPipelineBehavior<,>))
            .Select(descriptor => descriptor.ImplementationType)
            .ToArray();

        Assert.Equal(typeof(LoggingBehavior<,>), pipelineBehaviors[0]);
        Assert.Equal(typeof(ValidationBehavior<,>), pipelineBehaviors[1]);
    }

    private sealed record TestRequest(string Value) : IRequest<string>;

    private sealed class TestRequestValidator : AbstractValidator<TestRequest>
    {
        public TestRequestValidator()
        {
            RuleFor(request => request.Value).NotEmpty();
        }
    }
}
