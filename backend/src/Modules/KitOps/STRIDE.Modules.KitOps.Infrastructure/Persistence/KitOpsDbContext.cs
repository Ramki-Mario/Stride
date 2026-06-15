using Microsoft.EntityFrameworkCore;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.KitOps.Domain.Entities;

namespace STRIDE.Modules.KitOps.Infrastructure.Persistence;

/// <summary>
/// KitOps module DbContext. Applies a global EF query filter scoped to the current tenant on
/// every entity — the first STRIDE module to enforce tenant isolation by construction rather
/// than by per-query convention. Background services that must cross tenants call
/// <c>IgnoreQueryFilters()</c> explicitly.
/// </summary>
public sealed class KitOpsDbContext : DbContext
{
    private readonly Guid _tenantId;

    public KitOpsDbContext(DbContextOptions<KitOpsDbContext> options, ITenantContext tenantContext)
        : base(options)
    {
        _tenantId = tenantContext.TenantId;
    }

    // Internal constructor for design-time factory and tests — no live tenant context available.
    internal KitOpsDbContext(DbContextOptions<KitOpsDbContext> options, Guid tenantId)
        : base(options)
    {
        _tenantId = tenantId;
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
}
