using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.KitOps.Application.Commands.DeactivateKitItem;

public sealed record DeactivateKitItemCommand(
    Guid TenantId,
    Guid KitItemId,
    Guid DeactivatedBy) : IRequest<Result>;
