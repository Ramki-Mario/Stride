using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application;
using STRIDE.Modules.Identity.Application.Abstractions;
using RefreshTokenEntity = STRIDE.Modules.Identity.Domain.Entities.RefreshToken;

namespace STRIDE.Modules.Identity.Application.Commands.LoginUser;

internal sealed class LoginCommandHandler
    : IRequestHandler<LoginCommand, Result<LoginResult>>
{
    private readonly ITenantResolver          _tenantResolver;
    private readonly ITenantContextSetter     _tenantSetter;
    private readonly IUserRepository          _users;
    private readonly IRoleRepository          _roles;
    private readonly IPasswordHasher          _hasher;
    private readonly IJwtTokenService         _jwt;
    private readonly IRefreshTokenRepository  _refreshTokens;
    private readonly IRefreshTokenGenerator   _tokenGenerator;
    private readonly ILogger<LoginCommandHandler> _logger;

    public LoginCommandHandler(
        ITenantResolver          tenantResolver,
        ITenantContextSetter     tenantSetter,
        IUserRepository          users,
        IRoleRepository          roles,
        IPasswordHasher          hasher,
        IJwtTokenService         jwt,
        IRefreshTokenRepository  refreshTokens,
        IRefreshTokenGenerator   tokenGenerator,
        ILogger<LoginCommandHandler> logger)
    {
        _tenantResolver = tenantResolver;
        _tenantSetter   = tenantSetter;
        _users          = users;
        _roles          = roles;
        _hasher         = hasher;
        _jwt            = jwt;
        _refreshTokens  = refreshTokens;
        _tokenGenerator = tokenGenerator;
        _logger         = logger;
    }

    public async Task<Result<LoginResult>> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Resolve tenant ─────────────────────────────────────────────
        var tenantId = await _tenantResolver.ResolveFromEmailAsync(request.Email, cancellationToken);
        if (tenantId is null)
        {
            _logger.LoginTenantNotFound(request.Email);
            return Result.Failure<LoginResult>("Invalid email or password.");
        }

        _tenantSetter.SetTenantId(tenantId.Value);

        // ── 2. Load user ──────────────────────────────────────────────────
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        var user = await _users.GetByNormalizedEmailAsync(normalizedEmail, cancellationToken);

        if (user is null || !user.IsActive)
        {
            _logger.LoginUserNotFound(request.Email);
            return Result.Failure<LoginResult>("Invalid email or password.");
        }

        // ── 3. Verify password ────────────────────────────────────────────
        if (!_hasher.Verify(request.Password, user.PasswordHash))
        {
            _logger.LoginIncorrectPassword(request.Email);
            return Result.Failure<LoginResult>("Invalid email or password.");
        }

        // ── 4. Resolve role names ─────────────────────────────────────────
        var allRoles  = await _roles.GetAllAsync(cancellationToken);
        var roleIndex = allRoles.ToDictionary(r => r.Id, r => r.Name);
        var roleNames = user.Roles
            .Where(ur => !ur.IsDeleted && roleIndex.ContainsKey(ur.RoleId))
            .Select(ur => roleIndex[ur.RoleId])
            .ToList()
            .AsReadOnly();

        // ── 5. Mint access token ──────────────────────────────────────────
        var jwtResult = _jwt.Generate(new JwtTokenRequest(
            UserId:      user.Id,
            TenantId:    tenantId.Value,
            Email:       user.Email,
            DisplayName: user.DisplayName,
            Roles:       roleNames));

        // ── 6. Issue refresh token ────────────────────────────────────────
        var (rawToken, expiresAt) = _tokenGenerator.Generate();
        var refreshToken = RefreshTokenEntity.Create(
            tenantId:  tenantId.Value,
            userId:    user.Id,
            token:     rawToken,
            expiresAt: expiresAt,
            createdBy: user.Id);

        await _refreshTokens.AddAsync(refreshToken, cancellationToken);
        await _refreshTokens.SaveChangesAsync(cancellationToken);

        _logger.LoginSucceeded(user.Id, tenantId.Value);

        return Result.Success(new LoginResult(
            UserId:                   user.Id,
            TenantId:                 tenantId.Value,
            Email:                    user.Email,
            DisplayName:              user.DisplayName,
            Roles:                    roleNames,
            AccessToken:              jwtResult.AccessToken,
            AccessTokenExpiresAtUtc:  jwtResult.ExpiresAtUtc,
            RefreshToken:             rawToken,
            RefreshTokenExpiresAtUtc: expiresAt));
    }
}
