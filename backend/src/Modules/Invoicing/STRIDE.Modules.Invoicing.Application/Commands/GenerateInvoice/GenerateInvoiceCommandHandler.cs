using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Invoicing.Domain.Entities;
using STRIDE.Modules.Invoicing.Domain.Exceptions;
using STRIDE.Modules.Invoicing.Domain.Repositories;

namespace STRIDE.Modules.Invoicing.Application.Commands.GenerateInvoice;

internal sealed class GenerateInvoiceCommandHandler
    : IRequestHandler<GenerateInvoiceCommand, Result<Guid>>
{
    private readonly IInvoiceRepository _repo;
    private readonly IAuditLogger       _audit;
    private readonly ICurrentUser       _currentUser;

    public GenerateInvoiceCommandHandler(
        IInvoiceRepository repo,
        IAuditLogger       audit,
        ICurrentUser       currentUser)
    {
        _repo        = repo;
        _audit       = audit;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(GenerateInvoiceCommand request, CancellationToken ct)
    {
        // Enforce unique invoice number within tenant
        var existing = await _repo.GetByNumberAsync(request.TenantId, request.InvoiceNumber, ct);
        if (existing is not null)
            return Result<Guid>.Failure($"Invoice number '{request.InvoiceNumber}' already exists.");

        try
        {
            var invoice = Invoice.Generate(
                request.TenantId,
                request.InvoiceNumber,
                request.ClientName,
                request.ClientEmail,
                request.Currency,
                request.DueDate,
                request.Notes,
                request.CreatedBy);

            foreach (var item in request.LineItems)
                invoice.AddLineItem(item.Description, item.UnitPrice, item.Quantity);

            await _repo.AddAsync(invoice, ct);
            await _repo.SaveChangesAsync(ct);

            await _audit.LogAsync(
                tenantId:     request.TenantId,
                actorId:      request.CreatedBy,
                actorEmail:   _currentUser.Email,
                action:       AuditActions.InvoiceGenerated,
                resourceType: "Invoice",
                resourceId:   invoice.Id,
                newValueJson: $"{{\"invoiceNumber\":\"{request.InvoiceNumber}\",\"clientName\":\"{request.ClientName}\"}}");

            return Result<Guid>.Success(invoice.Id);
        }
        catch (InvoiceDomainException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
    }
}

