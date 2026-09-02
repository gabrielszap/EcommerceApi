using MediatR;

namespace EcommerceApi.Application.Authentication.Login;

public sealed record LoginCommand(string? Email, string? Password) : IRequest<LoginResult>;
