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

    public async Task<Result<Guid>> Handle(GenerateInvoiceCommand request, CancellationToken cancellationToken)
    {
        // Enforce unique invoice number within tenant
        var existing = await _repo.GetByNumberAsync(request.TenantId, request.InvoiceNumber, cancellationToken);
        if (existing is not null)
            return Result<Guid>.Failure($"Invoice number '{request.InvoiceNumber}' already exists.");

        try
        {
            var invoice = Invoice.Generate(new NewInvoice(
                TenantId:      request.TenantId,
                InvoiceNumber: request.InvoiceNumber,
                ClientName:    request.ClientName,
                ClientEmail:   request.ClientEmail,
                Currency:      request.Currency,
                DueDate:       request.DueDate,
                CreatedBy:     request.CreatedBy,
                Notes:         request.Notes,
                ClientId:      request.ClientId));

            foreach (var item in request.LineItems)
                invoice.AddLineItem(item.Description, item.UnitPrice, item.Quantity);

            await _repo.AddAsync(invoice, cancellationToken);
            await _repo.SaveChangesAsync(cancellationToken);

            await _audit.LogAsync(new AuditLogEntry(
                TenantId:     request.TenantId,
                ActorId:      request.CreatedBy,
                ActorEmail:   _currentUser.Email,
                Action:       AuditActions.InvoiceGenerated,
                ResourceType: "Invoice",
                ResourceId:   invoice.Id,
                NewValueJson: $"{{\"invoiceNumber\":\"{request.InvoiceNumber}\",\"clientName\":\"{request.ClientName}\"}}"));

            return Result<Guid>.Success(invoice.Id);
        }
        catch (InvoiceDomainException ex)
        {
            return Result<Guid>.Failure(ex.Message);
        }
    }
}

