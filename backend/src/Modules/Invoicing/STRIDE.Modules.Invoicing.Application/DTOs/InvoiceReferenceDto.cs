namespace STRIDE.Modules.Invoicing.Application.DTOs;

/// <summary>
/// Lightweight read model used by cross-module consumers that only need to know
/// whether an invoice exists for a workflow instance and what its current status is.
/// </summary>
public sealed record InvoiceReferenceDto(
    Guid   Id,
    string InvoiceNumber,
    int    Status,
    string StatusLabel);
