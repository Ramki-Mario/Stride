using MediatR;
using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Domain.Entities;
using STRIDE.BuildingBlocks.Infrastructure.Events;
using STRIDE.Modules.Workflows.Domain.Entities;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence;

public sealed class WorkflowsDbContext : DbContext
{
    private readonly IPublisher _publisher;

    public WorkflowsDbContext(DbContextOptions<WorkflowsDbContext> options, IPublisher publisher)
        : base(options)
    {
        _publisher = publisher;
    }

    public DbSet<WorkflowDefinition>  WorkflowDefinitions  => Set<WorkflowDefinition>();
    public DbSet<StepDefinition>      StepDefinitions      => Set<StepDefinition>();
    public DbSet<StepFieldDefinition> StepFieldDefinitions => Set<StepFieldDefinition>();
    public DbSet<WorkflowInstance>    WorkflowInstances    => Set<WorkflowInstance>();
    public DbSet<StepInstance>        StepInstances        => Set<StepInstance>();
    public DbSet<BillableItem>        BillableItems        => Set<BillableItem>();
    public DbSet<StepFieldValue>      StepFieldValues      => Set<StepFieldValue>();
    public DbSet<Attachment>          Attachments          => Set<Attachment>();
    public DbSet<WorkflowComment>         WorkflowComments         => Set<WorkflowComment>();
    public DbSet<WorkflowActivityEvent>   WorkflowActivityEvents   => Set<WorkflowActivityEvent>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("workflows");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkflowsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }

    /// <summary>
    /// Saves changes then dispatches all domain events collected by aggregate roots.
    /// Events are dispatched AFTER the database commit so handlers see consistent state.
    /// </summary>
    public override async Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
    {
        // Collect and clear domain events before saving
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

        // Dispatch events after successful save
        foreach (var domainEvent in events)
        {
            var notification = (INotification)Activator.CreateInstance(
                typeof(DomainEventNotification<>).MakeGenericType(domainEvent.GetType()),
                domainEvent)!;

            await _publisher.Publish(notification, cancellationToken);
        }

        return result;
    }
}
