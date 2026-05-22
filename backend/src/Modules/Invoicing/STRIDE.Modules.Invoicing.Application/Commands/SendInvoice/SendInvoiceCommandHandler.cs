using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Invoicing.Domain.Exceptions;
using STRIDE.Modules.Invoicing.Domain.Repositories;

namespace STRIDE.Modules.Invoicing.Application.Commands.SendInvoice;

internal sealed class SendInvoiceCommandHandler : IRequestHandler<SendInvoiceCommand, Result>
{
    private readonly IInvoiceRepository _repo;

    public SendInvoiceCommandHandler(IInvoiceRepository repo) => _repo = repo;

    public async Task<Result> Handle(SendInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await _repo.GetByIdAsync(request.TenantId, request.InvoiceId, ct);
        if (invoice is null)
            return Result.Failure($"Invoice '{request.InvoiceId}' not found.");

        try
        {
            invoice.Send(request.SentBy);
            await _repo.SaveChangesAsync(ct);
            return Result.Success();
        }
        catch (InvoiceDomainException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
