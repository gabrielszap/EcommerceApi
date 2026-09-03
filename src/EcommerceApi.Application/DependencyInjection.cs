using EcommerceApi.Application.Common.Behaviors;
using EcommerceApi.Application.Authentication;
using FluentValidation;
using MediatR;
using Microsoft.Extensions.DependencyInjection;

namespace EcommerceApi.Application;

public static class ApplicationDependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssemblyContaining<AssemblyMarker>());
        services.AddValidatorsFromAssemblyContaining<AssemblyMarker>();
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddSingleton<ITestCredentialsValidator, InMemoryTestCredentialsValidator>();

        return services;
    }
}
