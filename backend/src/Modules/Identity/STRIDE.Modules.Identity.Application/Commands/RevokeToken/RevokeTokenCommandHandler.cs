using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Abstractions;

namespace STRIDE.Modules.Identity.Application.Commands.RevokeToken;

internal sealed class RevokeTokenCommandHandler : IRequestHandler<RevokeTokenCommand, Result>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly ITenantContextSetter    _tenantSetter;
    private readonly ILogger<RevokeTokenCommandHandler> _logger;

    public RevokeTokenCommandHandler(
        IRefreshTokenRepository refreshTokens,
        ITenantContextSetter    tenantSetter,
        ILogger<RevokeTokenCommandHandler> logger)
    {
        _refreshTokens = refreshTokens;
        _tenantSetter  = tenantSetter;
        _logger        = logger;
    }

    public async Task<Result> Handle(RevokeTokenCommand request, CancellationToken cancellationToken)
    {
        var token = await _refreshTokens.GetByTokenCrossTenantAsync(request.Token, cancellationToken);
        if (token is null)
        {
            _logger.RevokeTokenNotFound();
            return Result.Failure("Refresh token not found.");
        }

        _tenantSetter.SetTenantId(token.TenantId);

        // Idempotent — already revoked tokens are silently accepted (covers double-logout).
        if (token.RevokedAt is null)
        {
            token.Revoke(DateTime.UtcNow);
            await _refreshTokens.SaveChangesAsync(cancellationToken);
        }

        _logger.RevokeTokenSucceeded(token.UserId, token.TenantId);
        return Result.Success();
    }
}
