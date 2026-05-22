using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Invoicing.Domain.ValueObjects;

namespace STRIDE.Modules.Invoicing.Domain.Entities;

/// <summary>
/// A single line on an Invoice (description + unit price × quantity).
/// Owned by the Invoice aggregate root.
/// </summary>
public sealed class InvoiceLineItem : BaseEntity<Guid>
{
    public Guid    InvoiceId   { get; private set; }
    public string  Description { get; private set; } = string.Empty;
    public decimal UnitPrice   { get; private set; }
    public int     Quantity    { get; private set; }
    public string  Currency    { get; private set; } = "USD";

    /// <summary>Computed subtotal = UnitPrice × Quantity.</summary>
    public decimal Subtotal => Math.Round(UnitPrice * Quantity, 2);

    // EF Core constructor
    private InvoiceLineItem() { }

    internal static InvoiceLineItem Create(
        Guid invoiceId,
        string description,
        decimal unitPrice,
        int quantity,
        string currency)
    {
        if (string.IsNullOrWhiteSpace(description))
            throw new ArgumentException("Line item description is required.", nameof(description));
        if (unitPrice < 0)
            throw new ArgumentOutOfRangeException(nameof(unitPrice), "Unit price cannot be negative.");
        if (quantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(quantity), "Quantity must be at least 1.");

        return new InvoiceLineItem
        {
            Id          = Guid.NewGuid(),
            InvoiceId   = invoiceId,
            Description = description.Trim(),
            UnitPrice   = Math.Round(unitPrice, 2),
            Quantity    = quantity,
            Currency    = currency.Trim().ToUpperInvariant(),
        };
    }
}
