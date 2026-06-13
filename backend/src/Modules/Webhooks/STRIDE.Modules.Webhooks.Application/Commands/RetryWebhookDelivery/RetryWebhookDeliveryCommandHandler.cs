using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Webhooks.Domain.Exceptions;
using STRIDE.Modules.Webhooks.Domain.Repositories;

namespace STRIDE.Modules.Webhooks.Application.Commands.RetryWebhookDelivery;

internal sealed class RetryWebhookDeliveryCommandHandler
    : IRequestHandler<RetryWebhookDeliveryCommand, Result>
{
    private readonly IWebhookDeliveryRepository _deliveries;

    public RetryWebhookDeliveryCommandHandler(IWebhookDeliveryRepository deliveries)
        => _deliveries = deliveries;

    public async Task<Result> Handle(RetryWebhookDeliveryCommand request, CancellationToken cancellationToken)
    {
        var delivery = await _deliveries.GetByIdAsync(request.TenantId, request.DeliveryId, cancellationToken);
        if (delivery is null)
            return Result.Failure("Webhook delivery not found.");

        // Guard: delivery must belong to the requested subscription (IDOR defence).
        if (delivery.WebhookSubscriptionId != request.SubscriptionId)
            return Result.Failure("Webhook delivery not found.");

        try
        {
            delivery.ScheduleManualRetry();
            _deliveries.Update(delivery);
            await _deliveries.SaveChangesAsync(cancellationToken);
            return Result.Success();
        }
        catch (WebhookDomainException ex)
        {
            return Result.Failure(ex.Message);
        }
    }
}
