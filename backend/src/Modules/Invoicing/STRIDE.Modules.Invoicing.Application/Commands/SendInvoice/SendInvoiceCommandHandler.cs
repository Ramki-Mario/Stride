using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Invoicing.Domain.Exceptions;
using STRIDE.Modules.Invoicing.Domain.Repositories;

namespace STRIDE.Modules.Invoicing.Application.Commands.SendInvoice;

internal sealed class SendInvoiceCommandHandler : IRequestHandler<SendInvoiceCommand, Result>
{
    private readonly IInvoiceRepository _repo;
    private readonly IAuditLogger       _audit;
    private readonly ICurrentUser       _currentUser;

    public SendInvoiceCommandHandler(
        IInvoiceRepository repo,
        IAuditLogger       audit,
        ICurrentUser       currentUser)
    {
        _repo        = repo;
        _audit       = audit;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(SendInvoiceCommand request, CancellationToken ct)
    {
        var invoice = await _repo.GetByIdAsync(request.TenantId, request.InvoiceId, ct);
        if (invoice is null)
            return Result.Failure($"Invoice '{request.InvoiceId}' not found.");

        try
        {
            invoice.Send(request.SentBy);
            await _repo.SaveChangesAsync(ct);

            await _audit.LogAsync(
                tenantId:     request.TenantId,
                actorId:      request.SentBy,
                actorEmail:   _currentUser.Email,
                action:       AuditActions.InvoiceSent,
                resourceType: "Invoice",
                resourceId:   request.InvoiceId);

            return Result.Success();
        }
        catch (InvoiceDomainException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}

