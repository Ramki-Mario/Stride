using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Invoicing.Application.Abstractions;
using STRIDE.Modules.Invoicing.Application.DTOs;

namespace STRIDE.Modules.Invoicing.Application.Queries.GetInvoiceById;

internal sealed class GetInvoiceByIdQueryHandler
    : IRequestHandler<GetInvoiceByIdQuery, Result<InvoiceDetailDto>>
{
    private readonly IInvoiceReadService _read;

    public GetInvoiceByIdQueryHandler(IInvoiceReadService read) => _read = read;

    public async Task<Result<InvoiceDetailDto>> Handle(
        GetInvoiceByIdQuery request, CancellationToken cancellationToken)
    {
        var dto = await _read.GetInvoiceByIdAsync(request.TenantId, request.InvoiceId, cancellationToken);

        return dto is null
            ? Result<InvoiceDetailDto>.Failure($"Invoice '{request.InvoiceId}' not found.")
            : Result<InvoiceDetailDto>.Success(dto);
    }
}
