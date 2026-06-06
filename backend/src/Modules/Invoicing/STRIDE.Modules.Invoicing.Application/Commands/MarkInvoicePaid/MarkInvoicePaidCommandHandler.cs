using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Invoicing.Domain.Exceptions;
using STRIDE.Modules.Invoicing.Domain.Repositories;

namespace STRIDE.Modules.Invoicing.Application.Commands.MarkInvoicePaid;

internal sealed class MarkInvoicePaidCommandHandler : IRequestHandler<MarkInvoicePaidCommand, Result>
{
    private readonly IInvoiceRepository _repo;
    private readonly IAuditLogger       _audit;
    private readonly ICurrentUser       _currentUser;

    public MarkInvoicePaidCommandHandler(
        IInvoiceRepository repo,
        IAuditLogger       audit,
        ICurrentUser       currentUser)
    {
        _repo        = repo;
        _audit       = audit;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(MarkInvoicePaidCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _repo.GetByIdAsync(request.TenantId, request.InvoiceId, cancellationToken);
        if (invoice is null)
            return Result.Failure($"Invoice '{request.InvoiceId}' not found.");

        try
        {
            invoice.MarkPaid(request.MarkedBy);
            await _repo.SaveChangesAsync(cancellationToken);

            await _audit.LogAsync(new AuditLogEntry(
                TenantId:     request.TenantId,
                ActorId:      request.MarkedBy,
                ActorEmail:   _currentUser.Email,
                Action:       AuditActions.InvoicePaid,
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

