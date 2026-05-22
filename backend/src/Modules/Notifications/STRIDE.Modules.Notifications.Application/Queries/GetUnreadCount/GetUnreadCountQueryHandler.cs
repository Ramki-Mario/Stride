using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Notifications.Application.Repositories;

namespace STRIDE.Modules.Notifications.Application.Queries.GetUnreadCount;

internal sealed class GetUnreadCountQueryHandler
    : IRequestHandler<GetUnreadCountQuery, Result<int>>
{
    private readonly INotificationRepository _notifications;

    public GetUnreadCountQueryHandler(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<Result<int>> Handle(GetUnreadCountQuery request, CancellationToken ct)
    {
        var count = await _notifications.GetUnreadCountAsync(
            request.TenantId, request.RecipientId, ct);

        return Result.Success(count);
    }
}
