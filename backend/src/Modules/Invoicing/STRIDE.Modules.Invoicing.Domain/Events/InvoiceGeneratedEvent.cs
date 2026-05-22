using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Invoicing.Domain.Events;

public sealed record InvoiceGeneratedEvent(
    Guid   InvoiceId,
    Guid   TenantId,
    string InvoiceNumber,
    Guid   CreatedBy) : IDomainEvent;
