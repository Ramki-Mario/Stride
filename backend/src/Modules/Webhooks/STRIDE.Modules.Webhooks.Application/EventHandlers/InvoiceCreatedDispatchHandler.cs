using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Invoicing.Domain.Events;
using STRIDE.Modules.Webhooks.Application.Abstractions;

namespace STRIDE.Modules.Webhooks.Application.EventHandlers;

internal sealed class InvoiceCreatedDispatchHandler
    : INotificationHandler<DomainEventNotification<InvoiceGeneratedEvent>>
{
    private readonly IWebhookDispatcher                       _dispatcher;
    private readonly ILogger<InvoiceCreatedDispatchHandler>   _logger;

    public InvoiceCreatedDispatchHandler(
        IWebhookDispatcher dispatcher,
        ILogger<InvoiceCreatedDispatchHandler> logger)
    {
        _dispatcher = dispatcher;
        _logger     = logger;
    }

    public async Task Handle(
        DomainEventNotification<InvoiceGeneratedEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        try
        {
            await _dispatcher.DispatchAsync(
                tenantId:  e.TenantId,
                eventType: Domain.WebhookEventTypes.InvoiceCreated,
                data: new
                {
                    invoiceId     = e.InvoiceId,
                    invoiceNumber = e.InvoiceNumber,
                    createdBy     = e.CreatedBy,
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception dispatching webhook for invoice.created (invoice {Id})",
                e.InvoiceId);
        }
    }
}
