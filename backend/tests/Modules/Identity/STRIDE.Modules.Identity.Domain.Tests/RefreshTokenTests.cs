using FluentAssertions;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Domain.Tests;

public sealed class RefreshTokenTests
{
    private static readonly Guid   TenantId  = Guid.NewGuid();
    private static readonly Guid   UserId    = Guid.NewGuid();
    private static readonly Guid   CreatedBy = Guid.NewGuid();
    private static readonly string RawToken  = new('a', 64);
    private static readonly DateTime Future  = DateTime.UtcNow.AddDays(7);
    private static readonly DateTime Past    = DateTime.UtcNow.AddDays(-1);
    private static readonly DateTime Now     = DateTime.UtcNow;

    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsAllExpectedProperties()
    {
        var rt = RefreshToken.Create(TenantId, UserId, RawToken, Future, CreatedBy);

        rt.Id.Should().NotBeEmpty();
        rt.TenantId.Should().Be(TenantId);
        rt.UserId.Should().Be(UserId);
        rt.Token.Should().Be(RawToken);
        rt.ExpiresAt.Should().Be(Future);
        rt.RevokedAt.Should().BeNull();
        rt.ReplacedByToken.Should().BeNull();
        rt.IsDeleted.Should().BeFalse();
        rt.CreatedBy.Should().Be(CreatedBy);
    }

    // ── IsActive ──────────────────────────────────────────────────────────────

    [Fact]
    public void IsActive_ReturnsTrueForFreshToken()
    {
        var rt = RefreshToken.Create(TenantId, UserId, RawToken, Future, CreatedBy);

        rt.IsActive(Now).Should().BeTrue();
    }

    [Fact]
    public void IsActive_ReturnsFalseWhenExpired()
    {
        var rt = RefreshToken.Create(TenantId, UserId, RawToken, Past, CreatedBy);

        rt.IsActive(Now).Should().BeFalse();
    }

    [Fact]
    public void IsActive_ReturnsFalseWhenRevoked()
    {
        var rt = RefreshToken.Create(TenantId, UserId, RawToken, Future, CreatedBy);
        rt.Revoke(Now);

        rt.IsActive(Now).Should().BeFalse();
    }

    // ── Revoke ────────────────────────────────────────────────────────────────

    [Fact]
    public void Revoke_SetsRevokedAt()
    {
        var rt = RefreshToken.Create(TenantId, UserId, RawToken, Future, CreatedBy);
        rt.Revoke(Now);

        rt.RevokedAt.Should().Be(Now);
    }

    [Fact]
    public void Revoke_WithReplacedByToken_StoresRotatedToken()
    {
        var newToken = new string('b', 64);
        var rt = RefreshToken.Create(TenantId, UserId, RawToken, Future, CreatedBy);
        rt.Revoke(Now, replacedByToken: newToken);

        rt.ReplacedByToken.Should().Be(newToken);
    }

    [Fact]
    public void Revoke_WithoutReplacedByToken_LeavesReplacedByTokenNull()
    {
        var rt = RefreshToken.Create(TenantId, UserId, RawToken, Future, CreatedBy);
        rt.Revoke(Now);

        rt.ReplacedByToken.Should().BeNull();
    }
}
