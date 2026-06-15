using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.KitOps.Domain.Exceptions;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Application.Commands.ReturnKitCheckout;

internal sealed class ReturnKitCheckoutCommandHandler
    : IRequestHandler<ReturnKitCheckoutCommand, Result>
{
    private readonly IKitCheckoutRepository    _checkouts;
    private readonly IKitReservationRepository _reservations;
    private readonly IAuditLogger              _audit;
    private readonly ICurrentUser              _currentUser;

    public ReturnKitCheckoutCommandHandler(
        IKitCheckoutRepository    checkouts,
        IKitReservationRepository reservations,
        IAuditLogger              audit,
        ICurrentUser              currentUser)
    {
        _checkouts    = checkouts;
        _reservations = reservations;
        _audit        = audit;
        _currentUser  = currentUser;
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

        // The returned unit frees a slot — flag the next queued request as fulfilled (FIFO).
        // Both the checkout and the reservation are tracked by the same scoped DbContext, so a
        // single SaveChanges persists the return and the fulfillment atomically.
        var nextReservation = await _reservations.GetOldestPendingByKitItemIdAsync(
            checkout.KitItemId, cancellationToken);
        if (nextReservation is not null)
            nextReservation.Fulfill(request.ReturnedByUserId);

        await _checkouts.SaveChangesAsync(cancellationToken);

        _ = _audit.LogAsync(new AuditLogEntry(
            TenantId:     request.TenantId,
            ActorId:      request.ReturnedByUserId,
            ActorEmail:   _currentUser.Email,
            Action:       AuditActions.KitReturned,
            ResourceType: "KitCheckout",
            ResourceId:   checkout.Id,
            NewValueJson: $"{{\"kitItemId\":\"{checkout.KitItemId}\"}}"));

        if (nextReservation is not null)
            _ = _audit.LogAsync(new AuditLogEntry(
                TenantId:     request.TenantId,
                ActorId:      request.ReturnedByUserId,
                ActorEmail:   _currentUser.Email,
                Action:       AuditActions.KitReservationFulfilled,
                ResourceType: "KitReservation",
                ResourceId:   nextReservation.Id,
                NewValueJson: $"{{\"kitItemId\":\"{checkout.KitItemId}\"}}"));

        return Result.Success();
    }
}
