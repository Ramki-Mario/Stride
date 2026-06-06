using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Abstractions;
using PasswordVO = STRIDE.Modules.Identity.Domain.ValueObjects.Password;
using STRIDE.Modules.Identity.Application.Commands.LoginUser;
using STRIDE.Modules.Identity.Domain.Entities;
using STRIDE.Modules.Identity.Domain.ValueObjects;

namespace STRIDE.Modules.Identity.Application.Tests;

public sealed class LoginCommandHandlerTests
{
    // ── Shared substitutes ────────────────────────────────────────────────────

    private readonly ITenantResolver      _tenantResolver = Substitute.For<ITenantResolver>();
    private readonly ITenantContextSetter _tenantSetter   = Substitute.For<ITenantContextSetter>();
    private readonly IUserRepository      _users          = Substitute.For<IUserRepository>();
    private readonly IRoleRepository      _roles          = Substitute.For<IRoleRepository>();
    private readonly IPasswordHasher      _hasher         = Substitute.For<IPasswordHasher>();
    private readonly IJwtTokenService     _jwt            = Substitute.For<IJwtTokenService>();

    private readonly LoginCommandHandler _sut;

    private static readonly Guid   TenantId  = Guid.NewGuid();
    private static readonly string Email        = "alice@company.com";
    private static readonly string PlainPassword = "S3cur3P@ss!";
    private static readonly string TokenStr     = "jwt_token_value";

    public LoginCommandHandlerTests()
    {
        _sut = new LoginCommandHandler(
            _tenantResolver, _tenantSetter, _users, _roles,
            _hasher, _jwt, NullLogger<LoginCommandHandler>.Instance);
    }

    // ── Success path ──────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WithValidCredentials_ReturnsSuccessWithToken()
    {
        // Arrange
        var user = BuildActiveUser();
        SetupHappyPath(user);

        var command = new LoginCommand(Email, PlainPassword);

        // Act
        var result = await _sut.Handle(command, CancellationToken.None);

        // Assert
        result.IsSuccess.Should().BeTrue();
        result.Value.AccessToken.Should().Be(TokenStr);
        result.Value.Email.Should().Be(Email);
        result.Value.TenantId.Should().Be(TenantId);
    }

    [Fact]
    public async Task Handle_WithValidCredentials_SetsTenantContextBeforeQueryingUser()
    {
        // Arrange
        var user = BuildActiveUser();
        SetupHappyPath(user);

        // Act
        await _sut.Handle(new LoginCommand(Email, PlainPassword), CancellationToken.None);

        // Assert — tenant must be set before user lookup (ordering guarantee)
        Received.InOrder(() =>
        {
            _tenantSetter.SetTenantId(TenantId);
            _ = _users.GetByNormalizedEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>());
        });
    }

    // ── Failure paths ─────────────────────────────────────────────────────────

    [Fact]
    public async Task Handle_WhenTenantNotFound_ReturnsFailure()
    {
        // Arrange
        _tenantResolver.ResolveFromEmailAsync(Email, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        // Act
        var result = await _sut.Handle(new LoginCommand(Email, PlainPassword), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Handle_WhenUserNotFound_ReturnsFailure()
    {
        // Arrange
        _tenantResolver.ResolveFromEmailAsync(Email, Arg.Any<CancellationToken>())
            .Returns(TenantId);
        _users.GetByNormalizedEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((User?)null);

        // Act
        var result = await _sut.Handle(new LoginCommand(Email, PlainPassword), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Handle_WhenUserIsInactive_ReturnsFailure()
    {
        // Arrange
        var user = BuildActiveUser();
        user.Deactivate();

        _tenantResolver.ResolveFromEmailAsync(Email, Arg.Any<CancellationToken>())
            .Returns(TenantId);
        _users.GetByNormalizedEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);

        // Act
        var result = await _sut.Handle(new LoginCommand(Email, PlainPassword), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Handle_WhenPasswordIsWrong_ReturnsFailure()
    {
        // Arrange
        var user = BuildActiveUser();

        _tenantResolver.ResolveFromEmailAsync(Email, Arg.Any<CancellationToken>())
            .Returns(TenantId);
        _users.GetByNormalizedEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);
        _hasher.Verify(PlainPassword, Arg.Any<Password>()).Returns(false); // wrong password

        // Act
        var result = await _sut.Handle(new LoginCommand(Email, PlainPassword), CancellationToken.None);

        // Assert
        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Invalid email or password");
    }

    [Fact]
    public async Task Handle_OnFailure_DoesNotCallJwtTokenService()
    {
        // Arrange — tenant not found
        _tenantResolver.ResolveFromEmailAsync(Email, Arg.Any<CancellationToken>())
            .Returns((Guid?)null);

        // Act
        await _sut.Handle(new LoginCommand(Email, PlainPassword), CancellationToken.None);

        // Assert
        _jwt.DidNotReceive().Generate(Arg.Any<JwtTokenRequest>());
    }

    // ── Helpers ───────────────────────────────────────────────────────────────

    private User BuildActiveUser()
    {
        var user = User.Create(TenantId, Email, "Alice", PasswordVO.FromHash("hash"), Guid.NewGuid());
        user.ClearDomainEvents();
        return user;
    }

    private void SetupHappyPath(User user)
    {
        _tenantResolver.ResolveFromEmailAsync(Email, Arg.Any<CancellationToken>())
            .Returns(TenantId);
        _users.GetByNormalizedEmailAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns(user);
        _hasher.Verify(PlainPassword, Arg.Any<Password>()).Returns(true);
        _roles.GetAllAsync(Arg.Any<CancellationToken>())
            .Returns(Array.Empty<Role>() as IReadOnlyList<Role>);
        _jwt.Generate(Arg.Any<JwtTokenRequest>())
            .Returns(new JwtTokenResult(TokenStr, DateTime.UtcNow.AddHours(1)));
    }
}
