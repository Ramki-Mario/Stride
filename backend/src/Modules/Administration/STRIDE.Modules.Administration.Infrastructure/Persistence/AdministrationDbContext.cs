using Microsoft.EntityFrameworkCore;
using STRIDE.Modules.Administration.Domain.Entities;

namespace STRIDE.Modules.Administration.Infrastructure.Persistence;

public sealed class AdministrationDbContext : DbContext
{
    public AdministrationDbContext(DbContextOptions<AdministrationDbContext> options) : base(options) { }

    public DbSet<TenantSettings> TenantSettings => Set<TenantSettings>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.HasDefaultSchema("administration");
        modelBuilder.ApplyConfigurationsFromAssembly(typeof(AdministrationDbContext).Assembly);
        base.OnModelCreating(modelBuilder);
    }
}
