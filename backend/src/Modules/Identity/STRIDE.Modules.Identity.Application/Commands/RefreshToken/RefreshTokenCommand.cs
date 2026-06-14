using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Identity.Application.Commands.RefreshToken;

/// <summary>
/// Validates an existing refresh token, rotates it, and returns a new access+refresh token pair.
/// The old refresh token is revoked; the new one is persisted. Rotation prevents replay attacks.
/// </summary>
public sealed record RefreshTokenCommand(string Token) : IRequest<Result<TokenPairResult>>;
