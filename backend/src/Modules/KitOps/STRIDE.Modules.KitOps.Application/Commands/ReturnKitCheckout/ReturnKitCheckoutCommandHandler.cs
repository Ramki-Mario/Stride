using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.KitOps.Domain.Exceptions;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Application.Commands.ReturnKitCheckout;

internal sealed class ReturnKitCheckoutCommandHandler
    : IRequestHandler<ReturnKitCheckoutCommand, Result>
{
    private readonly IKitCheckoutRepository _checkouts;
    private readonly IAuditLogger           _audit;
    private readonly ICurrentUser           _currentUser;

    public ReturnKitCheckoutCommandHandler(
        IKitCheckoutRepository checkouts,
        IAuditLogger           audit,
        ICurrentUser           currentUser)
    {
        _checkouts   = checkouts;
        _audit       = audit;
        _currentUser = currentUser;
    }

    public async Task<Result> Handle(
        ReturnKitCheckoutCommand request, CancellationToken cancellationToken)
    {
        var checkout = await _checkouts.GetByIdAsync(request.CheckoutId, cancellationToken);
        if (checkout is null)
            return Result.Failure($"Checkout '{request.CheckoutId}' not found.");

        try
        {
            checkout.Return(DateTime.UtcNow, request.ReturnedByUserId);
        }
        catch (KitOpsDomainException ex)
        {
            return Result.Failure(ex.Message);
        }

        await _checkouts.SaveChangesAsync(cancellationToken);

        _ = _audit.LogAsync(new AuditLogEntry(
            TenantId:     request.TenantId,
            ActorId:      request.ReturnedByUserId,
            ActorEmail:   _currentUser.Email,
            Action:       AuditActions.KitReturned,
            ResourceType: "KitCheckout",
            ResourceId:   checkout.Id,
            NewValueJson: $"{{\"kitItemId\":\"{checkout.KitItemId}\"}}"));

        return Result.Success();
    }
}
