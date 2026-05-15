using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Identity.Application.Commands.RegisterUser;

/// <summary>
/// Self-registers a new user.  The tenant is resolved from the email address
/// using the same domain-lookup / fallback strategy as login.
/// </summary>
public sealed record RegisterCommand(
    string Email,
    string Password,
    string DisplayName) : IRequest<Result<RegisterResult>>;
