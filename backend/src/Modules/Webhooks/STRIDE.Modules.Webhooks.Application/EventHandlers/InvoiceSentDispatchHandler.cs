using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Invoicing.Domain.Events;
using STRIDE.Modules.Webhooks.Application.Abstractions;

namespace STRIDE.Modules.Webhooks.Application.EventHandlers;

internal sealed class InvoiceSentDispatchHandler
    : INotificationHandler<DomainEventNotification<InvoiceSentEvent>>
{
    private readonly IWebhookDispatcher                     _dispatcher;
    private readonly ILogger<InvoiceSentDispatchHandler>    _logger;

    public InvoiceSentDispatchHandler(
        IWebhookDispatcher dispatcher,
        ILogger<InvoiceSentDispatchHandler> logger)
    {
        _dispatcher = dispatcher;
        _logger     = logger;
    }

    public async Task Handle(
        DomainEventNotification<InvoiceSentEvent> notification,
        CancellationToken cancellationToken)
    {
        var e = notification.DomainEvent;

        try
        {
            await _dispatcher.DispatchAsync(
                tenantId:  e.TenantId,
                eventType: Domain.WebhookEventTypes.InvoiceSent,
                data: new
                {
                    invoiceId     = e.InvoiceId,
                    invoiceNumber = e.InvoiceNumber,
                    sentBy        = e.SentBy,
                },
                cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex,
                "Unhandled exception dispatching webhook for invoice.sent (invoice {Id})",
                e.InvoiceId);
        }
    }
}
