using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Invoicing.Application.DTOs;
using STRIDE.Modules.Invoicing.Domain.Repositories;

namespace STRIDE.Modules.Invoicing.Application.Queries.GetInvoiceByWorkflowInstanceId;

internal sealed class GetInvoiceByWorkflowInstanceIdQueryHandler
    : IRequestHandler<GetInvoiceByWorkflowInstanceIdQuery, Result<InvoiceReferenceDto?>>
{
    private static readonly string[] StatusLabels = ["Draft", "Sent", "Paid", "Void"];

    private readonly IInvoiceRepository _repo;

    public GetInvoiceByWorkflowInstanceIdQueryHandler(IInvoiceRepository repo)
        => _repo = repo;

    public async Task<Result<InvoiceReferenceDto?>> Handle(
        GetInvoiceByWorkflowInstanceIdQuery request,
        CancellationToken cancellationToken)
    {
        var invoice = await _repo.GetBySourceWorkflowInstanceIdAsync(
            request.TenantId, request.WorkflowInstanceId, cancellationToken);

        if (invoice is null)
            return Result.Success<InvoiceReferenceDto?>(null);

        var statusInt = (int)invoice.Status;
        var dto = new InvoiceReferenceDto(
            invoice.Id,
            invoice.InvoiceNumber,
            statusInt,
            StatusLabels[statusInt]);

        return Result.Success<InvoiceReferenceDto?>(dto);
    }
}
