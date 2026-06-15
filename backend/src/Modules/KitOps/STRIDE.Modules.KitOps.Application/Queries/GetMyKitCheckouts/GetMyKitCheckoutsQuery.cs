using MediatR;
using STRIDE.Modules.KitOps.Application.DTOs;

namespace STRIDE.Modules.KitOps.Application.Queries.GetMyKitCheckouts;

public sealed record GetMyKitCheckoutsQuery(
    Guid TenantId,
    Guid UserId) : IRequest<IReadOnlyList<MyKitCheckoutDto>>;
