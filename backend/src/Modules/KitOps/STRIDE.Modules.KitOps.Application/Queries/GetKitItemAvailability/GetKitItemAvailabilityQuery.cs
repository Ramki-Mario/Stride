using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Application.Queries.GetKitItemAvailability;

public sealed record GetKitItemAvailabilityQuery(
    Guid TenantId,
    Guid KitItemId) : IRequest<Result<KitItemAvailabilityDto>>;
