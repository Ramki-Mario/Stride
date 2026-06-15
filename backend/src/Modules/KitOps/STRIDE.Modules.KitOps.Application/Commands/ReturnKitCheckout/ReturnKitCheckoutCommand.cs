using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.KitOps.Application.Commands.ReturnKitCheckout;

public sealed record ReturnKitCheckoutCommand(
    Guid TenantId,
    Guid CheckoutId,
    Guid ReturnedByUserId) : IRequest<Result>;
