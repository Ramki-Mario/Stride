using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Exceptions;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Application.Commands.CheckoutKitItem;

internal sealed class CheckoutKitItemCommandHandler
    : IRequestHandler<CheckoutKitItemCommand, Result<Guid>>
{
    private readonly IKitItemRepository     _kitItems;
    private readonly IKitCheckoutRepository _checkouts;
    private readonly IAuditLogger           _audit;
    private readonly ICurrentUser           _currentUser;

    public CheckoutKitItemCommandHandler(
        IKitItemRepository     kitItems,
        IKitCheckoutRepository checkouts,
        IAuditLogger           audit,
        ICurrentUser           currentUser)
    {
        _kitItems    = kitItems;
        _checkouts   = checkouts;
        _audit       = audit;
        _currentUser = currentUser;
    }

    public async Task<Result<Guid>> Handle(
        CheckoutKitItemCommand request, CancellationToken cancellationToken)
    {
        var item = await _kitItems.GetByIdAsync(request.KitItemId, cancellationToken);
        if (item is null)
            return Result.Failure<Guid>($"Kit item '{request.KitItemId}' not found.");

        if (!item.IsActive)
            return Result.Failure<Guid>($"Kit item '{item.Name}' is inactive and cannot be checked out.");

        KitCheckout checkout;
        try
        {
            var expectedReturnAt = DateTime.UtcNow.AddDays(request.Days);
            checkout = KitCheckout.Create(new NewKitCheckout(
                request.TenantId,
                request.KitItemId,
                request.CheckedOutByUserId,
                expectedReturnAt,
                request.Notes));
        }
        catch (KitOpsDomainException ex)
        {
            return Result.Failure<Guid>(ex.Message);
        }

        // Atomic, lock-protected availability check + insert — prevents oversubscription.
        var checkedOut = await _checkouts.TryCheckoutAsync(item, checkout, cancellationToken);
        if (!checkedOut)
            return Result.Failure<Guid>($"No units of '{item.Name}' are currently available for checkout.");

        _ = _audit.LogAsync(new AuditLogEntry(
            TenantId:     request.TenantId,
            ActorId:      request.CheckedOutByUserId,
            ActorEmail:   _currentUser.Email,
            Action:       AuditActions.KitCheckedOut,
            ResourceType: "KitCheckout",
            ResourceId:   checkout.Id,
            NewValueJson: $"{{\"kitItemId\":\"{item.Id}\",\"expectedReturnAt\":\"{checkout.ExpectedReturnAt:O}\"}}"));

        return Result.Success(checkout.Id);
    }
}
