namespace STRIDE.Modules.Clients.Application.DTOs;

public sealed record ClientWorkflowDto(
    Guid     Id,
    string   WorkflowName,
    string   Status,
    DateTime CreatedAt,
    DateTime? CompletedAt);

public sealed record ClientInvoiceDto(
    Guid     Id,
    string   InvoiceNumber,
    int      Status,
    string   StatusLabel,
    decimal  TotalAmount,
    string   Currency,
    DateTime CreatedAt);

public sealed record ClientHistoryDto(
    Guid                        ClientId,
    string                      ClientName,
    IReadOnlyList<ClientWorkflowDto> Workflows,
    IReadOnlyList<ClientInvoiceDto>  Invoices);
