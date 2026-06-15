using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Enums;
using STRIDE.Modules.KitOps.Domain.Events;
using STRIDE.Modules.KitOps.Domain.Exceptions;

namespace STRIDE.Modules.KitOps.Domain.Entities;

public sealed record NewKitCheckout(
    Guid     TenantId,
    Guid     KitItemId,
    Guid     CheckedOutByUserId,
    DateTime ExpectedReturnAt,
    string?  Notes);

/// <summary>
/// Records a single checkout of a <see cref="KitItem"/> by a field user.
/// Status transitions: Active → Returned | Active → Overdue.
/// </summary>
public sealed class KitCheckout : AuditableEntity
{
    public const int NotesMaxLength = 500;

    public Guid              KitItemId          { get; private set; }
    public Guid              CheckedOutByUserId { get; private set; }
    public DateTime          ExpectedReturnAt   { get; private set; }
    public DateTime?         ReturnedAt         { get; private set; }
    public Guid?             ReturnedByUserId   { get; private set; }
    public KitCheckoutStatus Status             { get; private set; }
    public string?           Notes              { get; private set; }

    private KitCheckout() { }

    public static KitCheckout Create(NewKitCheckout input)
    {
        if (input.ExpectedReturnAt <= DateTime.UtcNow)
            throw new KitOpsDomainException("Expected return date must be in the future.");

        ValidateNotes(input.Notes);

        var now = DateTime.UtcNow;
        var checkout = new KitCheckout
        {
            Id                 = Guid.NewGuid(),
            TenantId           = input.TenantId,
            KitItemId          = input.KitItemId,
            CheckedOutByUserId = input.CheckedOutByUserId,
            ExpectedReturnAt   = input.ExpectedReturnAt,
            Status             = KitCheckoutStatus.Active,
            Notes              = input.Notes?.Trim(),
            CreatedAt          = now,
            UpdatedAt          = now,
            CreatedBy          = input.CheckedOutByUserId,
        };

        checkout.RaiseDomainEvent(new KitCheckedOutEvent(
            checkout.Id,
            checkout.KitItemId,
            checkout.TenantId,
            checkout.CheckedOutByUserId,
            checkout.ExpectedReturnAt));

        return checkout;
    }

    public void Return(DateTime returnedAt, Guid returnedByUserId)
    {
        if (Status == KitCheckoutStatus.Returned)
            throw new KitOpsDomainException("Kit item has already been returned.");

        if (IsDeleted)
            throw new KitOpsDomainException("A deleted checkout record cannot be updated.");

        ReturnedAt       = returnedAt;
        ReturnedByUserId = returnedByUserId;
        Status           = KitCheckoutStatus.Returned;
        UpdatedAt        = DateTime.UtcNow;

        RaiseDomainEvent(new KitReturnedEvent(
            Id, KitItemId, TenantId, returnedByUserId, returnedAt));
    }

    public void MarkOverdue()
    {
        if (Status != KitCheckoutStatus.Active)
            throw new KitOpsDomainException("Only active checkouts can be marked overdue.");

        Status    = KitCheckoutStatus.Overdue;
        UpdatedAt = DateTime.UtcNow;
    }

    private static void ValidateNotes(string? notes)
    {
        if (notes is not null && notes.Trim().Length > NotesMaxLength)
            throw new KitOpsDomainException($"Notes cannot exceed {NotesMaxLength} characters.");
    }
}
