using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.Commands.CreateKitReservation;
using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Application.Tests.Commands;

public sealed class CreateKitReservationCommandHandlerTests
{
    private readonly IKitItemRepository        _items        = Substitute.For<IKitItemRepository>();
    private readonly IKitCheckoutRepository    _checkouts    = Substitute.For<IKitCheckoutRepository>();
    private readonly IKitReservationRepository _reservations = Substitute.For<IKitReservationRepository>();
    private readonly IAuditLogger              _audit        = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser              _currentUser  = Substitute.For<ICurrentUser>();
    private readonly CreateKitReservationCommandHandler _handler;

    private static readonly Guid TenantId  = Guid.NewGuid();
    private static readonly Guid KitItemId = Guid.NewGuid();
    private static readonly Guid UserId    = Guid.NewGuid();

    public CreateKitReservationCommandHandlerTests()
    {
        _currentUser.Email.Returns("field@test.com");
        _handler = new CreateKitReservationCommandHandler(
            _items, _checkouts, _reservations, _audit, _currentUser);
    }

    private static KitItem ItemWithQuantity(int total = 2) =>
        KitItem.Create(new NewKitItem(TenantId, "Radio HT1000", "Communications", null, total, UserId));

    private CreateKitReservationCommand Command() =>
        new(TenantId, KitItemId, UserId, "Need for night shift");

    /// <summary>Make the item fully checked out so no units are available.</summary>
    private void ArrangeFullyOut(KitItem item)
    {
        _items.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns(item);
        _checkouts.GetOutstandingCountByKitItemIdAsync(KitItemId, Arg.Any<CancellationToken>())
                  .Returns(item.TotalQuantity);
    }

    [Fact]
    public async Task Handle_NoUnitsAvailable_CreatesReservation()
    {
        var item = ItemWithQuantity();
        ArrangeFullyOut(item);
        _reservations.ExistsPendingForUserAsync(KitItemId, UserId, Arg.Any<CancellationToken>())
                     .Returns(false);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        result.Value.Should().NotBe(Guid.Empty);
        await _reservations.Received(1).AddAsync(Arg.Any<KitReservation>(), Arg.Any<CancellationToken>());
        await _reservations.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_UnitsAvailable_RejectsReservation()
    {
        var item = ItemWithQuantity(total: 5);
        _items.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns(item);
        _checkouts.GetOutstandingCountByKitItemIdAsync(KitItemId, Arg.Any<CancellationToken>())
                  .Returns(2);   // 5 - 2 = 3 available

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("available");
        await _reservations.DidNotReceive().AddAsync(Arg.Any<KitReservation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_DuplicatePendingRequest_Rejects()
    {
        var item = ItemWithQuantity();
        ArrangeFullyOut(item);
        _reservations.ExistsPendingForUserAsync(KitItemId, UserId, Arg.Any<CancellationToken>())
                     .Returns(true);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already have a pending request");
        await _reservations.DidNotReceive().AddAsync(Arg.Any<KitReservation>(), Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ItemNotFound_ReturnsFailure()
    {
        _items.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns((KitItem?)null);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_InactiveItem_ReturnsFailure()
    {
        var item = ItemWithQuantity();
        item.Deactivate(UserId);
        _items.GetByIdAsync(KitItemId, Arg.Any<CancellationToken>()).Returns(item);

        var result = await _handler.Handle(Command(), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("inactive");
        await _reservations.DidNotReceive().AddAsync(Arg.Any<KitReservation>(), Arg.Any<CancellationToken>());
    }
}
