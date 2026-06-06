using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Invoicing.Domain.Exceptions;
using STRIDE.Modules.Invoicing.Domain.Repositories;

namespace STRIDE.Modules.Invoicing.Application.Commands.VoidInvoice;

internal sealed class VoidInvoiceCommandHandler : IRequestHandler<VoidInvoiceCommand, Result>
{
    private readonly IInvoiceRepository _repo;
    private readonly IAuditLogger       _audit;
    private readonly ICurrentUser       _currentUser;

    public VoidInvoiceCommandHandler(
        IInvoiceRepository repo,
        IAuditLogger       audit,
        ICurrentUser       currentUser)
    {
        _repo        = repo;
        _audit       = audit;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(VoidInvoiceCommand request, CancellationToken cancellationToken)
    {
        var invoice = await _repo.GetByIdAsync(request.TenantId, request.InvoiceId, cancellationToken);
        if (invoice is null)
            return Result.Failure($"Invoice '{request.InvoiceId}' not found.");

        try
        {
            invoice.Void(request.VoidedBy);
            await _repo.SaveChangesAsync(cancellationToken);

            await _audit.LogAsync(
                tenantId:     request.TenantId,
                actorId:      request.VoidedBy,
                actorEmail:   _currentUser.Email,
                action:       AuditActions.InvoiceVoided,
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

