using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Commands.LoginUser;
using STRIDE.Modules.Identity.Application.Commands.RegisterTenant;
using STRIDE.Modules.Identity.Domain;
using STRIDE.Modules.Identity.Domain.Entities;
using PasswordVO = STRIDE.Modules.Identity.Domain.ValueObjects.Password;

namespace STRIDE.Modules.Identity.Application.Tests;

public sealed class RegisterTenantCommandHandlerTests
{
    private readonly ITenantContextSetter    _tenantSetter   = Substitute.For<ITenantContextSetter>();
    private readonly ITenantRepository       _tenants        = Substitute.For<ITenantRepository>();
    private readonly IRoleRepository         _roles          = Substitute.For<IRoleRepository>();
    private readonly IPermissionRepository   _permissions    = Substitute.For<IPermissionRepository>();
    private readonly IUserRepository         _users          = Substitute.For<IUserRepository>();
    private readonly IPasswordHasher         _hasher         = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService        _jwt            = Substitute.For<IJwtTokenService>();
    private readonly IRefreshTokenRepository _refreshTokens  = Substitute.For<IRefreshTokenRepository>();
    private readonly IRefreshTokenGenerator  _tokenGenerator = Substitute.For<IRefreshTokenGenerator>();

    private readonly RegisterTenantCommandHandler _sut;

    private static readonly string OrgName     = "Acme Corp";
    private static readonly string Slug        = "acme";
    private static readonly string Plan        = "Pro";
    private static readonly string AdminEmail  = "admin@acme.com";
    private static readonly string AdminPass   = "Str0ng!Pass";
    private static readonly string AdminName   = "Admin User";
    private static readonly string TokenStr    = "jwt_token";
    private static readonly string RefreshStr  = new('r', 64);
    private static readonly DateTime RefreshExp = DateTime.UtcNow.AddDays(7);

    public RegisterTenantCommandHandlerTests()
    {
        _sut = new RegisterTenantCommandHandler(
            _tenantSetter, _tenants, _roles, _permissions, _users,
            _hasher, _jwt, _refreshTokens, _tokenGenerator,
            NullLogger<RegisterTenantCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_CreatesAndReturnsTenantAdminRole()
    {
        SetupHappyPath();

        var result = await _sut.Handle(BuildCommand(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Roles.Should().Contain("TenantAdmin");
    }

    [Fact]
    public async Task Handle_PersistsTenantAdminRoleWithIsSystemRoleTrue()
    {
        SetupHappyPath();

        await _sut.Handle(BuildCommand(), CancellationToken.None);

        await _roles.Received(1).AddAsync(
            Arg.Is<Role>(r => r.IsSystemRole && r.NormalizedName == "TENANTADMIN"),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_GrantsAllPermissionsToTenantAdminRole()
    {
        var allPerms = DefaultPermissions.All
            .Select(p => Permission.Create(p.Key, p.Description))
            .ToList();

        _permissions.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(allPerms);

        SetupHappyPathExceptPermissions();

        await _sut.Handle(BuildCommand(), CancellationToken.None);

        await _roles.Received(1).AddAsync(
            Arg.Is<Role>(r => r.Permissions.Count == 10 && !r.Permissions.Any(p => p.IsDeleted)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AssignsTenantAdminRoleToUser()
    {
        SetupHappyPath();

        await _sut.Handle(BuildCommand(), CancellationToken.None);

        await _users.Received(1).AddAsync(
            Arg.Is<User>(u => u.Roles.Any(r => !r.IsDeleted)),
            Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_SetsTenantContextBeforeCreatingRole()
    {
        SetupHappyPath();

        await _sut.Handle(BuildCommand(), CancellationToken.None);

        Received.InOrder(() =>
        {
            _tenantSetter.SetTenantId(Arg.Any<Guid>());
            _ = _permissions.GetAllAsync(Arg.Any<CancellationToken>());
        });
    }

    [Fact]
    public async Task Handle_MintesJwtWithTenantAdminRole()
    {
        SetupHappyPath();

        await _sut.Handle(BuildCommand(), CancellationToken.None);

        _jwt.Received(1).Generate(Arg.Is<JwtTokenRequest>(r =>
            r.Roles.Contains("TenantAdmin")));
    }

    [Fact]
    public async Task Handle_PersistsRefreshToken()
    {
        SetupHappyPath();

        await _sut.Handle(BuildCommand(), CancellationToken.None);

        await _refreshTokens.Received(1).AddAsync(
            Arg.Is<Domain.Entities.RefreshToken>(t => t.Token == RefreshStr),
            Arg.Any<CancellationToken>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static RegisterTenantCommand BuildCommand()
        => new(OrgName, Slug, Plan, AdminEmail, AdminPass, AdminName);

    private void SetupHappyPath()
    {
        var permissions = DefaultPermissions.All
            .Select(p => Permission.Create(p.Key, p.Description))
            .ToList();

        _permissions.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(permissions);

        SetupHappyPathExceptPermissions();
    }

    private void SetupHappyPathExceptPermissions()
    {
        _hasher.Hash(AdminPass).Returns(PasswordVO.FromHash("hash"));
        _jwt.Generate(Arg.Any<JwtTokenRequest>())
            .Returns(new JwtTokenResult(TokenStr, DateTime.UtcNow.AddHours(1)));
        _tokenGenerator.Generate()
            .Returns((RefreshStr, RefreshExp));
    }
}
