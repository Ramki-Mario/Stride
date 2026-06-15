using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.KitOps.Application.Commands.CancelKitReservation;

public sealed record CancelKitReservationCommand(
    Guid TenantId,
    Guid ReservationId,
    Guid CancelledByUserId) : IRequest<Result>
{
    /// <summary>Sentinel error the API maps to 403 Forbidden (caller is not the reservation owner).</summary>
    public const string NotOwnerError = "You can only cancel your own reservation.";
}
