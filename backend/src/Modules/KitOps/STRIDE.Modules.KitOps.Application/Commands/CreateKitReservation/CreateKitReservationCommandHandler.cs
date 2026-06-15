using MediatR;
using STRIDE.BuildingBlocks.Application.Abstractions;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.KitOps.Domain.Entities;
using STRIDE.Modules.KitOps.Domain.Exceptions;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Application.Commands.CreateKitReservation;

internal sealed class CreateKitReservationCommandHandler
    : IRequestHandler<CreateKitReservationCommand, Result<Guid>>
{
    private readonly IKitItemRepository        _kitItems;
    private readonly IKitCheckoutRepository    _checkouts;
    private readonly IKitReservationRepository _reservations;
    private readonly IAuditLogger              _audit;
    private readonly ICurrentUser              _currentUser;

    public CreateKitReservationCommandHandler(
        IKitItemRepository        kitItems,
        IKitCheckoutRepository    checkouts,
        IKitReservationRepository reservations,
        IAuditLogger              audit,
        ICurrentUser              currentUser)
    {
        _kitItems     = kitItems;
        _checkouts    = checkouts;
        _reservations = reservations;
        _audit        = audit;
        _currentUser  = currentUser;
    }

    public async Task<Result<Guid>> Handle(
        CreateKitReservationCommand request, CancellationToken cancellationToken)
    {
        var item = await _kitItems.GetByIdAsync(request.KitItemId, cancellationToken);
        if (item is null)
            return Result.Failure<Guid>($"Kit item '{request.KitItemId}' not found.");

        if (!item.IsActive)
            return Result.Failure<Guid>($"Kit item '{item.Name}' is inactive and cannot be reserved.");

        // Reservations are only for kit that is currently out — if a unit is free, check out directly.
        var outstanding = await _checkouts.GetOutstandingCountByKitItemIdAsync(
            request.KitItemId, cancellationToken);
        if (item.TotalQuantity - outstanding > 0)
            return Result.Failure<Guid>(
                $"'{item.Name}' has units available — check it out directly instead of reserving.");

        if (await _reservations.ExistsPendingForUserAsync(
                request.KitItemId, request.RequestedByUserId, cancellationToken))
            return Result.Failure<Guid>($"You already have a pending request for '{item.Name}'.");

        KitReservation reservation;
        try
        {
            reservation = KitReservation.Create(new NewKitReservation(
                request.TenantId,
                request.KitItemId,
                request.RequestedByUserId,
                request.Notes));
        }
        catch (KitOpsDomainException ex)
        {
            return Result.Failure<Guid>(ex.Message);
        }

        await _reservations.AddAsync(reservation, cancellationToken);
        await _reservations.SaveChangesAsync(cancellationToken);

        _ = _audit.LogAsync(new AuditLogEntry(
            TenantId:     request.TenantId,
            ActorId:      request.RequestedByUserId,
            ActorEmail:   _currentUser.Email,
            Action:       AuditActions.KitReservationCreated,
            ResourceType: "KitReservation",
            ResourceId:   reservation.Id,
            NewValueJson: $"{{\"kitItemId\":\"{item.Id}\"}}"));

        return Result.Success(reservation.Id);
    }
}
