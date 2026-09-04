using System.Reflection;
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
        Assembly applicationAssembly = typeof(ApplicationDependencyInjection).Assembly;

        services.AddMediatR(configuration =>
            configuration.RegisterServicesFromAssembly(applicationAssembly));
        services.AddValidatorsFromAssembly(applicationAssembly);
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(LoggingBehavior<,>));
        services.AddTransient(typeof(IPipelineBehavior<,>), typeof(ValidationBehavior<,>));
        services.AddSingleton<ITestCredentialsValidator, InMemoryTestCredentialsValidator>();

        return services;
    }
}
