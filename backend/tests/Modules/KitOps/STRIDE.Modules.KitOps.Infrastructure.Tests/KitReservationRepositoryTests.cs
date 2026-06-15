using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Infrastructure.Persistence;

namespace STRIDE.Modules.KitOps.Infrastructure.Tests;

/// <summary>
/// Verifies the reservation queue predicates: the duplicate-pending guard
/// (<see cref="KitReservationRepository.ExistsPendingForUserAsync"/>) and FIFO ordering of the
/// oldest pending request. The DB-level unique filtered index is the production backstop for the
/// guard; these tests cover the application-level predicate on EF InMemory.
/// </summary>
public sealed class KitReservationRepositoryTests
{
    private readonly Guid _tenantId = Guid.NewGuid();

    private KitOpsDbContext CtxFor(string dbName)
    {
        var opts = new DbContextOptionsBuilder<KitOpsDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;

        return new KitOpsDbContext(opts, _tenantId);
    }

    private KitReservation MakeReservation(Guid kitItemId, Guid userId)
        => KitReservation.Create(new NewKitReservation(_tenantId, kitItemId, userId, null));

    // CreatedAt is set to UtcNow at construction; override it so FIFO ordering is deterministic.
    private static void SetCreatedAt(KitReservation reservation, DateTime value)
        => typeof(KitReservation).GetProperty(nameof(KitReservation.CreatedAt))!
            .SetValue(reservation, value);

    [Fact]
    public async Task ExistsPendingForUser_TrueOnlyWhilePending()
    {
        var dbName    = Guid.NewGuid().ToString();
        var kitItemId = Guid.NewGuid();
        var userId    = Guid.NewGuid();

        var pending = MakeReservation(kitItemId, userId);

        await using (var ctx = CtxFor(dbName))
        {
            ctx.KitReservations.Add(pending);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CtxFor(dbName))
        {
            var repo = new KitReservationRepository(ctx);
            (await repo.ExistsPendingForUserAsync(kitItemId, userId)).Should().BeTrue();
            (await repo.ExistsPendingForUserAsync(kitItemId, Guid.NewGuid())).Should().BeFalse("different user");
        }

        // Once cancelled, the user no longer holds a pending request and may re-request.
        await using (var ctx = CtxFor(dbName))
        {
            var reservation = await ctx.KitReservations.FirstAsync(r => r.Id == pending.Id);
            reservation.Cancel(userId);
            await ctx.SaveChangesAsync();
        }

        await using (var ctx = CtxFor(dbName))
        {
            var repo = new KitReservationRepository(ctx);
            (await repo.ExistsPendingForUserAsync(kitItemId, userId)).Should().BeFalse("request was cancelled");
        }
    }

    [Fact]
    public async Task GetOldestPending_ReturnsEarliestCreatedRequest()
    {
        var dbName    = Guid.NewGuid().ToString();
        var kitItemId = Guid.NewGuid();

        var first  = MakeReservation(kitItemId, Guid.NewGuid());
        var second = MakeReservation(kitItemId, Guid.NewGuid());
        SetCreatedAt(first,  DateTime.UtcNow.AddMinutes(-10));
        SetCreatedAt(second, DateTime.UtcNow.AddMinutes(-5));

        await using (var ctx = CtxFor(dbName))
        {
            ctx.KitReservations.AddRange(second, first);
            await ctx.SaveChangesAsync();
        }

        await using var readCtx = CtxFor(dbName);
        var repo = new KitReservationRepository(readCtx);

        var oldest = await repo.GetOldestPendingByKitItemIdAsync(kitItemId);

        oldest.Should().NotBeNull();
        oldest!.Id.Should().Be(first.Id);
    }
}
