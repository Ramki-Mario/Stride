using STRIDE.Modules.KitOps.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.DTOs;
using STRIDE.Modules.KitOps.Application.Queries.GetMyKitReservations;

namespace STRIDE.Modules.KitOps.Application.Tests.Queries;

public sealed class GetMyKitReservationsQueryHandlerTests
{
    private readonly IKitUserHistoryReadService     _history = Substitute.For<IKitUserHistoryReadService>();
    private readonly GetMyKitReservationsQueryHandler _handler;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId   = Guid.NewGuid();

    public GetMyKitReservationsQueryHandlerTests()
    {
        _handler = new GetMyKitReservationsQueryHandler(_history);
    }

    [Fact]
    public async Task Handle_DelegatesToReadService_WithCorrectTenantAndUser()
    {
        var expected = new List<MyKitReservationDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Body Camera", "Safety",
                DateTime.UtcNow.AddDays(-1), null, "Pending", 0),
        };
        _history.GetMyReservationsAsync(TenantId, UserId, Arg.Any<CancellationToken>())
                .Returns((IReadOnlyList<MyKitReservationDto>)expected);

        var result = await _handler.Handle(
            new GetMyKitReservationsQuery(TenantId, UserId), CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
        await _history.Received(1).GetMyReservationsAsync(TenantId, UserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoReservations_ReturnsEmptyList()
    {
        _history.GetMyReservationsAsync(TenantId, UserId, Arg.Any<CancellationToken>())
                .Returns((IReadOnlyList<MyKitReservationDto>)new List<MyKitReservationDto>());

        var result = await _handler.Handle(
            new GetMyKitReservationsQuery(TenantId, UserId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenThrough()
    {
        using var cts = new CancellationTokenSource();
        _history.GetMyReservationsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), cts.Token)
                .Returns((IReadOnlyList<MyKitReservationDto>)new List<MyKitReservationDto>());

        await _handler.Handle(new GetMyKitReservationsQuery(TenantId, UserId), cts.Token);

        await _history.Received(1).GetMyReservationsAsync(TenantId, UserId, cts.Token);
    }
}
