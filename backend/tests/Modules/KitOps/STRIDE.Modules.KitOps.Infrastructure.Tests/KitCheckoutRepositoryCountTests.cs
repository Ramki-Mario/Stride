using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Infrastructure.Persistence;

namespace STRIDE.Modules.KitOps.Infrastructure.Tests;

/// <summary>
/// Verifies the availability-counting predicates used to derive live stock:
/// outstanding = not-yet-returned (Active or Overdue), overdue = outstanding past its expected
/// return date. These run on EF InMemory (plain LINQ). The concurrency-safe
/// <see cref="KitCheckoutRepository.TryCheckoutAsync"/> guard relies on SQL Server row locks and is
/// exercised against a real database, not InMemory.
/// </summary>
public sealed class KitCheckoutRepositoryCountTests
{
    private readonly Guid _tenantId = Guid.NewGuid();
    private readonly Guid _userId   = Guid.NewGuid();

    private KitOpsDbContext CtxFor(string dbName)
    {
        var opts = new DbContextOptionsBuilder<KitOpsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new KitOpsDbContext(opts, _tenantId);
    }

    private KitCheckout MakeCheckout(Guid kitItemId, int days = 3)
        => KitCheckout.Create(new NewKitCheckout(
            _tenantId, kitItemId, _userId, DateTime.UtcNow.AddDays(days), null));

    [Fact]
    public async Task OutstandingCount_ExcludesReturnedCheckouts()
    {
        var dbName    = Guid.NewGuid().ToString();
        var kitItemId = Guid.NewGuid();

        var c1 = MakeCheckout(kitItemId);
        var c2 = MakeCheckout(kitItemId);
        var c3 = MakeCheckout(kitItemId);
        c3.Return(DateTime.UtcNow, _userId);   // returned → not outstanding

        await using (var ctx = CtxFor(dbName))
        {
            ctx.KitCheckouts.AddRange(c1, c2, c3);
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = CtxFor(dbName);
        var repo = new KitCheckoutRepository(readCtx);

        var outstanding = await repo.GetOutstandingCountByKitItemIdAsync(kitItemId);

        outstanding.Should().Be(2);
    }

    [Fact]
    public async Task OverdueCount_CountsOnlyOutstandingPastTheAsOfDate()
    {
        var dbName    = Guid.NewGuid().ToString();
        var kitItemId = Guid.NewGuid();

        var c1 = MakeCheckout(kitItemId, days: 3);
        var c2 = MakeCheckout(kitItemId, days: 3);
        c2.Return(DateTime.UtcNow, _userId);   // returned → never overdue

        await using (var ctx = CtxFor(dbName))
        {
            ctx.KitCheckouts.AddRange(c1, c2);
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = CtxFor(dbName);
        var repo = new KitCheckoutRepository(readCtx);

        // As of now: the 3-day checkout is not yet past due.
        (await repo.GetOverdueCountByKitItemIdAsync(kitItemId, DateTime.UtcNow))
            .Should().Be(0);

        // As of 10 days out: the outstanding 3-day checkout is overdue; the returned one is not.
        (await repo.GetOverdueCountByKitItemIdAsync(kitItemId, DateTime.UtcNow.AddDays(10)))
            .Should().Be(1);
    }
}
