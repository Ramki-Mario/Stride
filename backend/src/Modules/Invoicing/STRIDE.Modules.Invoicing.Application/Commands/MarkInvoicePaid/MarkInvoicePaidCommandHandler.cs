using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Invoicing.Domain.Exceptions;
using STRIDE.Modules.Invoicing.Domain.Repositories;

namespace STRIDE.Modules.Invoicing.Application.Commands.MarkInvoicePaid;

internal sealed class MarkInvoicePaidCommandHandler : IRequestHandler<MarkInvoicePaidCommand, Result>
{
    private readonly IInvoiceRepository _repo;

    public MarkInvoicePaidCommandHandler(IInvoiceRepository repo) => _repo = repo;

    public async Task<Result> Handle(MarkInvoicePaidCommand request, CancellationToken ct)
    {
        var invoice = await _repo.GetByIdAsync(request.TenantId, request.InvoiceId, ct);
        if (invoice is null)
            return Result.Failure($"Invoice '{request.InvoiceId}' not found.");

        try
        {
            invoice.MarkPaid(request.MarkedBy);
            await _repo.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (InvoiceDomainException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
