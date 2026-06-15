using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.KitOps.Application.Commands.UpdateKitItem;

public sealed record UpdateKitItemCommand(
    Guid    TenantId,
    Guid    KitItemId,
    string  Name,
    string  Category,
    string? Description,
    int     TotalQuantity,
    Guid    UpdatedBy) : IRequest<Result>;
