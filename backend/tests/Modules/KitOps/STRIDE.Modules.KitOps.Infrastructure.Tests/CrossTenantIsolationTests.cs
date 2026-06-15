using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Infrastructure.Persistence;

namespace STRIDE.Modules.KitOps.Infrastructure.Tests;

/// <summary>
/// Proves that KitOps EF global query filters block cross-tenant data access.
/// Each test shares an in-memory database so Tenant A's rows exist in the store;
/// a context scoped to Tenant B must not be able to read, find, or count them.
/// </summary>
public sealed class CrossTenantIsolationTests : IDisposable
{
    private readonly string _dbName = Guid.NewGuid().ToString();

    private readonly Guid _tenantA = Guid.NewGuid();
    private readonly Guid _tenantB = Guid.NewGuid();

    // ── Helpers ───────────────────────────────────────────────────────────────

    private KitOpsDbContext CtxFor(Guid tenantId)
    {
        var opts = new DbContextOptionsBuilder<KitOpsDbContext>()
            .UseInMemoryDatabase(_dbName)
            .Options;

        return new KitOpsDbContext(opts, tenantId);
    }

    private static KitItem MakeItem(Guid tenantId, string name = "Helmet")
        => KitItem.Create(new NewKitItem(tenantId, name, "PPE", null, 3, Guid.NewGuid()));

    private static KitCheckout MakeCheckout(Guid tenantId, Guid kitItemId)
        => KitCheckout.Create(new NewKitCheckout(
            tenantId, kitItemId, Guid.NewGuid(), DateTime.UtcNow.AddDays(7), null));

    private static KitReservation MakeReservation(Guid tenantId, Guid kitItemId)
        => KitReservation.Create(new NewKitReservation(
            tenantId, kitItemId, Guid.NewGuid(), null));

    public void Dispose() { }

    // ── KitItem isolation ─────────────────────────────────────────────────────

    [Fact]
    public async Task KitItem_TenantB_CannotRead_TenantA_Item()
    {
        var item = MakeItem(_tenantA);
        await using (var ctx = CtxFor(_tenantA))
        {
            ctx.KitItems.Add(item);
            await ctx.SaveChangesAsync();
        }

        await using var ctxB = CtxFor(_tenantB);
        var found = await ctxB.KitItems.FirstOrDefaultAsync(k => k.Id == item.Id);

        found.Should().BeNull("global query filter must exclude Tenant A rows from Tenant B context");
    }

    [Fact]
    public async Task KitItem_TenantB_CannotFind_TenantA_Item()
    {
        var item = MakeItem(_tenantA);
        await using (var ctx = CtxFor(_tenantA))
        {
            ctx.KitItems.Add(item);
            await ctx.SaveChangesAsync();
        }

        await using var ctxB = CtxFor(_tenantB);
        var found = await ctxB.KitItems.FindAsync(item.Id);

        found.Should().BeNull("FindAsync must respect the global query filter");
    }

    [Fact]
    public async Task KitItem_TenantA_CanRead_OwnItem()
    {
        var item = MakeItem(_tenantA);
        await using (var ctx = CtxFor(_tenantA))
        {
            ctx.KitItems.Add(item);
            await ctx.SaveChangesAsync();
        }

        await using var ctxA2 = CtxFor(_tenantA);
        var found = await ctxA2.KitItems.FirstOrDefaultAsync(k => k.Id == item.Id);

        found.Should().NotBeNull("Tenant A must be able to read its own items");
    }

    [Fact]
    public async Task KitItem_TenantB_CountIsZero_WhenOnlyTenantAItems_Exist()
    {
        await using (var ctx = CtxFor(_tenantA))
        {
            ctx.KitItems.Add(MakeItem(_tenantA, "Helmet"));
            ctx.KitItems.Add(MakeItem(_tenantA, "Gloves"));
            await ctx.SaveChangesAsync();
        }

        await using var ctxB = CtxFor(_tenantB);
        var count = await ctxB.KitItems.CountAsync();

        count.Should().Be(0);
    }

    // ── KitCheckout isolation ─────────────────────────────────────────────────

    [Fact]
    public async Task KitCheckout_TenantB_CannotRead_TenantA_Checkout()
    {
        var item     = MakeItem(_tenantA);
        var checkout = MakeCheckout(_tenantA, item.Id);

        await using (var ctx = CtxFor(_tenantA))
        {
            ctx.KitItems.Add(item);
            ctx.KitCheckouts.Add(checkout);
            await ctx.SaveChangesAsync();
        }

        await using var ctxB = CtxFor(_tenantB);
        var found = await ctxB.KitCheckouts.FirstOrDefaultAsync(c => c.Id == checkout.Id);

        found.Should().BeNull("Tenant B must not see Tenant A's checkout records");
    }

    [Fact]
    public async Task KitCheckout_TenantB_ActiveCount_IsZero_ForTenantA_KitItem()
    {
        var item     = MakeItem(_tenantA);
        var checkout = MakeCheckout(_tenantA, item.Id);

        await using (var ctx = CtxFor(_tenantA))
        {
            ctx.KitItems.Add(item);
            ctx.KitCheckouts.Add(checkout);
            await ctx.SaveChangesAsync();
        }

        await using var ctxB = CtxFor(_tenantB);
        var count = await ctxB.KitCheckouts.CountAsync(c => c.KitItemId == item.Id);

        count.Should().Be(0, "active checkout count must be zero when queried from a different tenant");
    }

    // ── KitReservation isolation ──────────────────────────────────────────────

    [Fact]
    public async Task KitReservation_TenantB_CannotRead_TenantA_Reservation()
    {
        var item        = MakeItem(_tenantA);
        var reservation = MakeReservation(_tenantA, item.Id);

        await using (var ctx = CtxFor(_tenantA))
        {
            ctx.KitItems.Add(item);
            ctx.KitReservations.Add(reservation);
            await ctx.SaveChangesAsync();
        }

        await using var ctxB = CtxFor(_tenantB);
        var found = await ctxB.KitReservations.FirstOrDefaultAsync(r => r.Id == reservation.Id);

        found.Should().BeNull("Tenant B must not see Tenant A's reservation records");
    }

    [Fact]
    public async Task KitReservation_TenantB_PendingCount_IsZero_ForTenantA_KitItem()
    {
        var item        = MakeItem(_tenantA);
        var reservation = MakeReservation(_tenantA, item.Id);

        await using (var ctx = CtxFor(_tenantA))
        {
            ctx.KitItems.Add(item);
            ctx.KitReservations.Add(reservation);
            await ctx.SaveChangesAsync();
        }

        await using var ctxB = CtxFor(_tenantB);
        var count = await ctxB.KitReservations.CountAsync(r => r.KitItemId == item.Id);

        count.Should().Be(0, "reservation count must be zero when queried from a different tenant");
    }
}
