using MediatR;
using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Notifications.Domain.Entities;

namespace STRIDE.Modules.Notifications.Infrastructure.Persistence;

public sealed class NotificationsDbContext : DbContext
{
    private readonly IPublisher _publisher;

    public NotificationsDbContext(DbContextOptions<NotificationsDbContext> options, IPublisher publisher)
        : base(options)
    {
        _publisher = publisher;
    }

    public DbSet<Notification> Notifications => Set<Notification>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("notifications");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(NotificationsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Saves changes then dispatches all domain events collected by aggregate roots.
    /// Events are dispatched AFTER the database commit so handlers see consistent state.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken ct = default)
    {
        var aggregates = ChangeTracker
            .Entries<AuditableEntity>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates
            .SelectMany(a => a.DomainEvents)
            .ToList();

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        var result = await base.SaveChangesAsync(ct);

        foreach (var domainEvent in events)
        {
            var notification = (INotification)Activator.CreateInstance(
                typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()),
                domainEvent)!;

            await _publisher.Publish(notification, ct);
        }

        return result;
    }
}
