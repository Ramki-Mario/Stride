using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Commands.AcceptInvite;
using STRIDE.Modules.Identity.Application.Commands.LoginUser;
using STRIDE.Modules.Identity.Domain.Entities;
using STRIDE.Modules.Identity.Domain.ValueObjects;

namespace STRIDE.Modules.Identity.Application.Tests;

public sealed class AcceptInviteCommandHandlerTests
{
    private readonly IInviteTokenRepository              _inviteTokens  = Substitute.For<IInviteTokenRepository>();
    private readonly IUserRepository                     _users         = Substitute.For<IUserRepository>();
    private readonly IRoleRepository                     _roles         = Substitute.For<IRoleRepository>();
    private readonly IRefreshTokenRepository             _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly IPasswordHasher                     _hasher        = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService                    _jwt           = Substitute.For<IJwtTokenService>();
    private readonly IRefreshTokenGenerator              _tokenGen      = Substitute.For<IRefreshTokenGenerator>();
    private readonly ITenantContextSetter                _tenantSetter  = Substitute.For<ITenantContextSetter>();

    private readonly AcceptInviteCommandHandler _sut;

    private static readonly Guid   TenantId   = Guid.NewGuid();
    private static readonly Guid   UserId     = Guid.NewGuid();
    private static readonly string RawToken   = "valid_raw_token";
    private static readonly string NewRefresh = new('r', 64);

    public AcceptInviteCommandHandlerTests()
    {
        _sut = new AcceptInviteCommandHandler(
            _inviteTokens, _users, _roles, _refreshTokens,
            _hasher, _jwt, _tokenGen, _tenantSetter,
            NullLogger<AcceptInviteCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithValidToken_ReturnsLoginResult()
    {
        SetupHappyPath();

        var result = await _sut.Handle(
            new AcceptInviteCommand(RawToken, "Password1!", "Password1!"),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().BeOfType<LoginResult>();
    }

    [Fact]
    public async Task Handle_WithValidToken_SetsTenantContext()
    {
        SetupHappyPath();

        await _sut.Handle(
            new AcceptInviteCommand(RawToken, "Password1!", "Password1!"),
            CancellationToken.None);

        _tenantSetter.Received(1).SetTenantId(TenantId);
    }

    [Fact]
    public async Task Handle_WithExpiredToken_ReturnsFailure()
    {
        var invite = BuildInviteToken(expiresAt: DateTime.UtcNow.AddHours(-1));
        var hash   = ComputeHash(RawToken);
        _inviteTokens.GetByHashAsync(hash, Arg.Any<CancellationToken>()).Returns(invite);

        var result = await _sut.Handle(
            new AcceptInviteCommand(RawToken, "Password1!", "Password1!"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("expired");
    }

    [Fact]
    public async Task Handle_WhenTokenAlreadyUsed_ReturnsFailure()
    {
        var invite = BuildInviteToken();
        invite.MarkUsed();
        var hash = ComputeHash(RawToken);
        _inviteTokens.GetByHashAsync(hash, Arg.Any<CancellationToken>()).Returns(invite);

        var result = await _sut.Handle(
            new AcceptInviteCommand(RawToken, "Password1!", "Password1!"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithUnknownToken_ReturnsFailure()
    {
        _inviteTokens.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((InviteToken?)null);

        var result = await _sut.Handle(
            new AcceptInviteCommand(RawToken, "Password1!", "Password1!"),
            CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WithValidToken_MarksInviteUsed()
    {
        SetupHappyPath();
        var invite = BuildInviteToken();
        var hash   = ComputeHash(RawToken);
        _inviteTokens.GetByHashAsync(hash, Arg.Any<CancellationToken>()).Returns(invite);
        SetupUserAndJwt();

        await _sut.Handle(
            new AcceptInviteCommand(RawToken, "Password1!", "Password1!"),
            CancellationToken.None);

        invite.IsUsed.Should().BeTrue();
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private void SetupHappyPath()
    {
        var invite = BuildInviteToken();
        var hash   = ComputeHash(RawToken);
        _inviteTokens.GetByHashAsync(hash, Arg.Any<CancellationToken>()).Returns(invite);
        SetupUserAndJwt();
    }

    private void SetupUserAndJwt()
    {
        var user = User.Create(TenantId, "alice@a.com", "Alice",
            Password.FromHash("placeholder"), Guid.NewGuid());
        user.ClearDomainEvents();
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);
        _roles.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Role>() as IReadOnlyList<Role>);
        _hasher.Hash(Arg.Any<string>()).Returns(Password.FromHash("hashed_pw"));
        _jwt.Generate(Arg.Any<JwtTokenRequest>())
            .Returns(new JwtTokenResult("access_token", DateTime.UtcNow.AddHours(1)));
        _tokenGen.Generate().Returns((NewRefresh, DateTime.UtcNow.AddDays(7)));
    }

    private static InviteToken BuildInviteToken(DateTime? expiresAt = null)
        => InviteToken.Create(TenantId, UserId, ComputeHash(RawToken),
            expiresAt ?? DateTime.UtcNow.AddHours(48), createdBy: Guid.NewGuid());

    private static string ComputeHash(string raw)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
