using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Abstractions;
using RefreshTokenEntity = STRIDE.Modules.Identity.Domain.Entities.RefreshToken;

namespace STRIDE.Modules.Identity.Application.Commands.RefreshToken;

internal sealed class RefreshTokenCommandHandler
    : IRequestHandler<RefreshTokenCommand, Result<TokenPairResult>>
{
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IRefreshTokenGenerator  _tokenGenerator;
    private readonly IUserRepository         _users;
    private readonly IRoleRepository         _roles;
    private readonly IJwtTokenService        _jwt;
    private readonly ILogger<RefreshTokenCommandHandler> _logger;

    public RefreshTokenCommandHandler(
        IRefreshTokenRepository refreshTokens,
        IRefreshTokenGenerator  tokenGenerator,
        IUserRepository         users,
        IRoleRepository         roles,
        IJwtTokenService        jwt,
        ILogger<RefreshTokenCommandHandler> logger)
    {
        _refreshTokens  = refreshTokens;
        _tokenGenerator = tokenGenerator;
        _users          = users;
        _roles          = roles;
        _jwt            = jwt;
        _logger         = logger;
    }

    public async Task<Result<TokenPairResult>> Handle(
        RefreshTokenCommand request,
        CancellationToken cancellationToken)
    {
        var utcNow = DateTime.UtcNow;

        // ── 1. Load and validate the incoming token ───────────────────────
        var existing = await _refreshTokens.GetByTokenAsync(request.Token, cancellationToken);
        if (existing is null || !existing.IsActive(utcNow))
        {
            _logger.RefreshTokenInvalid();
            return Result.Failure<TokenPairResult>("Refresh token is invalid or has expired.");
        }

        // ── 2. Load user ──────────────────────────────────────────────────
        var user = await _users.GetByIdAsync(existing.UserId, cancellationToken);
        if (user is null || !user.IsActive)
        {
            _logger.RefreshTokenUserNotFound(existing.UserId);
            return Result.Failure<TokenPairResult>("Refresh token is invalid or has expired.");
        }

        // ── 3. Resolve roles ──────────────────────────────────────────────
        var allRoles  = await _roles.GetAllAsync(cancellationToken);
        var roleIndex = allRoles.ToDictionary(r => r.Id, r => r.Name);
        var roleNames = user.Roles
            .Where(ur => !ur.IsDeleted && roleIndex.ContainsKey(ur.RoleId))
            .Select(ur => roleIndex[ur.RoleId])
            .ToList()
            .AsReadOnly();

        // ── 4. Rotate: revoke old, create new ─────────────────────────────
        var (newRawToken, newExpiresAt) = _tokenGenerator.Generate();

        existing.Revoke(utcNow, replacedByToken: newRawToken);

        var newRefreshToken = RefreshTokenEntity.Create(
            tenantId:  existing.TenantId,
            userId:    existing.UserId,
            token:     newRawToken,
            expiresAt: newExpiresAt,
            createdBy: existing.UserId);

        await _refreshTokens.AddAsync(newRefreshToken, cancellationToken);
        await _refreshTokens.SaveChangesAsync(cancellationToken);

        // ── 5. Mint new access token ──────────────────────────────────────
        var jwtResult = _jwt.Generate(new JwtTokenRequest(
            UserId:      user.Id,
            TenantId:    user.TenantId,
            Email:       user.Email,
            DisplayName: user.DisplayName,
            Roles:       roleNames));

        _logger.RefreshTokenRotated(user.Id, user.TenantId);

        return Result.Success(new TokenPairResult(
            AccessToken:              jwtResult.AccessToken,
            AccessTokenExpiresAtUtc:  jwtResult.ExpiresAtUtc,
            RefreshToken:             newRawToken,
            RefreshTokenExpiresAtUtc: newExpiresAt));
    }
}
