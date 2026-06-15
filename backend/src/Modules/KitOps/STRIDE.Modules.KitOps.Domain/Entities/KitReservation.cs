using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Enums;
using STRIDE.Modules.KitOps.Domain.Events;
using STRIDE.Modules.KitOps.Domain.Exceptions;

namespace STRIDE.Modules.KitOps.Domain.Entities;

public sealed record NewKitReservation(
    Guid    TenantId,
    Guid    KitItemId,
    Guid    RequestedByUserId,
    string? Notes);

/// <summary>
/// A pending request to borrow a <see cref="KitItem"/> that is currently out.
/// Status transitions: Pending → Fulfilled | Pending → Cancelled.
/// </summary>
public sealed class KitReservation : AuditableEntity
{
    public const int NotesMaxLength = 500;

    public Guid                 KitItemId         { get; private set; }
    public Guid                 RequestedByUserId { get; private set; }
    public KitReservationStatus Status            { get; private set; }
    public string?              Notes             { get; private set; }

    private KitReservation() { }

    public static KitReservation Create(NewKitReservation input)
    {
        ValidateNotes(input.Notes);

        var now = DateTime.UtcNow;
        var reservation = new KitReservation
        {
            Id                = Guid.NewGuid(),
            TenantId          = input.TenantId,
            KitItemId         = input.KitItemId,
            RequestedByUserId = input.RequestedByUserId,
            Status            = KitReservationStatus.Pending,
            Notes             = input.Notes?.Trim(),
            CreatedAt         = now,
            UpdatedAt         = now,
            CreatedBy         = input.RequestedByUserId,
        };

        reservation.RaiseDomainEvent(new KitReservationCreatedEvent(
            reservation.Id,
            reservation.KitItemId,
            reservation.TenantId,
            reservation.RequestedByUserId));

        return reservation;
    }

    public void Fulfill(Guid fulfilledByUserId)
    {
        if (Status != KitReservationStatus.Pending)
            throw new KitOpsDomainException("Only pending reservations can be fulfilled.");

        if (IsDeleted)
            throw new KitOpsDomainException("A deleted reservation cannot be fulfilled.");

        Status    = KitReservationStatus.Fulfilled;
        UpdatedAt = DateTime.UtcNow;
    }

    public void Cancel(Guid cancelledByUserId)
    {
        if (Status != KitReservationStatus.Pending)
            throw new KitOpsDomainException("Only pending reservations can be cancelled.");

        if (IsDeleted)
            throw new KitOpsDomainException("A deleted reservation cannot be cancelled.");

        Status    = KitReservationStatus.Cancelled;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new KitReservationCancelledEvent(
            Id, KitItemId, TenantId, cancelledByUserId));
    }

    private static void ValidateNotes(string? notes)
    {
        if (notes is not null && notes.Trim().Length > NotesMaxLength)
            throw new KitOpsDomainException($"Notes cannot exceed {NotesMaxLength} characters.");
    }
}
