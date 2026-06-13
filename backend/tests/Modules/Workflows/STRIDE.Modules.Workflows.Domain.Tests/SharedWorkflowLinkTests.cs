using FluentAssertions;
using STRIDE.Modules.Workflows.Domain.Entities;
using STRIDE.Modules.Workflows.Domain.Exceptions;

namespace STRIDE.Modules.Workflows.Domain.Tests;

public sealed class SharedWorkflowLinkTests
{
    private static SharedWorkflowLink CreateLink(int expiryDays = SharedWorkflowLink.DefaultExpiryDays) =>
        SharedWorkflowLink.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), expiryDays);

    // ── Creation ──────────────────────────────────────────────────────────────

    [Fact]
    public void Create_SetsExpiryThirtyDaysOutByDefault()
    {
        var before = DateTime.UtcNow;

        var link = CreateLink();

        link.ExpiresAt.Should().BeCloseTo(before.AddDays(30), TimeSpan.FromMinutes(1));
        link.RevokedAt.Should().BeNull();
        link.ViewCount.Should().Be(0);
        link.IsRevoked.Should().BeFalse();
    }

    [Fact]
    public void Create_GeneratesUrlSafeToken()
    {
        var link = CreateLink();

        link.Token.Should().NotBeNullOrWhiteSpace();
        // base64url: no '+', '/', or '=' padding — safe inside a URL path segment.
        link.Token.Should().NotContainAny("+", "/", "=");
        link.Token.Length.Should().BeGreaterThan(40); // 32 random bytes → 43 base64url chars
    }

    [Fact]
    public void Create_GeneratesUniqueUnpredictableTokens()
    {
        var tokens = Enumerable.Range(0, 100).Select(_ => CreateLink().Token).ToList();

        tokens.Should().OnlyHaveUniqueItems();
    }

    [Fact]
    public void Create_WithEmptyInstanceId_Throws()
    {
        var act = () => SharedWorkflowLink.Create(Guid.Empty, Guid.NewGuid(), Guid.NewGuid());

        act.Should().Throw<WorkflowDomainException>().WithMessage("*WorkflowInstanceId*");
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-5)]
    public void Create_WithNonPositiveExpiry_Throws(int days)
    {
        var act = () => SharedWorkflowLink.Create(Guid.NewGuid(), Guid.NewGuid(), Guid.NewGuid(), days);

        act.Should().Throw<WorkflowDomainException>().WithMessage("*expiry*");
    }

    // ── Validity ──────────────────────────────────────────────────────────────

    [Fact]
    public void IsValid_FreshLink_IsTrue()
    {
        var link = CreateLink();

        link.IsValid(DateTime.UtcNow).Should().BeTrue();
    }

    [Fact]
    public void IsValid_AfterExpiry_IsFalse()
    {
        var link = CreateLink(expiryDays: 1);

        link.IsExpired(DateTime.UtcNow.AddDays(2)).Should().BeTrue();
        link.IsValid(DateTime.UtcNow.AddDays(2)).Should().BeFalse();
    }

    [Fact]
    public void IsValid_AtExactExpiryInstant_IsFalse()
    {
        var link = CreateLink();

        // Boundary: expiry is inclusive — at ExpiresAt the link is already gone.
        link.IsExpired(link.ExpiresAt).Should().BeTrue();
    }

    // ── Revocation ────────────────────────────────────────────────────────────

    [Fact]
    public void Revoke_MarksRevokedAndInvalidatesImmediately()
    {
        var link = CreateLink();

        link.Revoke();

        link.IsRevoked.Should().BeTrue();
        link.RevokedAt.Should().NotBeNull();
        link.IsValid(DateTime.UtcNow).Should().BeFalse();
    }

    [Fact]
    public void Revoke_IsIdempotent()
    {
        var link = CreateLink();

        link.Revoke();
        var firstRevokedAt = link.RevokedAt;
        link.Revoke();

        link.RevokedAt.Should().Be(firstRevokedAt);
    }

    // ── View counting ─────────────────────────────────────────────────────────

    [Fact]
    public void IncrementViewCount_IncrementsByOne()
    {
        var link = CreateLink();

        link.IncrementViewCount();
        link.IncrementViewCount();

        link.ViewCount.Should().Be(2);
    }
}
