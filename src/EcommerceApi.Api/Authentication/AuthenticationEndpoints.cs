using EcommerceApi.Application.Authentication.Login;
using EcommerceApi.Api.OpenApi;
using MediatR;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;

namespace EcommerceApi.Api.Authentication;

public static class AuthenticationEndpoints
{
    public static IEndpointRouteBuilder MapAuthenticationEndpoints(this IEndpointRouteBuilder endpoints)
    {
        var group = endpoints.MapGroup("/auth").WithTags("Authentication");
        group.MapPost("/login", LoginAsync)
            .AllowAnonymous()
            .WithName("Login")
            .WithSummary("Issues a JWT for the fixed evaluator credentials.")
            .WithDescription("Anonymous evaluator-only login. The fixed credentials are a practical-test fixture, not a production identity design. Use the returned accessToken as Authorization: Bearer <accessToken> on protected API routes.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized)
            .ProducesProblem(StatusCodes.Status500InternalServerError)
            .WithJsonRequestExample("""
                {
                  "email": "dev@martech.com",
                  "password": "Senha@123"
                }
                """)
            .WithJsonResponseExample(StatusCodes.Status200OK, """
                {
                  "accessToken": "<jwt>",
                  "expiresAtUtc": "2026-09-02T13:00:00Z"
                }
                """)
            .WithProblemDetailsExamples(
                StatusCodes.Status400BadRequest,
                StatusCodes.Status401Unauthorized,
                StatusCodes.Status500InternalServerError);

        return endpoints;
    }

    private static async Task<Results<Ok<LoginResponse>, ProblemHttpResult>> LoginAsync(
        LoginRequest request,
        ISender sender,
        CancellationToken cancellationToken)
    {
        var result = await sender.Send(new LoginCommand(request.Email, request.Password), cancellationToken);
        if (!result.IsAuthenticated)
        {
            return TypedResults.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Invalid credentials.",
                type: "https://httpstatuses.com/401");
        }

        var accessToken = result.AccessToken!;
        return TypedResults.Ok(new LoginResponse(accessToken.Value, accessToken.ExpiresAtUtc));
    }
}
