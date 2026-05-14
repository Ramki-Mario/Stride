using MediatR;
using STRIDE.BuildingBlocks.Application.Events;

namespace STRIDE.BuildingBlocks.Infrastructure.Events;

internal sealed class MediatREventBus : IEventBus
{
    private readonly IPublisher _publisher;

    public MediatREventBus(IPublisher publisher) => _publisher = publisher;

    public Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default)
        where T : IIntegrationEvent
        => _publisher.Publish(integrationEvent, ct);
}
