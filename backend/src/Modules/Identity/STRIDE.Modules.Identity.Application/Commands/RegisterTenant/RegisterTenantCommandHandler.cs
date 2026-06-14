using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Commands.LoginUser;
using STRIDE.Modules.Identity.Domain.Entities;
using RefreshTokenEntity = STRIDE.Modules.Identity.Domain.Entities.RefreshToken;

namespace STRIDE.Modules.Identity.Application.Commands.RegisterTenant;

/// <summary>
/// Atomically provisions a new tenant workspace:
///   1. Create Tenant record
///   2. Create Admin role for the new tenant
///   3. Create the first admin user (active, not pending)
///   4. Assign Admin role via the User aggregate
///   5. Create UserTenantMapping (required for TenantResolver on login)
///   6. Flush all changes in a single SaveChangesAsync call
///   7. Mint a JWT + refresh token and return LoginResult — the BFF will auto-issue a session cookie
/// </summary>
internal sealed class RegisterTenantCommandHandler
    : IRequestHandler<RegisterTenantCommand, Result<LoginResult>>
{
    private const  string AdminRoleName  = "Admin";
    private static readonly Guid SystemActorId = Guid.Empty;

    private readonly ITenantContextSetter    _tenantSetter;
    private readonly ITenantRepository       _tenants;
    private readonly IRoleRepository         _roles;
    private readonly IUserRepository         _users;
    private readonly IPasswordHasher         _hasher;
    private readonly IJwtTokenService        _jwt;
    private readonly IRefreshTokenRepository _refreshTokens;
    private readonly IRefreshTokenGenerator  _tokenGenerator;
    private readonly ILogger<RegisterTenantCommandHandler> _logger;

    public RegisterTenantCommandHandler(
        ITenantContextSetter    tenantSetter,
        ITenantRepository       tenants,
        IRoleRepository         roles,
        IUserRepository         users,
        IPasswordHasher         hasher,
        IJwtTokenService        jwt,
        IRefreshTokenRepository refreshTokens,
        IRefreshTokenGenerator  tokenGenerator,
        ILogger<RegisterTenantCommandHandler> logger)
    {
        _tenantSetter   = tenantSetter;
        _tenants        = tenants;
        _roles          = roles;
        _users          = users;
        _hasher         = hasher;
        _jwt            = jwt;
        _refreshTokens  = refreshTokens;
        _tokenGenerator = tokenGenerator;
        _logger         = logger;
    }

    public async Task<Result<LoginResult>> Handle(
        RegisterTenantCommand request,
        CancellationToken cancellationToken)
    {
        // ── 1. Create Tenant ──────────────────────────────────────────────────
        var tenant = Tenant.Create(
            name:      request.OrgName.Trim(),
            slug:      request.Slug.Trim().ToLowerInvariant(),
            plan:      request.Plan.Trim(),
            createdBy: SystemActorId);

        await _tenants.AddAsync(tenant, cancellationToken);

        // ── 2. Establish tenant scope for all downstream repositories ─────────
        _tenantSetter.SetTenantId(tenant.Id);

        // ── 3. Create Admin role ──────────────────────────────────────────────
        var adminRole = Role.Create(
            tenantId:    tenant.Id,
            name:        AdminRoleName,
            description: "Full administrative access",
            createdBy:   SystemActorId);

        await _roles.AddAsync(adminRole, cancellationToken);

        // ── 4. Create first admin user ────────────────────────────────────────
        var passwordHash = _hasher.Hash(request.AdminPassword);
        var user = User.Create(
            tenantId:     tenant.Id,
            email:        request.AdminEmail.Trim(),
            displayName:  request.AdminDisplayName.Trim(),
            passwordHash: passwordHash,
            createdBy:    SystemActorId);

        user.ClearDomainEvents(); // No dispatcher active — suppress events

        // ── 5. Assign Admin role via domain aggregate ─────────────────────────
        user.AssignRole(adminRole, SystemActorId);

        await _users.AddAsync(user, cancellationToken);

        // ── 6. UserTenantMapping (TenantResolver fallback for generic email domains) ──
        var mapping = UserTenantMapping.Create(
            tenantId:  tenant.Id,
            userId:    user.Id,
            roleName:  AdminRoleName,
            createdBy: SystemActorId);

        await _users.AddTenantMappingAsync(mapping, cancellationToken);

        // ── 7. Flush — all entities saved in one EF transaction ───────────────
        await _users.SaveChangesAsync(cancellationToken);

        _logger.TenantRegistered(tenant.Id, tenant.Slug, user.Id);

        // ── 8. Mint JWT + refresh token ───────────────────────────────────────
        var roleNames   = new[] { AdminRoleName }.AsReadOnly();
        var jwtResult   = _jwt.Generate(new JwtTokenRequest(
            UserId:      user.Id,
            TenantId:    tenant.Id,
            Email:       user.Email,
            DisplayName: user.DisplayName,
            Roles:       roleNames));

        var (rawToken, refreshExpiry) = _tokenGenerator.Generate();
        var refreshToken = RefreshTokenEntity.Create(
            tenantId:  tenant.Id,
            userId:    user.Id,
            token:     rawToken,
            expiresAt: refreshExpiry,
            createdBy: user.Id);

        await _refreshTokens.AddAsync(refreshToken, cancellationToken);
        await _refreshTokens.SaveChangesAsync(cancellationToken);

        return Result.Success(new LoginResult(
            UserId:                   user.Id,
            TenantId:                 tenant.Id,
            Email:                    user.Email,
            DisplayName:              user.DisplayName,
            Roles:                    roleNames,
            AccessToken:              jwtResult.AccessToken,
            AccessTokenExpiresAtUtc:  jwtResult.ExpiresAtUtc,
            RefreshToken:             rawToken,
            RefreshTokenExpiresAtUtc: refreshExpiry));
    }
}
