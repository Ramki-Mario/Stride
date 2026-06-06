using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Notifications.Application.Repositories;

namespace STRIDE.Modules.Notifications.Application.Queries.GetNotifications;

internal sealed class GetNotificationsQueryHandler
    : IRequestHandler<GetNotificationsQuery, Result<IReadOnlyList<NotificationDto>>>
{
    private readonly INotificationRepository _notifications;

    public GetNotificationsQueryHandler(INotificationRepository notifications)
    {
        _notifications = notifications;
    }

    public async Task<Result<IReadOnlyList<NotificationDto>>> Handle(
        GetNotificationsQuery request,
        CancellationToken cancellationToken)
    {
        var notifications = await _notifications.GetByRecipientAsync(
            request.TenantId, request.RecipientId, cancellationToken);

        var dtos = notifications
            .Select(n => new NotificationDto(
                n.Id,
                n.TenantId,
                n.RecipientId,
                n.Type,
                n.Title,
                n.Body,
                n.IsRead,
                n.CreatedAt))
            .ToList()
            .AsReadOnly();

        return Result.Success<IReadOnlyList<NotificationDto>>(dtos);
    }
}
