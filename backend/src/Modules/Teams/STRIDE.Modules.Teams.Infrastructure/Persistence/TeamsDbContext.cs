using Microsoft.EntityFrameworkCore;
using STRIDE.Modules.Teams.Domain.Entities;

namespace STRIDE.Modules.Teams.Infrastructure.Persistence;

public sealed class TeamsDbContext : DbContext
{
    public TeamsDbContext(DbContextOptions<TeamsDbContext> options) : base(options) { }

    public DbSet<Team> Teams => Set<Team>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("teams");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(TeamsDbContext).Assembly);
    }
}
