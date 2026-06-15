using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.KitOps.Application.Commands.CreateKitReservation;

public sealed record CreateKitReservationCommand(
    Guid    TenantId,
    Guid    KitItemId,
    Guid    RequestedByUserId,
    string? Notes) : IRequest<Result<Guid>>;
