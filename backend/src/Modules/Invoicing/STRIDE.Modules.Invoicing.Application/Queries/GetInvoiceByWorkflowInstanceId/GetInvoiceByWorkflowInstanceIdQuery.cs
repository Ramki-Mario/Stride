using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Invoicing.Application.DTOs;

namespace STRIDE.Modules.Invoicing.Application.Queries.GetInvoiceByWorkflowInstanceId;

/// <summary>
/// Returns a lightweight <see cref="InvoiceReferenceDto"/> for the invoice that was
/// auto-generated (or manually created) from a specific workflow instance, or null when
/// no invoice exists yet.
/// </summary>
public sealed record GetInvoiceByWorkflowInstanceIdQuery(
    Guid TenantId,
    Guid WorkflowInstanceId) : IRequest<Result<InvoiceReferenceDto?>>;
