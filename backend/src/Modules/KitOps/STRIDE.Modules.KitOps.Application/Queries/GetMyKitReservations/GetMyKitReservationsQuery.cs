using MediatR;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Application.Queries.GetMyKitReservations;

public sealed record GetMyKitReservationsQuery(
    Guid TenantId,
    Guid UserId) : IRequest<IReadOnlyList<MyKitReservationDto>>;
