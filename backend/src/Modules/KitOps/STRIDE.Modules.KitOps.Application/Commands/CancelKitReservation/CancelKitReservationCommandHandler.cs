using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.KitOps.Domain.Exceptions;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Application.Commands.CancelKitReservation;

internal sealed class CancelKitReservationCommandHandler
    : IRequestHandler<CancelKitReservationCommand, Result>
{
    private readonly IKitReservationRepository _reservations;
    private readonly IAuditLogger              _audit;
    private readonly ICurrentUser              _currentUser;

    public CancelKitReservationCommandHandler(
        IKitReservationRepository reservations,
        IAuditLogger              audit,
        ICurrentUser              currentUser)
    {
        _reservations = reservations;
        _audit        = audit;
        _currentUser  = currentUser;
    }

    public async Task<Result> Handle(
        CancelKitReservationCommand request, CancellationToken cancellationToken)
    {
        var reservation = await _reservations.GetByIdAsync(request.ReservationId, cancellationToken);
        if (reservation is null)
            return Result.Failure($"Reservation '{request.ReservationId}' not found.");

        if (reservation.RequestedByUserId != request.CancelledByUserId)
            return Result.Failure(CancelKitReservationCommand.NotOwnerError);

        try
        {
            reservation.Cancel(request.CancelledByUserId);
        }
        catch (KitOpsDomainException ex)
        {
            return Result.Failure(ex.Message);
        }

        await _reservations.SaveChangesAsync(cancellationToken);

        _ = _audit.LogAsync(new AuditLogEntry(
            TenantId:     request.TenantId,
            ActorId:      request.CancelledByUserId,
            ActorEmail:   _currentUser.Email,
            Action:       AuditActions.KitReservationCancelled,
            ResourceType: "KitReservation",
            ResourceId:   reservation.Id));

        return Result.Success();
    }
}
