using EcommerceApi.Api.Authentication;
using EcommerceApi.Api.ErrorHandling;
using EcommerceApi.Api.OpenApi;
using EcommerceApi.Api.Orders;
using EcommerceApi.Application;
using EcommerceApi.Infrastructure;
using EcommerceApi.Infrastructure.Persistence;
using Serilog;

var builder = WebApplication.CreateBuilder(args);

builder.Logging.ClearProviders();
builder.Host.UseSerilog((context, configuration) =>
{
    configuration
        .ReadFrom.Configuration(context.Configuration)
        .Enrich.FromLogContext()
        .WriteTo.Console();
});
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();
builder.Services.AddEcommerceOpenApi();
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);
builder.Services.AddJwtAccessTokenGeneration(builder.Configuration);
builder.Services.AddJwtAuthentication(builder.Configuration);

var app = builder.Build();

await app.Services.ApplyDatabaseMigrationsAsync();

app.UseExceptionHandler();
app.UseAuthentication();
app.UseAuthorization();
app.UseEcommerceOpenApi();
app.MapAuthenticationEndpoints();
app.MapOrderEndpoints();

app.Run();

public partial class Program
{
}
