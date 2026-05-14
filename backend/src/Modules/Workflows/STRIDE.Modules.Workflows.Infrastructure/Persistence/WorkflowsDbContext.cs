using Microsoft.EntityFrameworkCore;

namespace STRIDE.Modules.Workflows.Infrastructure.Persistence;

public sealed class WorkflowsDbContext : DbContext
{
    public WorkflowsDbContext(DbContextOptions<WorkflowsDbContext> options) : base(options) { }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("workflows");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(WorkflowsDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
