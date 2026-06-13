namespace STRIDE.Modules.Workflows.Application.DTOs;

/// <summary>
/// Redacted invoice snapshot for the unauthenticated client-facing job view (US-179).
/// Exposes billing reference and totals only — no internal notes, dates, or email.
/// </summary>
public sealed record PublicInvoiceDto(
    string  InvoiceNumber,
    string  StatusLabel,
    string  Currency,
    decimal TotalAmount,
    DateOnly DueDate,
    IReadOnlyList<PublicInvoiceLineItemDto> LineItems);

public sealed record PublicInvoiceLineItemDto(
    string  Description,
    decimal Subtotal);
