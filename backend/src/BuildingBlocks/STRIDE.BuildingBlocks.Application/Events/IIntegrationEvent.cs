using MediatR;

namespace STRIDE.BuildingBlocks.Application.Events;

/// <summary>
/// Marker for cross-module integration events published via IEventBus.
/// Extends INotification so the MediatR-backed in-process IEventBus can dispatch it (ADR-014).
/// </summary>
public interface IIntegrationEvent : INotification { }
