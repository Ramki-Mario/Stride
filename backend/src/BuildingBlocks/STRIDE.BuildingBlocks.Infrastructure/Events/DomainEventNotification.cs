using MediatR;
using STRIDE.BuildingBlocks.Domain.Events;

namespace STRIDE.BuildingBlocks.Infrastructure.Events;

/// <summary>
/// Wraps an <see cref="IDomainEvent"/> as an MediatR <see cref="INotification"/>
/// so it can be published via <see cref="IPublisher"/>.
/// <para>
/// Usage: override <c>SaveChangesAsync</c> in a module DbContext, collect domain events
/// from tracked aggregates, clear them, then publish each wrapped event after the
/// database transaction commits.
/// </para>
/// </summary>
public sealed record DomainEventNotification<T>(T DomainEvent) : INotification
    where T : IDomainEvent;
