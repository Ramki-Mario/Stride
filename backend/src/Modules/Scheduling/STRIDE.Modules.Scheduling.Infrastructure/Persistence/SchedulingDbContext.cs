using Microsoft.EntityFrameworkCore;
using STRIDE.Modules.Scheduling.Domain.Entities;

namespace STRIDE.Modules.Scheduling.Infrastructure.Persistence;

public sealed class SchedulingDbContext : DbContext
{
    public SchedulingDbContext(DbContextOptions<SchedulingDbContext> options) : base(options) { }

    public DbSet<ScheduleDefinition> ScheduleDefinitions => Set<ScheduleDefinition>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("scheduling");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(SchedulingDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
