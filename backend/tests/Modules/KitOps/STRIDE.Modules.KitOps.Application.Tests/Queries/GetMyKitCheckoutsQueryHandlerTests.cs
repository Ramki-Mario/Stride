using STRIDE.Modules.KitOps.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.DTOs;
using STRIDE.Modules.KitOps.Application.Queries.GetMyKitCheckouts;

namespace STRIDE.Modules.KitOps.Application.Tests.Queries;

public sealed class GetMyKitCheckoutsQueryHandlerTests
{
    private readonly IKitUserHistoryReadService  _history = Substitute.For<IKitUserHistoryReadService>();
    private readonly GetMyKitCheckoutsQueryHandler _handler;

    private static readonly Guid TenantId = Guid.NewGuid();
    private static readonly Guid UserId   = Guid.NewGuid();

    public GetMyKitCheckoutsQueryHandlerTests()
    {
        _handler = new GetMyKitCheckoutsQueryHandler(_history);
    }

    [Fact]
    public async Task Handle_DelegatesToReadService_WithCorrectTenantAndUser()
    {
        var expected = new List<MyKitCheckoutDto>
        {
            new(Guid.NewGuid(), Guid.NewGuid(), "Radio HT1000", "Communications",
                DateTime.UtcNow.AddDays(-3), DateTime.UtcNow.AddDays(4), null, null, "Active", 0),
        };
        _history.GetMyCheckoutsAsync(TenantId, UserId, Arg.Any<CancellationToken>())
                .Returns((IReadOnlyList<MyKitCheckoutDto>)expected);

        var result = await _handler.Handle(
            new GetMyKitCheckoutsQuery(TenantId, UserId), CancellationToken.None);

        result.Should().BeEquivalentTo(expected);
        await _history.Received(1).GetMyCheckoutsAsync(TenantId, UserId, Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoCheckouts_ReturnsEmptyList()
    {
        _history.GetMyCheckoutsAsync(TenantId, UserId, Arg.Any<CancellationToken>())
                .Returns((IReadOnlyList<MyKitCheckoutDto>)new List<MyKitCheckoutDto>());

        var result = await _handler.Handle(
            new GetMyKitCheckoutsQuery(TenantId, UserId), CancellationToken.None);

        result.Should().BeEmpty();
    }

    [Fact]
    public async Task Handle_PassesCancellationTokenThrough()
    {
        using var cts = new CancellationTokenSource();
        _history.GetMyCheckoutsAsync(Arg.Any<Guid>(), Arg.Any<Guid>(), cts.Token)
                .Returns((IReadOnlyList<MyKitCheckoutDto>)new List<MyKitCheckoutDto>());

        await _handler.Handle(new GetMyKitCheckoutsQuery(TenantId, UserId), cts.Token);

        await _history.Received(1).GetMyCheckoutsAsync(TenantId, UserId, cts.Token);
    }
}
