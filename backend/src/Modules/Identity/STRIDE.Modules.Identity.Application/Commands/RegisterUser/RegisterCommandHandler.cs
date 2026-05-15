using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Application.Commands.RegisterUser;

internal sealed class RegisterCommandHandler
    : IRequestHandler<RegisterCommand, Result<RegisterResult>>
{
    // System actor GUID used as CreatedBy for self-registration
    // (no authenticated user exists yet at this point).
    private static readonly Guid SystemActorId = Guid.Empty;

    private readonly ITenantResolver       _tenantResolver;
    private readonly ITenantContextSetter  _tenantSetter;
    private readonly IUserRepository       _users;
    private readonly IPasswordHasher       _hasher;
    private readonly ILogger<RegisterCommandHandler> _logger;

    public RegisterCommandHandler(
        ITenantResolver      tenantResolver,
        ITenantContextSetter tenantSetter,
        IUserRepository      users,
        IPasswordHasher      hasher,
        ILogger<RegisterCommandHandler> logger)
    {
        _tenantResolver = tenantResolver;
        _tenantSetter   = tenantSetter;
        _users          = users;
        _hasher         = hasher;
        _logger         = logger;
    }

    public async Task<Result<RegisterResult>> Handle(
        RegisterCommand request,
        CancellationToken ct)
    {
        // ── 1. Resolve tenant ─────────────────────────────────────────────
        var tenantId = await _tenantResolver.ResolveFromEmailAsync(request.Email, ct);
        if (tenantId is null)
        {
            _logger.LogWarning(
                "Registration failed: no tenant found for email domain of {Email}", request.Email);
            return Result.Failure<RegisterResult>(
                "No tenant could be associated with this email address. " +
                "Contact your administrator to set up your organization.");
        }

        _tenantSetter.SetTenantId(tenantId.Value);

        // ── 2. Duplicate-email guard ──────────────────────────────────────
        var normalizedEmail = request.Email.Trim().ToUpperInvariant();
        if (await _users.ExistsByEmailAsync(normalizedEmail, ct))
        {
            _logger.LogWarning(
                "Registration failed: email {Email} already exists in tenant {TenantId}",
                request.Email, tenantId.Value);
            return Result.Failure<RegisterResult>("An account with this email address already exists.");
        }

        // ── 3. Hash password and create user ──────────────────────────────
        var passwordHash = _hasher.Hash(request.Password);
        var user = User.Create(
            tenantId:    tenantId.Value,
            email:       request.Email.Trim(),
            displayName: request.DisplayName.Trim(),
            passwordHash: passwordHash,
            createdBy:   SystemActorId);

        await _users.AddAsync(user, ct);
        await _users.SaveChangesAsync(ct);

        _logger.LogInformation(
            "User {UserId} registered in tenant {TenantId}", user.Id, tenantId.Value);

        return Result.Success(new RegisterResult(
            UserId:      user.Id,
            TenantId:    tenantId.Value,
            Email:       user.Email,
            DisplayName: user.DisplayName));
    }
}
