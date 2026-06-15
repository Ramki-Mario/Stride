using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.KitOps.Application.Commands.CreateKitItem;

public sealed record CreateKitItemCommand(
    Guid    TenantId,
    string  Name,
    string  Category,
    string? Description,
    int     TotalQuantity,
    Guid    CreatedBy) : IRequest<Result<Guid>>;
