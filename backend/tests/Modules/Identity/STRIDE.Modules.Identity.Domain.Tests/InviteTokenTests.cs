using FluentAssertions;
using STRIDE.Modules.Identity.Domain.Entities;

namespace STRIDE.Modules.Identity.Domain.Tests;

public sealed class InviteTokenTests
{
    private static readonly Guid TenantId  = Guid.NewGuid();
    private static readonly Guid UserId    = Guid.NewGuid();
    private static readonly Guid CreatedBy = Guid.NewGuid();
    private const string TokenHash = "abc123";

    [Fact]
    public void Create_SetsAllProperties()
    {
        var expiry = DateTime.UtcNow.AddHours(48);
        var token  = InviteToken.Create(TenantId, UserId, TokenHash, expiry, CreatedBy);

        token.TenantId.Should().Be(TenantId);
        token.UserId.Should().Be(UserId);
        token.TokenHash.Should().Be(TokenHash);
        token.ExpiresAt.Should().BeCloseTo(expiry, TimeSpan.FromSeconds(1));
        token.IsUsed.Should().BeFalse();
        token.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenUnusedAndNotExpired_ReturnsTrue()
    {
        var token = InviteToken.Create(TenantId, UserId, TokenHash,
            DateTime.UtcNow.AddHours(48), CreatedBy);

        token.IsValid(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsValid_WhenExpired_ReturnsFalse()
    {
        var token = InviteToken.Create(TenantId, UserId, TokenHash,
            DateTime.UtcNow.AddHours(-1), CreatedBy);

        token.IsValid(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void IsValid_WhenUsed_ReturnsFalse()
    {
        var token = InviteToken.Create(TenantId, UserId, TokenHash,
            DateTime.UtcNow.AddHours(48), CreatedBy);
        token.MarkUsed();

        token.IsValid(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void MarkUsed_SetsIsUsedTrue()
    {
        var token = InviteToken.Create(TenantId, UserId, TokenHash,
            DateTime.UtcNow.AddHours(48), CreatedBy);

        token.MarkUsed();

        token.IsUsed.Should().BeTrue();
    }

    [Fact]
    public void IsValid_AtExactExpiry_ReturnsTrue()
    {
        var expiry = DateTime.UtcNow.AddSeconds(1);
        var token  = InviteToken.Create(TenantId, UserId, TokenHash, expiry, CreatedBy);

        token.IsValid(expiry).Should().BeTrue();
    }
}
