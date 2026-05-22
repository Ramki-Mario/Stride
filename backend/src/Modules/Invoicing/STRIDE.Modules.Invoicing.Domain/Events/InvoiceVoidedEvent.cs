using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.Modules.Invoicing.Domain.Events;

public sealed record InvoiceVoidedEvent(
    Guid   InvoiceId,
    Guid   TenantId,
    string InvoiceNumber,
    Guid   VoidedBy) : IDomainEvent;
