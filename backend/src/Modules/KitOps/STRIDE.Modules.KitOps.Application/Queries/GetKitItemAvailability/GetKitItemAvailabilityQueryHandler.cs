using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.KitOps.Application.DTOs;
using STRIDE.Modules.KitOps.Domain.Repositories;

namespace STRIDE.Modules.KitOps.Application.Queries.GetKitItemAvailability;

internal sealed class GetKitItemAvailabilityQueryHandler
    : IRequestHandler<GetKitItemAvailabilityQuery, Result<KitItemAvailabilityDto>>
{
    private readonly IKitItemRepository     _kitItems;
    private readonly IKitCheckoutRepository _checkouts;

    public GetKitItemAvailabilityQueryHandler(
        IKitItemRepository     kitItems,
        IKitCheckoutRepository checkouts)
    {
        _kitItems  = kitItems;
        _checkouts = checkouts;
    }

    public async Task<Result<KitItemAvailabilityDto>> Handle(
        GetKitItemAvailabilityQuery request, CancellationToken cancellationToken)
    {
        var item = await _kitItems.GetByIdAsync(request.KitItemId, cancellationToken);
        if (item is null)
            return Result.Failure<KitItemAvailabilityDto>($"Kit item '{request.KitItemId}' not found.");

        var outstanding = await _checkouts.GetOutstandingCountByKitItemIdAsync(
            request.KitItemId, cancellationToken);
        var overdue = await _checkouts.GetOverdueCountByKitItemIdAsync(
            request.KitItemId, DateTime.UtcNow, cancellationToken);

        var available = Math.Max(0, item.TotalQuantity - outstanding);

        return Result.Success(new KitItemAvailabilityDto(
            item.Id,
            item.Name,
            item.TotalQuantity,
            outstanding,
            available,
            overdue));
    }
}
