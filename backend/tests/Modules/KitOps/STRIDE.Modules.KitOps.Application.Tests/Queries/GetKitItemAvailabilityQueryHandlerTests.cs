using STRIDE.Modules.KitOps.Application.Queries.GetKitItemAvailability;
using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Application.Tests.Queries;

public sealed class GetKitItemAvailabilityQueryHandlerTests
{
    private readonly IKitItemRepository     _items     = Substitute.For<IKitItemRepository>();
    private readonly IKitCheckoutRepository _checkouts = Substitute.For<IKitCheckoutRepository>();
    private readonly GetKitItemAvailabilityQueryHandler _handler;

    private static readonly Guid TenantId  = Guid.NewGuid();
    private static readonly Guid KitItemId = Guid.NewGuid();
    private static readonly Guid UserId    = Guid.NewGuid();

    public GetKitItemAvailabilityQueryHandlerTests()
    {
        _handler = new GetKitItemAvailabilityQueryHandler(_items, _checkouts);
    }

    private static KitItem ItemWithQuantity(int total) =>
        KitItem.Create(new NewKitItem(TenantId, "Radio HT1000", "Communications", null, total, UserId));

    [Fact]
    public async Task Handle_ComputesAvailabilityAsTotalMinusOutstanding()
    {
        _items.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns(ItemWithQuantity(5));
        _checkouts.GetOutstandingCountByKitItemIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns(2);
        _checkouts.GetOverdueCountByKitItemIdAsync(KitItemId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                  .Returns(1);

        var result = await _handler.Handle(
            new GetKitItemAvailabilityQuery(TenantId, KitItemId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.TotalQuantity.Should().Be(5);
        result.Value.OutstandingCheckouts.Should().Be(2);
        result.Value.AvailableQuantity.Should().Be(3);
        result.Value.OverdueCheckouts.Should().Be(1);
    }

    [Fact]
    public async Task Handle_OutstandingExceedsTotal_AvailabilityClampedToZero()
    {
        _items.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns(ItemWithQuantity(2));
        _checkouts.GetOutstandingCountByKitItemIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns(3);
        _checkouts.GetOverdueCountByKitItemIdAsync(KitItemId, Arg.Any<DateTime>(), Arg.Any<CancellationToken>())
                  .Returns(0);

        var result = await _handler.Handle(
            new GetKitItemAvailabilityQuery(TenantId, KitItemId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value!.AvailableQuantity.Should().Be(0);
    }

    [Fact]
    public async Task Handle_ItemNotFound_ReturnsFailure()
    {
        _items.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns((KitItem?)null);

        var result = await _handler.Handle(
            new GetKitItemAvailabilityQuery(TenantId, KitItemId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }
}
