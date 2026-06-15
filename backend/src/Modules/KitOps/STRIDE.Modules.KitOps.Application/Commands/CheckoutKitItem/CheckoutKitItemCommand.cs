using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.KitOps.Application.Commands.CheckoutKitItem;

public sealed record CheckoutKitItemCommand(
    Guid    TenantId,
    Guid    KitItemId,
    Guid    CheckedOutByUserId,
    int     Days,
    string? Notes) : IRequest<Result<Guid>>;
