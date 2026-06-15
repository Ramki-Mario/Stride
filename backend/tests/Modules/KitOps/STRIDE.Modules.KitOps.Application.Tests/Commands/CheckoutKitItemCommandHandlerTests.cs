using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.Commands.CheckoutKitItem;
using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Application.Tests.Commands;

public sealed class CheckoutKitItemCommandHandlerTests
{
    private readonly IKitItemRepository     _items     = Substitute.For<IKitItemRepository>();
    private readonly IKitCheckoutRepository _checkouts = Substitute.For<IKitCheckoutRepository>();
    private readonly IAuditLogger           _audit     = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser           _currentUser = Substitute.For<ICurrentUser>();
    private readonly CheckoutKitItemCommandHandler _handler;

    private static readonly Guid TenantId  = Guid.NewGuid();
    private static readonly Guid KitItemId = Guid.NewGuid();
    private static readonly Guid UserId    = Guid.NewGuid();

    public CheckoutKitItemCommandHandlerTests()
    {
        _currentUser.Email.Returns("field@test.com");
        _handler = new CheckoutKitItemCommandHandler(_items, _checkouts, _audit, _currentUser);
    }

    private static KitItem ActiveItem() =>
        KitItem.Create(new NewKitItem(TenantId, "Radio HT1000", "Communications", null, 5, UserId));

    private CheckoutKitItemCommand Command(int days = 3) =>
        new(TenantId, KitItemId, UserId, days, "Site survey");

    [Fact]
    public async Task Handle_AvailableItem_ChecksOutAndReturnsSuccess()
    {
        _items.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns(ActiveItem());
        _checkouts.TryCheckoutAsync(Arg.Any<KitItem>(), Arg.Any<KitCheckout>(), Arg.Any<CancellationToken>())
                  .Returns(true);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _checkouts.Received(1).TryCheckoutAsync(
            Arg.Any<KitItem>(), Arg.Any<KitCheckout>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NoAvailability_ReturnsFailureWithoutOversubscribing()
    {
        _items.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns(ActiveItem());
        _checkouts.TryCheckoutAsync(Arg.Any<KitItem>(), Arg.Any<KitCheckout>(), Arg.Any<CancellationToken>())
                  .Returns(false);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("available");
    }

    [Fact]
    public async Task Handle_ItemNotFound_ReturnsFailureAndDoesNotCheckout()
    {
        _items.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns((KitItem?)null);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
        await _checkouts.DidNotReceive().TryCheckoutAsync(
            Arg.Any<KitItem>(), Arg.Any<KitCheckout>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_InactiveItem_ReturnsFailure()
    {
        var item = ActiveItem();
        item.Deactivate(UserId);
        _items.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("inactive");
        await _checkouts.DidNotReceive().TryCheckoutAsync(
            Arg.Any<KitItem>(), Arg.Any<KitCheckout>(), Arg.Any<CancellationToken>());
    }
}
