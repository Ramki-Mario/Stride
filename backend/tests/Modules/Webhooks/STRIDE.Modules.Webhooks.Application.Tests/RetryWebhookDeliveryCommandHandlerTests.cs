using STRIDE.Modules.Webhooks.Application.Commands.RetryWebhookDelivery;
using STRIDE.Modules.Webhooks.Domain;
using STRIDE.Modules.Webhooks.Domain.Entities;
using STRIDE.Modules.Webhooks.Domain.Repositories;

namespace STRIDE.Modules.Webhooks.Application.Tests;

public sealed class RetryWebhookDeliveryCommandHandlerTests
{
    private readonly IWebhookDeliveryRepository     _deliveries    = Substitute.For<IWebhookDeliveryRepository>();
    private readonly Guid                           _tenantId      = Guid.NewGuid();
    private readonly Guid                           _subscriptionId = Guid.NewGuid();

    private RetryWebhookDeliveryCommandHandler CreateHandler() =>
        new(_deliveries);

    private WebhookDelivery ExhaustedDelivery()
    {
        var d = WebhookDelivery.Create(
            _tenantId, _subscriptionId,
            WebhookEventTypes.WorkflowInstanceCompleted,
            "https://example.com/hooks", "{}");

        // Drive to exhausted (4 failures).
        d.RecordFailure(503, "err");
        d.RecordFailure(503, "err");
        d.RecordFailure(503, "err");
        d.RecordFailure(503, "err");
        return d;
    }

    [Fact]
    public async Task Handle_DeliveryNotFound_ReturnsFailure()
    {
        _deliveries.GetByIdAsync(_tenantId, Arg.Any<Guid>()).Returns((WebhookDelivery?)null);

        var result = await CreateHandler().Handle(
            new RetryWebhookDeliveryCommand(_tenantId, _subscriptionId, Guid.NewGuid()),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DeliveryBelongsToDifferentSubscription_ReturnsFailure()
    {
        var delivery = ExhaustedDelivery();
        _deliveries.GetByIdAsync(_tenantId, delivery.Id).Returns(delivery);

        var result = await CreateHandler().Handle(
            new RetryWebhookDeliveryCommand(_tenantId, Guid.NewGuid() /* wrong sub */, delivery.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_DeliveryNotExhausted_ReturnsFailure()
    {
        var d = WebhookDelivery.Create(
            _tenantId, _subscriptionId,
            WebhookEventTypes.WorkflowInstanceCompleted,
            "https://example.com/hooks", "{}");
        d.RecordFailure(503, "err"); // Failed, not Exhausted

        _deliveries.GetByIdAsync(_tenantId, d.Id).Returns(d);

        var result = await CreateHandler().Handle(
            new RetryWebhookDeliveryCommand(_tenantId, _subscriptionId, d.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeFalse();
    }

    [Fact]
    public async Task Handle_ValidExhaustedDelivery_ReturnsSuccessAndSaves()
    {
        var delivery = ExhaustedDelivery();
        _deliveries.GetByIdAsync(_tenantId, delivery.Id).Returns(delivery);

        var result = await CreateHandler().Handle(
            new RetryWebhookDeliveryCommand(_tenantId, _subscriptionId, delivery.Id),
            CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        _deliveries.Received(1).Update(delivery);
        await _deliveries.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }
}
