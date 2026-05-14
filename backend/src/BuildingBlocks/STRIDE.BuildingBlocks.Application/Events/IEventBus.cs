namespace STRIDE.BuildingBlocks.Application.Events;

public interface IEventBus
{
    Task PublishAsync<T>(T integrationEvent, CancellationToken ct = default)
        where T : IIntegrationEvent;
}
