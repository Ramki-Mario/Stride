using MediatR;
using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.KitOps.Domain.Entities;

namespace STRIDE.Modules.KitOps.Infrastructure.Persistence;

/// <summary>
/// KitOps module DbContext. Applies a global EF query filter scoped to the current tenant on
/// every entity — the first STRIDE module to enforce tenant isolation by construction rather
/// than by per-query convention. Background services that must cross tenants call
/// <c>IgnoreQueryFilters()</c> explicitly.
///
/// Overrides <see cref="SaveChangesAsync"/> to dispatch domain events raised by aggregate roots
/// after each successful save (same pattern as <c>WorkflowsDbContext</c>).
/// </summary>
public sealed class KitOpsDbContext : DbContext
{
    private readonly Guid       _tenantId;
    private readonly IPublisher _publisher;

    public KitOpsDbContext(
        DbContextOptions<KitOpsDbContext> options,
        ITenantContext tenantContext,
        IPublisher publisher)
        : base(options)
    {
        _tenantId  = tenantContext.TenantId;
        _publisher = publisher;
    }

    // Internal constructor for design-time factory and tests — no live tenant context available.
    // Uses a no-op publisher so EF tooling and InMemory tests never need MediatR wired up.
    internal KitOpsDbContext(DbContextOptions<KitOpsDbContext> options, Guid tenantId)
        : base(options)
    {
        _tenantId  = tenantId;
        _publisher = NullPublisher.Instance;
    }

    public DbSet<KitItem>        KitItems        => Set<KitItem>();
    public DbSet<KitCheckout>    KitCheckouts    => Set<KitCheckout>();
    public DbSet<KitReservation> KitReservations => Set<KitReservation>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("kitops");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(KitOpsDbContext).Assembly);

        // Global tenant + soft-delete filters — applied to every query automatically.
        modelBuilder.Entity<KitItem>()
            .HasQueryFilter(e => e.TenantId == _tenantId && !e.IsDeleted);

        modelBuilder.Entity<KitCheckout>()
            .HasQueryFilter(e => e.TenantId == _tenantId && !e.IsDeleted);

        modelBuilder.Entity<KitReservation>()
            .HasQueryFilter(e => e.TenantId == _tenantId && !e.IsDeleted);
    }

    /// <summary>
    /// Saves changes then dispatches all domain events collected by aggregate roots.
    /// Events are dispatched AFTER the database commit so handlers see consistent state.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
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

        var result = await base.SaveChangesAsync(cancellationToken);

        foreach (var domainEvent in events)
        {
            var notification = (INotification)Activator.CreateInstance(
                typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()),
                domainEvent)!;

            await _publisher.Publish(notification, cancellationToken);
        }

        return result;
    }

    // Thin no-op publisher used by the design-time factory and InMemory tests.
    private sealed class NullPublisher : IPublisher
    {
        public static readonly NullPublisher Instance = new();
        public Task Publish(object notification, CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task Publish<TNotification>(TNotification notification, CancellationToken cancellationToken = default)
            where TNotification : INotification => Task.CompletedTask;
    }
}
