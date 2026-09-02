using EcommerceApi.Application.Authentication.Login;
using FluentValidation;
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
            .WithDescription("Use the returned accessToken as Authorization: Bearer <accessToken> on protected API routes.")
            .Produces<LoginResponse>(StatusCodes.Status200OK)
            .ProducesValidationProblem(StatusCodes.Status400BadRequest)
            .ProducesProblem(StatusCodes.Status401Unauthorized);

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
