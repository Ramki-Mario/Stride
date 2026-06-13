using MediatR;
using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Webhooks.Application.Abstractions;
using STRIDE.Modules.Webhooks.Domain.Entities;

namespace STRIDE.Modules.Webhooks.Infrastructure.Persistence;

public sealed class WebhooksDbContext : DbContext
{
    private readonly IWebhookSecretProtector _protector;
    private readonly IPublisher?             _publisher;

    public DbSet<WebhookSubscription> WebhookSubscriptions { get; set; } = null!;
    public DbSet<WebhookDelivery>     WebhookDeliveries    { get; set; } = null!;

    public WebhooksDbContext(
        DbContextOptions<WebhooksDbContext> options,
        IWebhookSecretProtector protector,
        IPublisher publisher)
        : base(options)
    {
        _protector = protector;
        _publisher = publisher;
    }

    // Design-time factory uses this overload (no publisher needed for migrations).
    internal WebhooksDbContext(
        DbContextOptions<WebhooksDbContext> options,
        IWebhookSecretProtector protector)
        : base(options)
        => _protector = protector;

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("webhooks");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WebhooksDbContext).Assembly);

        // Encrypt the signing secret at rest.
        modelBuilder.Entity<WebhookSubscription>()
            .Property(s => s.SigningSecret)
            .HasConversion(
                plain  => _protector.Protect(plain),
                stored => _protector.Unprotect(stored));

        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Saves changes then dispatches all domain events raised by aggregate roots.
    /// Events are dispatched after the commit so handlers see consistent state.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        var aggregates = ChangeTracker
            .Entries<AuditableEntity>()
            .Where(e => e.Entity.DomainEvents.Count > 0)
            .Select(e => e.Entity)
            .ToList();

        var events = aggregates.SelectMany(a => a.DomainEvents).ToList();

        foreach (var aggregate in aggregates)
            aggregate.ClearDomainEvents();

        var result = await base.SaveChangesAsync(cancellationToken);

        if (_publisher is not null)
        {
            foreach (var domainEvent in events)
            {
                var notification = (INotification)Activator.CreateInstance(
                    typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()),
                    domainEvent)!;

                await _publisher.Publish(notification, cancellationToken);
            }
        }

        return result;
    }
}
