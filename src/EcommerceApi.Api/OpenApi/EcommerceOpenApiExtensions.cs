using Microsoft.OpenApi;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace EcommerceApi.Api.OpenApi;

public static class EcommerceOpenApiExtensions
{
    private const string DocumentName = "v1";
    private const string DocumentPath = "/openapi/v1.json";
    private const string ApiTitle = "EcommerceApi";

    public static IServiceCollection AddEcommerceOpenApi(this IServiceCollection services)
    {
        services.AddOpenApi(options => options.AddDocumentTransformer((document, _, _) =>
        {
            document.Info = new OpenApiInfo
            {
                Title = ApiTitle,
                Version = DocumentName,
                Description = "Order-management practical-test API using ASP.NET Core Minimal APIs, Clean Architecture, CQRS with MediatR, JWT Bearer authentication, EF Core, and SQLite."
            };

            document.Tags = new HashSet<OpenApiTag>
            {
                new OpenApiTag
                {
                    Name = "Authentication",
                    Description = "Anonymous evaluator login endpoint for issuing JWT access tokens."
                },
                new OpenApiTag
                {
                    Name = "Orders",
                    Description = "JWT-protected order creation, query, and cancellation endpoints."
                }
            };

            document.Components ??= new OpenApiComponents();
            document.Components.SecuritySchemes ??= new Dictionary<string, IOpenApiSecurityScheme>();
            document.Components.SecuritySchemes["Bearer"] = new OpenApiSecurityScheme
            {
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                Description = "Use the token returned by POST /auth/login as: Authorization: Bearer <accessToken>."
            };

            return Task.CompletedTask;
        }));

        return services;
    }

    public static WebApplication UseEcommerceOpenApi(this WebApplication app)
    {
        if (!IsOpenApiEnabled(app.Environment, app.Configuration))
        {
            return app;
        }

        app.MapOpenApi();
        app.MapSwaggerUI("swagger", options =>
        {
            options.DocumentTitle = $"{ApiTitle} {DocumentName}";
            options.ConfigObject.Urls =
            [
                new UrlDescriptor
                {
                    Name = $"{ApiTitle} {DocumentName}",
                    Url = DocumentPath
                }
            ];
            options.ConfigObject.PersistAuthorization = false;
        });
        app.MapGet("/", () => Results.Redirect("/swagger"))
            .ExcludeFromDescription();

        return app;
    }

    private static bool IsOpenApiEnabled(IHostEnvironment environment, IConfiguration configuration) =>
        environment.IsDevelopment() || configuration.GetValue<bool>("OpenApi:Enabled");
}
