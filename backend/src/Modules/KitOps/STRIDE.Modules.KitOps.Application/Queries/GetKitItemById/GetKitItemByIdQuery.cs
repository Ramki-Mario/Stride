using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Application.Queries.GetKitItemById;

public sealed record GetKitItemByIdQuery(
    Guid TenantId,
    Guid KitItemId) : IRequest<Result<KitItemDetailDto>>;
