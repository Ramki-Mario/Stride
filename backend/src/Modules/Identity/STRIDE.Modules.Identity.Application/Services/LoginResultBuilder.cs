using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Commands.LoginUser;
using STRIDE.Modules.Identity.Domain.Entities;
using RefreshTokenEntity = STRIDE.Modules.Identity.Domain.Entities.RefreshToken;

namespace STRIDE.Modules.Identity.Application.Services;

public sealed class LoginResultBuilder : ILoginResultBuilder
{
    private readonly IRoleRepository        _roles;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IJwtTokenService       _jwt;
    private readonly IRefreshTokenGenerator _tokenGenerator;

    public LoginResultBuilder(
        IRoleRepository         roles,
        IRefreshTokenRepository refreshTokens,
        IJwtTokenService        jwt,
        IRefreshTokenGenerator  tokenGenerator)
    {
        _roles          = roles;
        _refreshTokens  = refreshTokens;
        _jwt            = jwt;
        _tokenGenerator = tokenGenerator;
    }

    public async Task<LoginResult> BuildAsync(User user, Guid tenantId, CancellationToken ct = default)
    {
        var allRoles  = await _roles.GetAllAsync(ct);
        var roleIndex = allRoles.ToDictionary(r => r.Id, r => r.Name);
        var roleNames = user.Roles
            .Where(ur => !ur.IsDeleted && roleIndex.ContainsKey(ur.RoleId))
            .Select(ur => roleIndex[ur.RoleId])
            .ToList()
            .AsReadOnly();

        var jwtResult = _jwt.Generate(new JwtTokenRequest(
            UserId:      user.Id,
            TenantId:    tenantId,
            Email:       user.Email,
            DisplayName: user.DisplayName,
            Roles:       roleNames));

        var (rawRefresh, refreshExpiry) = _tokenGenerator.Generate();
        var refreshToken = RefreshTokenEntity.Create(
            tenantId:  tenantId,
            userId:    user.Id,
            token:     rawRefresh,
            expiresAt: refreshExpiry,
            createdBy: user.Id);

        await _refreshTokens.AddAsync(refreshToken, ct);
        await _refreshTokens.SaveChangesAsync(ct);

        return new LoginResult(
            UserId:                   user.Id,
            TenantId:                 tenantId,
            Email:                    user.Email,
            DisplayName:              user.DisplayName,
            Roles:                    roleNames,
            AccessToken:              jwtResult.AccessToken,
            AccessTokenExpiresAtUtc:  jwtResult.ExpiresAtUtc,
            RefreshToken:             rawRefresh,
            RefreshTokenExpiresAtUtc: refreshExpiry);
    }
}
