using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Enums;
using STRIDE.Modules.KitOps.Domain.Events;
using STRIDE.Modules.KitOps.Domain.Exceptions;
using STRIDE.Modules.KitOps.Domain.Tests.Builders;

namespace STRIDE.Modules.KitOps.Domain.Tests;

public sealed class KitReservationTests
{
    // ── Create ────────────────────────────────────────────────────────────────

    [Fact]
    public void Create_ValidInput_ReturnsPendingReservation()
    {
        var reservation = new KitReservationBuilder().Build();

        reservation.Status.Should().Be(KitReservationStatus.Pending);
        reservation.IsDeleted.Should().BeFalse();
    }

    [Fact]
    public void Create_ValidInput_RaisesKitReservationCreatedEvent()
    {
        var reservation = new KitReservationBuilder().Build();

        reservation.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<KitReservationCreatedEvent>();
    }

    [Fact]
    public void Create_SetsAllCoreProperties()
    {
        var tenantId = Guid.NewGuid();
        var kitItemId = Guid.NewGuid();
        var userId = Guid.NewGuid();

        var reservation = KitReservation.Create(new NewKitReservation(
            tenantId, kitItemId, userId, "Needed for site visit"));

        reservation.TenantId.Should().Be(tenantId);
        reservation.KitItemId.Should().Be(kitItemId);
        reservation.RequestedByUserId.Should().Be(userId);
        reservation.Notes.Should().Be("Needed for site visit");
    }

    [Fact]
    public void Create_NullNotes_IsAccepted()
    {
        var reservation = new KitReservationBuilder().WithNotes(null).Build();

        reservation.Notes.Should().BeNull();
    }

    [Fact]
    public void Create_NotesTooLong_ThrowsKitOpsDomainException()
    {
        var act = () => new KitReservationBuilder()
            .WithNotes(new string('x', KitReservation.NotesMaxLength + 1))
            .Build();

        act.Should().Throw<KitOpsDomainException>().WithMessage($"*{KitReservation.NotesMaxLength}*");
    }

    [Fact]
    public void Create_TrimsNotes()
    {
        var reservation = new KitReservationBuilder().WithNotes("  site visit  ").Build();

        reservation.Notes.Should().Be("site visit");
    }

    // ── Fulfill ───────────────────────────────────────────────────────────────

    [Fact]
    public void Fulfill_PendingReservation_SetsStatusToFulfilled()
    {
        var reservation = new KitReservationBuilder().Build();

        reservation.Fulfill(Guid.NewGuid());

        reservation.Status.Should().Be(KitReservationStatus.Fulfilled);
    }

    [Fact]
    public void Fulfill_PendingReservation_RaisesKitReservationFulfilledEvent()
    {
        var reservation = new KitReservationBuilder().Build();
        reservation.ClearDomainEvents();

        reservation.Fulfill(Guid.NewGuid());

        reservation.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<KitReservationFulfilledEvent>();
    }

    [Fact]
    public void Fulfill_AlreadyFulfilled_ThrowsKitOpsDomainException()
    {
        var reservation = new KitReservationBuilder().Build();
        reservation.Fulfill(Guid.NewGuid());

        var act = () => reservation.Fulfill(Guid.NewGuid());

        act.Should().Throw<KitOpsDomainException>().WithMessage("*pending*");
    }

    // ── Cancel ────────────────────────────────────────────────────────────────

    [Fact]
    public void Cancel_PendingReservation_SetsStatusToCancelled()
    {
        var reservation = new KitReservationBuilder().Build();

        reservation.Cancel(Guid.NewGuid());

        reservation.Status.Should().Be(KitReservationStatus.Cancelled);
    }

    [Fact]
    public void Cancel_PendingReservation_RaisesKitReservationCancelledEvent()
    {
        var reservation = new KitReservationBuilder().Build();
        reservation.ClearDomainEvents();

        reservation.Cancel(Guid.NewGuid());

        reservation.DomainEvents.Should().ContainSingle()
            .Which.Should().BeOfType<KitReservationCancelledEvent>();
    }

    [Fact]
    public void Cancel_AlreadyCancelled_ThrowsKitOpsDomainException()
    {
        var reservation = new KitReservationBuilder().Build();
        reservation.Cancel(Guid.NewGuid());

        var act = () => reservation.Cancel(Guid.NewGuid());

        act.Should().Throw<KitOpsDomainException>().WithMessage("*pending*");
    }

    [Fact]
    public void Cancel_FulfilledReservation_ThrowsKitOpsDomainException()
    {
        var reservation = new KitReservationBuilder().Build();
        reservation.Fulfill(Guid.NewGuid());

        var act = () => reservation.Cancel(Guid.NewGuid());

        act.Should().Throw<KitOpsDomainException>().WithMessage("*pending*");
    }
}
