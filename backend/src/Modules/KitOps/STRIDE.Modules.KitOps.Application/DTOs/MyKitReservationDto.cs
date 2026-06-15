namespace STRIDE.Modules.KitOps.Application.DTOs;

public sealed record MyKitReservationDto(
    Guid     ReservationId,
    Guid     KitItemId,
    string   KitItemName,
    string   Category,
    DateTime RequestedAt,
    string?  Notes,
    string   StatusLabel,
    int      StatusValue);
