using FluentAssertions;
using NSubstitute;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Queries.ValidateInviteToken;
using STRIDE.Modules.Identity.Domain.Entities;
using STRIDE.Modules.Identity.Domain.ValueObjects;

namespace STRIDE.Modules.Identity.Application.Tests;

public sealed class ValidateInviteTokenQueryHandlerTests
{
    private readonly IInviteTokenRepository _inviteTokens = Substitute.For<IInviteTokenRepository>();
    private readonly IUserRepository        _users        = Substitute.For<IUserRepository>();
    private readonly ITenantContextSetter   _tenantSetter = Substitute.For<ITenantContextSetter>();

    private readonly ValidateInviteTokenQueryHandler _sut;

    private static readonly Guid   TenantId = Guid.NewGuid();
    private static readonly Guid   UserId   = Guid.NewGuid();
    private static readonly string RawToken = "test_token_value";

    public ValidateInviteTokenQueryHandlerTests()
    {
        _sut = new ValidateInviteTokenQueryHandler(_inviteTokens, _users, _tenantSetter);
    }

    [Fact]
    public async Task Handle_WithValidToken_ReturnsEmailAndDisplayName()
    {
        var invite = BuildInviteToken();
        var user   = BuildUser();
        _inviteTokens.GetByHashAsync(ComputeHash(RawToken), Arg.Any<CancellationToken>()).Returns(invite);
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        var result = await _sut.Handle(new ValidateInviteTokenQuery(RawToken), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Email.Should().Be(user.Email);
        result.Value.DisplayName.Should().Be(user.DisplayName);
    }

    [Fact]
    public async Task Handle_WithValidToken_SetsTenantContext()
    {
        var invite = BuildInviteToken();
        var user   = BuildUser();
        _inviteTokens.GetByHashAsync(ComputeHash(RawToken), Arg.Any<CancellationToken>()).Returns(invite);
        _users.GetByIdAsync(UserId, Arg.Any<CancellationToken>()).Returns(user);

        await _sut.Handle(new ValidateInviteTokenQuery(RawToken), CancellationToken.None);

        _tenantSetter.Received(1).SetTenantId(TenantId);
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ReturnsFailure()
    {
        _inviteTokens.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>())
            .Returns((InviteToken?)null);

        var result = await _sut.Handle(new ValidateInviteTokenQuery(RawToken), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
    }

    [Fact]
    public async Task Handle_WhenTokenExpired_ReturnsFailure()
    {
        var expired = BuildInviteToken(expiresAt: DateTime.UtcNow.AddHours(-1));
        _inviteTokens.GetByHashAsync(Arg.Any<string>(), Arg.Any<CancellationToken>()).Returns(expired);

        var result = await _sut.Handle(new ValidateInviteTokenQuery(RawToken), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("expired");
    }

    // ── helpers ───────────────────────────────────────────────────────────────

    private static InviteToken BuildInviteToken(DateTime? expiresAt = null)
        => InviteToken.Create(TenantId, UserId, ComputeHash(RawToken),
            expiresAt ?? DateTime.UtcNow.AddHours(48), createdBy: Guid.NewGuid());

    private static User BuildUser()
    {
        var user = User.Create(TenantId, "alice@stride.io", "Alice Smith",
            Password.FromHash("placeholder"), Guid.NewGuid());
        user.ClearDomainEvents();
        return user;
    }

    private static string ComputeHash(string raw)
    {
        var bytes = System.Security.Cryptography.SHA256.HashData(
            System.Text.Encoding.UTF8.GetBytes(raw));
        return Convert.ToHexString(bytes).ToLowerInvariant();
    }
}
