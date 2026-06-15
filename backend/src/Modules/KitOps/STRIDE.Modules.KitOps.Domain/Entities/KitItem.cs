using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Events;
using STRIDE.Modules.KitOps.Domain.Exceptions;

namespace STRIDE.Modules.KitOps.Domain.Entities;

public sealed record NewKitItem(
    Guid    TenantId,
    string  Name,
    string  Category,
    string? Description,
    int     TotalQuantity,
    Guid    CreatedBy);

/// <summary>
/// A physical piece of equipment that can be checked out by field personnel.
/// Aggregate root — KitCheckout and KitReservation reference it by FK only.
/// </summary>
public sealed class KitItem : AuditableEntity
{
    public const int NameMaxLength        = 150;
    public const int CategoryMaxLength    = 100;
    public const int DescriptionMaxLength = 500;

    public string  Name          { get; private set; } = string.Empty;
    public string  Category      { get; private set; } = string.Empty;
    public string? Description   { get; private set; }
    public int     TotalQuantity { get; private set; }
    public bool    IsActive      { get; private set; }

    private KitItem() { }

    public static KitItem Create(NewKitItem input)
    {
        ValidateName(input.Name);
        ValidateCategory(input.Category);
        ValidateDescription(input.Description);
        ValidateQuantity(input.TotalQuantity);

        var now = DateTime.UtcNow;
        var item = new KitItem
        {
            Id            = Guid.NewGuid(),
            TenantId      = input.TenantId,
            Name          = input.Name.Trim(),
            Category      = input.Category.Trim(),
            Description   = input.Description?.Trim(),
            TotalQuantity = input.TotalQuantity,
            IsActive      = true,
            CreatedAt     = now,
            UpdatedAt     = now,
            CreatedBy     = input.CreatedBy,
        };

        item.RaiseDomainEvent(new KitItemCreatedEvent(
            item.Id, item.TenantId, item.Name, input.CreatedBy));

        return item;
    }

    public void Update(string name, string category, string? description, int totalQuantity, Guid updatedBy)
    {
        if (IsDeleted)
            throw new KitOpsDomainException("A deleted kit item cannot be updated.");

        ValidateName(name);
        ValidateCategory(category);
        ValidateDescription(description);
        ValidateQuantity(totalQuantity);

        Name          = name.Trim();
        Category      = category.Trim();
        Description   = description?.Trim();
        TotalQuantity = totalQuantity;
        UpdatedAt     = DateTime.UtcNow;

        RaiseDomainEvent(new KitItemUpdatedEvent(Id, TenantId, Name, updatedBy));
    }

    public void Deactivate(Guid deactivatedBy)
    {
        if (IsDeleted)
            throw new KitOpsDomainException("A deleted kit item cannot be deactivated.");

        if (!IsActive)
            throw new KitOpsDomainException("Kit item is already inactive.");

        IsActive  = false;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new KitItemDeactivatedEvent(Id, TenantId, deactivatedBy));
    }

    public void Reactivate(Guid reactivatedBy)
    {
        if (IsDeleted)
            throw new KitOpsDomainException("A deleted kit item cannot be reactivated.");

        if (IsActive)
            throw new KitOpsDomainException("Kit item is already active.");

        IsActive  = true;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new KitItemReactivatedEvent(Id, TenantId, reactivatedBy));
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
            throw new KitOpsDomainException("Kit item name cannot be empty.");

        if (name.Trim().Length > NameMaxLength)
            throw new KitOpsDomainException($"Kit item name cannot exceed {NameMaxLength} characters.");
    }

    private static void ValidateCategory(string category)
    {
        if (string.IsNullOrWhiteSpace(category))
            throw new KitOpsDomainException("Kit item category cannot be empty.");

        if (category.Trim().Length > CategoryMaxLength)
            throw new KitOpsDomainException($"Kit item category cannot exceed {CategoryMaxLength} characters.");
    }

    private static void ValidateDescription(string? description)
    {
        if (description is not null && description.Trim().Length > DescriptionMaxLength)
            throw new KitOpsDomainException($"Kit item description cannot exceed {DescriptionMaxLength} characters.");
    }

    private static void ValidateQuantity(int quantity)
    {
        if (quantity <= 0)
            throw new KitOpsDomainException("Total quantity must be greater than zero.");
    }
}
