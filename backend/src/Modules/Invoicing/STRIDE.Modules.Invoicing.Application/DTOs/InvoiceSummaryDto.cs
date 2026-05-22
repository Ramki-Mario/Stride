namespace STRIDE.Modules.Invoicing.Application.DTOs;

/// <summary>List-view DTO returned by GetInvoicesQuery.</summary>
public sealed record InvoiceSummaryDto(
    Guid     Id,
    string   InvoiceNumber,
    string   ClientName,
    string   ClientEmail,
    string   Currency,
    int      Status,
    string   StatusLabel,
    DateOnly DueDate,
    decimal  TotalAmount,
    DateTime CreatedAt,
    DateTime? SentAt,
    DateTime? PaidAt);
