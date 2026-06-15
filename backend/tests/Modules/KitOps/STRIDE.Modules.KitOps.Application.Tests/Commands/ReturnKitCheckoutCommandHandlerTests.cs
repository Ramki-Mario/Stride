using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.Commands.ReturnKitCheckout;
using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Enums;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Application.Tests.Commands;

public sealed class ReturnKitCheckoutCommandHandlerTests
{
    private readonly IKitCheckoutRepository    _checkouts    = Substitute.For<IKitCheckoutRepository>();
    private readonly IKitReservationRepository _reservations = Substitute.For<IKitReservationRepository>();
    private readonly IAuditLogger              _audit        = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser              _currentUser  = Substitute.For<ICurrentUser>();
    private readonly ReturnKitCheckoutCommandHandler _handler;

    private static readonly Guid TenantId   = Guid.NewGuid();
    private static readonly Guid CheckoutId = Guid.NewGuid();
    private static readonly Guid KitItemId  = Guid.NewGuid();
    private static readonly Guid UserId     = Guid.NewGuid();

    public ReturnKitCheckoutCommandHandlerTests()
    {
        _currentUser.Email.Returns("field@test.com");
        _handler = new ReturnKitCheckoutCommandHandler(_checkouts, _reservations, _audit, _currentUser);
    }

    private static KitCheckout ActiveCheckout() =>
        KitCheckout.Create(new NewKitCheckout(
            TenantId, KitItemId, UserId, DateTime.UtcNow.AddDays(3), null));

    private static KitReservation PendingReservation() =>
        KitReservation.Create(new NewKitReservation(TenantId, KitItemId, Guid.NewGuid(), null));

    [Fact]
    public async Task Handle_ActiveCheckout_ReturnsAndRestoresAvailability()
    {
        var checkout = ActiveCheckout();
        _checkouts.GetByIdAsync(CheckoutId, Arg.Any<CancellationToken>()).Returns(checkout);
        _reservations.GetOldestPendingByKitItemIdAsync(KitItemId, Arg.Any<CancellationToken>())
                     .Returns((KitReservation?)null);

        var result = await _handler.Handle(
            new ReturnKitCheckoutCommand(TenantId, CheckoutId, UserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        checkout.ReturnedAt.Should().NotBeNull();
        await _checkouts.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_WithPendingReservation_FulfillsOldestRequest()
    {
        var checkout    = ActiveCheckout();
        var reservation = PendingReservation();
        _checkouts.GetByIdAsync(CheckoutId, Arg.Any<CancellationToken>()).Returns(checkout);
        _reservations.GetOldestPendingByKitItemIdAsync(KitItemId, Arg.Any<CancellationToken>())
                     .Returns(reservation);

        var result = await _handler.Handle(
            new ReturnKitCheckoutCommand(TenantId, CheckoutId, UserId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        reservation.Status.Should().Be(KitReservationStatus.Fulfilled);
        await _checkouts.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_CheckoutNotFound_ReturnsFailure()
    {
        _checkouts.GetByIdAsync(CheckoutId, Arg.Any<CancellationToken>()).Returns((KitCheckout?)null);

        var result = await _handler.Handle(
            new ReturnKitCheckoutCommand(TenantId, CheckoutId, UserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
        await _checkouts.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_AlreadyReturned_ReturnsFailure()
    {
        var checkout = ActiveCheckout();
        checkout.Return(DateTime.UtcNow, UserId);
        _checkouts.GetByIdAsync(CheckoutId, Arg.Any<CancellationToken>()).Returns(checkout);

        var result = await _handler.Handle(
            new ReturnKitCheckoutCommand(TenantId, CheckoutId, UserId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("already been returned");
    }
}
