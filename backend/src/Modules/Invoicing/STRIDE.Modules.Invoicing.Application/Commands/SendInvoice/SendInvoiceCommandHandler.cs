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

    public async Task<Result> Handle(SendInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _repo.GetByIdAsync(request.TenantId, request.InvoiceId, cancellationToken);
        if (invoice is null)
            return Result.Failure($"Invoice '{request.InvoiceId}' not found.");

        try
        {
            invoice.Send(request.SentBy);
            await _repo.SaveChangesAsync(cancellationToken);

            await _audit.LogAsync(new AuditLogEntry(
                TenantId:     request.TenantId,
                ActorId:      request.SentBy,
                ActorEmail:   _currentUser.Email,
                Action:       AuditActions.InvoiceSent,
                ResourceType: "Invoice",
                ResourceId:   request.InvoiceId));

            return Result.Success();
        }
        catch (InvoiceDomainException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}

