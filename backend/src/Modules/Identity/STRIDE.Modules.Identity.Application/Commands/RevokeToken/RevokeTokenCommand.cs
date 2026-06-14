using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Identity.Application.Commands.RevokeToken;

/// <summary>
/// Revokes a refresh token so it can no longer be used to obtain new access tokens.
/// Used on explicit logout. Idempotent — already-revoked tokens return success.
/// </summary>
public sealed record RevokeTokenCommand(string Token) : IRequest<Result>;
