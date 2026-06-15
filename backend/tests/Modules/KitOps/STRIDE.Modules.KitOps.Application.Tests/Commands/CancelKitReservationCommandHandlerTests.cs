using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.Modules.KitOps.Application.Commands.CancelKitReservation;
using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Enums;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Application.Tests.Commands;

public sealed class CancelKitReservationCommandHandlerTests
{
    private readonly IKitReservationRepository _reservations = Substitute.For<IKitReservationRepository>();
    private readonly IAuditLogger              _audit        = Substitute.For<IAuditLogger>();
    private readonly ICurrentUser              _currentUser  = Substitute.For<ICurrentUser>();
    private readonly CancelKitReservationCommandHandler _handler;

    private static readonly Guid TenantId      = Guid.NewGuid();
    private static readonly Guid ReservationId = Guid.NewGuid();
    private static readonly Guid KitItemId     = Guid.NewGuid();
    private static readonly Guid OwnerId       = Guid.NewGuid();

    public CancelKitReservationCommandHandlerTests()
    {
        _currentUser.Email.Returns("field@test.com");
        _handler = new CancelKitReservationCommandHandler(_reservations, _audit, _currentUser);
    }

    private static KitReservation PendingReservation(Guid ownerId) =>
        KitReservation.Create(new NewKitReservation(TenantId, KitItemId, ownerId, null));

    [Fact]
    public async Task Handle_OwnerCancelsPending_Succeeds()
    {
        var reservation = PendingReservation(OwnerId);
        _reservations.GetByIdAsync(ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);

        var result = await _handler.Handle(
            new CancelKitReservationCommand(TenantId, ReservationId, OwnerId), CancellationToken.None);

        result.IsSuccess.Should().BeTrue();
        reservation.Status.Should().Be(KitReservationStatus.Cancelled);
        await _reservations.Received(1).SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_NotOwner_ReturnsOwnerError()
    {
        var reservation = PendingReservation(OwnerId);
        _reservations.GetByIdAsync(ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);

        var result = await _handler.Handle(
            new CancelKitReservationCommand(TenantId, ReservationId, Guid.NewGuid()), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Be(CancelKitReservationCommand.NotOwnerError);
        reservation.Status.Should().Be(KitReservationStatus.Pending);
        await _reservations.DidNotReceive().SaveChangesAsync(Arg.Any<CancellationToken>());
    }

    [Fact]
    public async Task Handle_ReservationNotFound_ReturnsFailure()
    {
        _reservations.GetByIdAsync(ReservationId, Arg.Any<CancellationToken>())
                     .Returns((KitReservation?)null);

        var result = await _handler.Handle(
            new CancelKitReservationCommand(TenantId, ReservationId, OwnerId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("not found");
    }

    [Fact]
    public async Task Handle_AlreadyCancelled_ReturnsFailure()
    {
        var reservation = PendingReservation(OwnerId);
        reservation.Cancel(OwnerId);
        _reservations.GetByIdAsync(ReservationId, Arg.Any<CancellationToken>()).Returns(reservation);

        var result = await _handler.Handle(
            new CancelKitReservationCommand(TenantId, ReservationId, OwnerId), CancellationToken.None);

        result.IsFailure.Should().BeTrue();
        result.Error.Should().Contain("Only pending reservations can be cancelled");
    }
}
