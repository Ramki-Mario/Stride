using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Commands.RefreshToken;
using STRIDE.Modules.Identity.Domain.Entities;
using STRIDE.Modules.Identity.Domain.ValueObjects;

namespace STRIDE.Modules.Identity.Application.Tests;

public sealed class RefreshTokenCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokens  = Substitute.For<IRefreshTokenRepository>();
    private readonly IRefreshTokenGenerator  _tokenGenerator = Substitute.For<IRefreshTokenGenerator>();
    private readonly IUserRepository         _users          = Substitute.For<IUserRepository>();
    private readonly IRoleRepository         _roles          = Substitute.For<IRoleRepository>();
    private readonly IJwtTokenService        _jwt            = Substitute.For<IJwtTokenService>();
    private readonly ITenantContextSetter    _tenantSetter   = Substitute.For<ITenantContextSetter>();

    private readonly RefreshTokenCommandHandler _sut;

    private static readonly Guid   TenantId   = Guid.NewGuid();
    private static readonly Guid   UserId     = Guid.NewGuid();
    private static readonly string OldToken   = new('a', 64);
    private static readonly string NewToken   = new('b', 64);
    private static readonly string JwtStr     = "new_jwt_value";
    private static readonly DateTime Future   = DateTime.UtcNow.AddDays(7);
    private static readonly DateTime NewExpiry = DateTime.UtcNow.AddDays(7);

    public RefreshTokenCommandHandlerTests()
    {
        _sut = new RefreshTokenCommandHandler(
            _refreshTokens, _tokenGenerator, _users, _roles, _jwt,
            _tenantSetter,
            NullLogger<RefreshTokenCommandHandler>.Instance);
    }

    // ── Success path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidToken_ReturnsNewTokenPair()
    {
        SetupHappyPath();

        var result = await _sut.Handle(new RefreshTokenCommand(OldToken), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be(JwtStr);
        result.Value.RefreshToken.Should().Be(NewToken);
    }

    [Fact]
    public async Task Handle_WithValidToken_SetsTenantContextFromToken()
    {
        SetupHappyPath();

        await _sut.Handle(new RefreshTokenCommand(OldToken), CancellationToken.None);

        _tenantSetter.Received(1).SetTenantId(TenantId);
    }

    [Fact]
    public async Task Handle_WithValidToken_RevokesOldTokenWithRotationPointer()
    {
        var existing = BuildActiveRefreshToken();
        _refreshTokens.GetByTokenCrossTenantAsync(OldToken, Arg.Any<CancellationToken>()).Returns(existing);
        SetupUserAndRoles();
        _tokenGenerator.Generate().Returns((NewToken, NewExpiry));
        _jwt.Generate(Arg.Any<JwtTokenRequest>())
            .Returns(new JwtTokenResult(JwtStr, DateTime.UtcNow.AddHours(1)));

        await _sut.Handle(new RefreshTokenCommand(OldToken), CancellationToken.None);

        existing.RevokedAt.Should().NotBeNull();
        existing.ReplacedByToken.Should().Be(NewToken);
    }

    [Fact]
    public async Task Handle_WithValidToken_PersistsNewRefreshToken()
    {
        SetupHappyPath();

        await _sut.Handle(new RefreshTokenCommand(OldToken), CancellationToken.None);

        await _refreshTokens.Received(1).AddAsync(
            Arg.Is<Domain.Entities.RefreshToken>(t => t.Token == NewToken && t.UserId == UserId),
            Arg.Any<CancellationToken>());
    }

    // ── Failure paths ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTokenNotFound_ReturnsFailure()
    {
        _refreshTokens.GetByTokenCrossTenantAsync(OldToken, Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.RefreshToken?)null);

        var result = await _sut.Handle(new RefreshTokenCommand(OldToken), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("invalid or has expired");
    }

    [Fact]
    public async Task Handle_WhenTokenIsExpired_ReturnsFailure()
    {
        var expired = Domain.Entities.RefreshToken.Create(
            TenantId, UserId, OldToken,
            expiresAt: DateTime.UtcNow.AddDays(-1),
            createdBy: UserId);
        _refreshTokens.GetByTokenCrossTenantAsync(OldToken, Arg.Any<CancellationToken>()).Returns(expired);

        var result = await _sut.Handle(new RefreshTokenCommand(OldToken), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTokenIsRevoked_ReturnsFailure()
    {
        var revoked = BuildActiveRefreshToken();
        revoked.Revoke(DateTime.UtcNow.AddMinutes(-1));
        _refreshTokens.GetByTokenCrossTenantAsync(OldToken, Arg.Any<CancellationToken>()).Returns(revoked);

        var result = await _sut.Handle(new RefreshTokenCommand(OldToken), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenUserInactive_ReturnsFailure()
    {
        var existing = BuildActiveRefreshToken();
        _refreshTokens.GetByTokenCrossTenantAsync(OldToken, Arg.Any<CancellationToken>()).Returns(existing);

        var inactive = User.Create(TenantId, "alice@a.com", "Alice",
            Password.FromHash("h"), Guid.NewGuid());
        inactive.Deactivate();
        inactive.ClearDomainEvents();
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(inactive);

        var result = await _sut.Handle(new RefreshTokenCommand(OldToken), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private static Domain.Entities.RefreshToken BuildActiveRefreshToken()
        => Domain.Entities.RefreshToken.Create(TenantId, UserId, OldToken, Future, UserId);

    private void SetupUserAndRoles()
    {
        var user = User.Create(TenantId, "alice@a.com", "Alice",
            Password.FromHash("h"), Guid.NewGuid());
        user.ClearDomainEvents();
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _roles.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Role>() as IReadOnlyList<Role>);
    }

    private void SetupHappyPath()
    {
        var existing = BuildActiveRefreshToken();
        _refreshTokens.GetByTokenCrossTenantAsync(OldToken, Arg.Any<CancellationToken>()).Returns(existing);
        SetupUserAndRoles();
        _tokenGenerator.Generate().Returns((NewToken, NewExpiry));
        _jwt.Generate(Arg.Any<JwtTokenRequest>())
            .Returns(new JwtTokenResult(JwtStr, DateTime.UtcNow.AddHours(1)));
    }
}
