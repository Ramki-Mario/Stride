using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Enums;
using STRIDE.Modules.KitOps.Domain.Events;
using STRIDE.Modules.KitOps.Domain.Exceptions;
using STRIDE.Modules.KitOps.Domain.Tests.Builders;

namespace STRIDE.Modules.KitOps.Domain.Tests;

public sealed class KitCheckoutTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidInput_ReturnsActiveCheckout()
    {
        var checkout = new KitCheckoutBuilder().Build();

        checkout.Status.Should().Be(KitCheckoutStatus.Active);
        checkout.IsDeleted.Should().BeFalse();
        checkout.ReturnedAt.Should().BeNull();
    }

    [Fact]
    public void Create_ValidInput_RaisesKitCheckedOutEvent()
    {
        var checkout = new KitCheckoutBuilder().Build();

        checkout.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<KitCheckedOutEvent>();
    }

    [Fact]
    public void Create_SetsAllCoreProperties()
    {
        var tenantId = Guid.NewGuid();
        var kitItemId = Guid.NewGuid();
        var userId = Guid.NewGuid();
        var expectedReturn = DateTime.UtcNow.AddDays(5);

        var checkout = KitCheckout.Create(new NewKitCheckout(
            tenantId, kitItemId, userId, expectedReturn, "Handle with care"));

        checkout.TenantId.Should().Be(tenantId);
        checkout.KitItemId.Should().Be(kitItemId);
        checkout.CheckedOutByUserId.Should().Be(userId);
        checkout.ExpectedReturnAt.Should().Be(expectedReturn);
        checkout.Notes.Should().Be("Handle with care");
    }

    [Fact]
    public void Create_PastExpectedReturn_ThrowsKitOpsDomainException()
    {
        var act = () => new KitCheckoutBuilder()
            .WithExpectedReturnAt(DateTime.UtcNow.AddDays(-1))
            .Build();

        act.Should().Throw<KitOpsDomainException>().WithMessage("*future*");
    }

    [Fact]
    public void Create_NotesTooLong_ThrowsKitOpsDomainException()
    {
        var act = () => new KitCheckoutBuilder()
            .WithNotes(new string('x', KitCheckout.NotesMaxLength + 1))
            .Build();

        act.Should().Throw<KitOpsDomainException>().WithMessage($"*{KitCheckout.NotesMaxLength}*");
    }

    [Fact]
    public void Create_NullNotes_IsAccepted()
    {
        var checkout = new KitCheckoutBuilder().WithNotes(null).Build();

        checkout.Notes.Should().BeNull();
    }

    [Fact]
    public void Create_TrimsNotes()
    {
        var checkout = new KitCheckoutBuilder().WithNotes("  fragile  ").Build();

        checkout.Notes.Should().Be("fragile");
    }

    // ── Return ────────────────────────────────────────────────────────────────

    [Fact]
    public void Return_ActiveCheckout_SetsStatusToReturned()
    {
        var checkout = new KitCheckoutBuilder().Build();
        var returnedAt = DateTime.UtcNow;

        checkout.Return(returnedAt, Guid.NewGuid());

        checkout.Status.Should().Be(KitCheckoutStatus.Returned);
        checkout.ReturnedAt.Should().Be(returnedAt);
    }

    [Fact]
    public void Return_ActiveCheckout_RaisesKitReturnedEvent()
    {
        var checkout = new KitCheckoutBuilder().Build();
        checkout.ClearDomainEvents();

        checkout.Return(DateTime.UtcNow, Guid.NewGuid());

        checkout.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<KitReturnedEvent>();
    }

    [Fact]
    public void Return_SetsReturnedByUserId()
    {
        var checkout = new KitCheckoutBuilder().Build();
        var userId = Guid.NewGuid();

        checkout.Return(DateTime.UtcNow, userId);

        checkout.ReturnedByUserId.Should().Be(userId);
    }

    [Fact]
    public void Return_AlreadyReturned_ThrowsKitOpsDomainException()
    {
        var checkout = new KitCheckoutBuilder().Build();
        checkout.Return(DateTime.UtcNow, Guid.NewGuid());

        var act = () => checkout.Return(DateTime.UtcNow, Guid.NewGuid());

        act.Should().Throw<KitOpsDomainException>().WithMessage("*already been returned*");
    }

    // ── MarkOverdue ───────────────────────────────────────────────────────────

    [Fact]
    public void MarkOverdue_ActiveCheckout_SetsStatusToOverdue()
    {
        var checkout = new KitCheckoutBuilder().Build();

        checkout.MarkOverdue();

        checkout.Status.Should().Be(KitCheckoutStatus.Overdue);
    }

    [Fact]
    public void MarkOverdue_ReturnedCheckout_ThrowsKitOpsDomainException()
    {
        var checkout = new KitCheckoutBuilder().Build();
        checkout.Return(DateTime.UtcNow, Guid.NewGuid());

        var act = () => checkout.MarkOverdue();

        act.Should().Throw<KitOpsDomainException>().WithMessage("*active*");
    }
}
