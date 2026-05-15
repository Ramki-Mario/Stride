using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Identity.Application.Commands.LoginUser;

/// <summary>
/// Authenticates a user by email and password.
/// Resolves the tenant from the email address, verifies the credentials,
/// and returns a signed JWT access token together with the user's profile.
/// </summary>
public sealed record LoginCommand(
    string Email,
    string Password) : IRequest<Result<LoginResult>>;
