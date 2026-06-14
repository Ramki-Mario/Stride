using System.Security.Cryptography;
using System.Text;
using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Commands.LoginUser;
using RefreshTokenEntity = STRIDE.Modules.Identity.Domain.Entities.RefreshToken;

namespace STRIDE.Modules.Identity.Application.Commands.AcceptInvite;

internal sealed class AcceptInviteCommandHandler
    : IRequestHandler<AcceptInviteCommand, Result<LoginResult>>
{
    private readonly IInviteTokenRepository              _inviteTokens;
    private readonly IUserRepository                     _users;
    private readonly IRoleRepository                     _roles;
    private readonly IRefreshTokenRepository             _refreshTokens;
    private readonly IPasswordHasher                     _hasher;
    private readonly IJwtTokenService                    _jwt;
    private readonly IRefreshTokenGenerator              _tokenGenerator;
    private readonly ITenantContextSetter                _tenantSetter;
    private readonly ILogger<AcceptInviteCommandHandler> _logger;

    public AcceptInviteCommandHandler(
        IInviteTokenRepository              inviteTokens,
        IUserRepository                     users,
        IRoleRepository                     roles,
        IRefreshTokenRepository             refreshTokens,
        IPasswordHasher                     hasher,
        IJwtTokenService                    jwt,
        IRefreshTokenGenerator              tokenGenerator,
        ITenantContextSetter                tenantSetter,
        ILogger<AcceptInviteCommandHandler> logger)
    {
        _inviteTokens   = inviteTokens;
        _users          = users;
        _roles          = roles;
        _refreshTokens  = refreshTokens;
        _hasher         = hasher;
        _jwt            = jwt;
        _tokenGenerator = tokenGenerator;
        _tenantSetter   = tenantSetter;
        _logger         = logger;
    }

    public async Task<Result<LoginResult>> Handle(
        AcceptInviteCommand request,
        CancellationToken   cancellationToken)
    {
        var tokenHash = HashToken(request.Token);
        var invite    = await _inviteTokens.GetByHashAsync(tokenHash, cancellationToken);

        if (invite is null || !invite.IsValid(DateTime.UtcNow))
        {
            _logger.LogWarning("Accept-invite attempt with invalid/expired token hash {Hash}.", tokenHash[..8]);
            return Result.Failure<LoginResult>("The invite link is invalid or has expired.");
        }

        _tenantSetter.SetTenantId(invite.TenantId);

        var user = await _users.GetByIdAsync(invite.UserId, cancellationToken);
        if (user is null)
            return Result.Failure<LoginResult>("User not found.");

        // Activate the user
        var passwordHash = _hasher.Hash(request.Password);
        user.UpdatePassword(passwordHash);
        user.Reactivate(); // sets IsActive=true, IsPending=false

        invite.MarkUsed();
        await _inviteTokens.SaveChangesAsync(cancellationToken);

        // Build login result (same as LoginCommandHandler)
        var allRoles  = await _roles.GetAllAsync(cancellationToken);
        var roleIndex = allRoles.ToDictionary(r => r.Id, r => r.Name);
        var roleNames = user.Roles
            .Where(ur => !ur.IsDeleted && roleIndex.ContainsKey(ur.RoleId))
            .Select(ur => roleIndex[ur.RoleId])
            .ToList()
            .AsReadOnly();

        var jwtResult = _jwt.Generate(new JwtTokenRequest(
            UserId:      user.Id,
            TenantId:    invite.TenantId,
            Email:       user.Email,
            DisplayName: user.DisplayName,
            Roles:       roleNames));

        var (rawRefresh, refreshExpiry) = _tokenGenerator.Generate();
        var refreshToken = RefreshTokenEntity.Create(
            tenantId:  invite.TenantId,
            userId:    user.Id,
            token:     rawRefresh,
            expiresAt: refreshExpiry,
            createdBy: user.Id);

        await _refreshTokens.AddAsync(refreshToken, cancellationToken);
        await _refreshTokens.SaveChangesAsync(cancellationToken);

        _logger.LogInformation("User {UserId} accepted invite and activated account.", user.Id);

        return Result.Success(new LoginResult(
            UserId:                   user.Id,
            TenantId:                 invite.TenantId,
            Email:                    user.Email,
            DisplayName:              user.DisplayName,
            Roles:                    roleNames,
            AccessToken:              jwtResult.AccessToken,
            AccessTokenExpiresAtUtc:  jwtResult.ExpiresAtUtc,
            RefreshToken:             rawRefresh,
            RefreshTokenExpiresAtUtc: refreshExpiry));
    }

    private static string HashToken(string rawToken)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(rawToken));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
