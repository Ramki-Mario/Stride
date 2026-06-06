using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.Modules.Invoicing.Domain.Enums;
using STRIDE.Modules.Invoicing.Domain.Events;
using STRIDE.Modules.Invoicing.Domain.Exceptions;

namespace STRIDE.Modules.Invoicing.Domain.Entities;

public sealed record NewInvoice(
    Guid     TenantId,
    string   InvoiceNumber,
    string   ClientName,
    string   ClientEmail,
    string   Currency,
    DateOnly DueDate,
    Guid     CreatedBy,
    string?  Notes = null);

/// <summary>
/// Invoice aggregate root.
///
/// State machine:
///   Draft → Sent  (via Send)
///   Sent  → Paid  (via MarkPaid)
///   Draft → Void  (via Void)
///   Sent  → Void  (via Void)
/// </summary>
public sealed class Invoice : AuditableEntity
{
    private readonly List<InvoiceLineItem> _lineItems = new();

    public string        InvoiceNumber { get; private set; } = string.Empty;
    public string        ClientName    { get; private set; } = string.Empty;
    public string        ClientEmail   { get; private set; } = string.Empty;
    public string        Currency      { get; private set; } = "USD";
    public InvoiceStatus Status        { get; private set; }
    public DateOnly      DueDate       { get; private set; }
    public string?       Notes         { get; private set; }
    public DateTime?     SentAt        { get; private set; }
    public DateTime?     PaidAt        { get; private set; }

    public IReadOnlyList<InvoiceLineItem> LineItems => _lineItems.AsReadOnly();

    /// <summary>Sum of all line item subtotals.</summary>
    public decimal TotalAmount => _lineItems.Sum(l => l.Subtotal);

    // EF Core constructor
    private Invoice() { }

    // ── Factory ───────────────────────────────────────────────────────────────

    public static Invoice Generate(NewInvoice data)
    {
        if (string.IsNullOrWhiteSpace(data.InvoiceNumber))
            throw new InvoiceDomainException("Invoice number is required.");
        if (string.IsNullOrWhiteSpace(data.ClientName))
            throw new InvoiceDomainException("Client name is required.");
        if (string.IsNullOrWhiteSpace(data.ClientEmail))
            throw new InvoiceDomainException("Client email is required.");
        if (data.DueDate < DateOnly.FromDateTime(DateTime.UtcNow))
            throw new InvoiceDomainException("Due date cannot be in the past.");

        var invoice = new Invoice
        {
            Id            = Guid.NewGuid(),
            TenantId      = data.TenantId,
            InvoiceNumber = data.InvoiceNumber.Trim(),
            ClientName    = data.ClientName.Trim(),
            ClientEmail   = data.ClientEmail.Trim().ToLowerInvariant(),
            Currency      = data.Currency.Trim().ToUpperInvariant(),
            Status        = InvoiceStatus.Draft,
            DueDate       = data.DueDate,
            Notes         = data.Notes?.Trim(),
            CreatedAt     = DateTime.UtcNow,
            UpdatedAt     = DateTime.UtcNow,
            CreatedBy     = data.CreatedBy,
        };

        invoice.RaiseDomainEvent(new InvoiceGeneratedEvent(
            invoice.Id, data.TenantId, invoice.InvoiceNumber, data.CreatedBy));

        return invoice;
    }

    // ── Line item management ──────────────────────────────────────────────────

    public void AddLineItem(string description, decimal unitPrice, int quantity)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvoiceDomainException("Line items can only be added to Draft invoices.");

        _lineItems.Add(InvoiceLineItem.Create(Id, description, unitPrice, quantity, Currency));
        UpdatedAt = DateTime.UtcNow;
    }

    // ── State transitions ─────────────────────────────────────────────────────

    public void Send(Guid sentBy)
    {
        if (Status != InvoiceStatus.Draft)
            throw new InvoiceDomainException("Only Draft invoices can be sent.");
        if (_lineItems.Count == 0)
            throw new InvoiceDomainException("Cannot send an invoice with no line items.");

        Status    = InvoiceStatus.Sent;
        SentAt    = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new InvoiceSentEvent(Id, TenantId, InvoiceNumber, sentBy));
    }

    public void MarkPaid(Guid markedBy)
    {
        if (Status != InvoiceStatus.Sent)
            throw new InvoiceDomainException("Only Sent invoices can be marked as paid.");

        Status    = InvoiceStatus.Paid;
        PaidAt    = DateTime.UtcNow;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new InvoicePaidEvent(
            Id, TenantId, InvoiceNumber, TotalAmount, Currency, markedBy));
    }

    public void Void(Guid voidedBy)
    {
        if (Status is InvoiceStatus.Paid or InvoiceStatus.Void)
            throw new InvoiceDomainException($"Cannot void a {Status} invoice.");

        Status    = InvoiceStatus.Void;
        UpdatedAt = DateTime.UtcNow;

        RaiseDomainEvent(new InvoiceVoidedEvent(Id, TenantId, InvoiceNumber, voidedBy));
    }
}
