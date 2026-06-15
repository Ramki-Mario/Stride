using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.KitOps.Application.Commands.ReactivateKitItem;

public sealed record ReactivateKitItemCommand(
    Guid TenantId,
    Guid KitItemId,
    Guid ReactivatedBy) : IRequest<Result>;
