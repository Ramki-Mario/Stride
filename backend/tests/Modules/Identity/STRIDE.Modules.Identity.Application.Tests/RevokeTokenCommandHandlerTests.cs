using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using STRIDE.Modules.Identity.Application.Abstractions;
using STRIDE.Modules.Identity.Application.Commands.RevokeToken;

namespace STRIDE.Modules.Identity.Application.Tests;

public sealed class RevokeTokenCommandHandlerTests
{
    private readonly IRefreshTokenRepository _refreshTokens = Substitute.For<IRefreshTokenRepository>();
    private readonly RevokeTokenCommandHandler _sut;

    private static readonly Guid   TenantId = Guid.NewGuid();
    private static readonly Guid   UserId   = Guid.NewGuid();
    private static readonly string RawToken = new('r', 64);

    public RevokeTokenCommandHandlerTests()
    {
        _sut = new RevokeTokenCommandHandler(
            _refreshTokens,
            NullLogger<RevokeTokenCommandHandler>.Instance);
    }

    [Fact]
    public async Task Handle_WithActiveToken_RevokesAndReturnsSuccess()
    {
        var token = BuildActiveToken();
        _refreshTokens.GetByTokenAsync(RawToken, Arg.Any<CancellationToken>()).Returns(token);

        var result = await _sut.Handle(new RevokeTokenCommand(RawToken), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        token.RevokedAt.Should().NotBeNull();
        await _refreshTokens.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithAlreadyRevokedToken_ReturnsSuccessWithoutSaving()
    {
        var token = BuildActiveToken();
        token.Revoke(DateTime.UtcNow.AddMinutes(-5));
        _refreshTokens.GetByTokenAsync(RawToken, Arg.Any<CancellationToken>()).Returns(token);

        var result = await _sut.Handle(new RevokeTokenCommand(RawToken), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        await _refreshTokens.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WhenTokenNotFound_ReturnsFailure()
    {
        _refreshTokens.GetByTokenAsync(RawToken, Arg.Any<CancellationToken>())
            .Returns((Domain.Entities.RefreshToken?)null);

        var result = await _sut.Handle(new RevokeTokenCommand(RawToken), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    // ── Helper ────────────────────────────────────────────────────────────────

    private static Domain.Entities.RefreshToken BuildActiveToken()
        => Domain.Entities.RefreshToken.Create(
            TenantId, UserId, RawToken,
            expiresAt: DateTime.UtcNow.AddDays(7),
            createdBy: UserId);
}
