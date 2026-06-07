using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Notifications.Application;
using STRIDE.Modules.Notifications.Application.Repositories;

namespace STRIDE.Modules.Notifications.Application.Commands.MarkNotificationAsRead;

internal sealed class MarkNotificationAsReadCommandHandler
    : IRequestHandler<MarkNotificationAsReadCommand, Result>
{
    private readonly INotificationRepository _notifications;
    private readonly ILogger<MarkNotificationAsReadCommandHandler> _logger;

    public MarkNotificationAsReadCommandHandler(
        INotificationRepository notifications,
        ILogger<MarkNotificationAsReadCommandHandler> logger)
    {
        _notifications = notifications;
        _logger        = logger;
    }

    public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notifications.GetByIdAsync(
            request.TenantId, request.NotificationId, cancellationToken);

        if (notification is null)
        {
            _logger.MarkAsReadNotFound(request.NotificationId, request.TenantId);
            return Result.Failure($"Notification '{request.NotificationId}' not found.");
        }

        if (notification.RecipientId != request.RecipientId)
        {
            _logger.MarkAsReadRecipientMismatch(request.NotificationId);
            return Result.Failure("You do not have permission to modify this notification.");
        }

        notification.MarkAsRead();
        await _notifications.UpdateAsync(notification, cancellationToken);
        await _notifications.SaveChangesAsync(cancellationToken);

        _logger.NotificationMarkedAsRead(request.NotificationId, request.RecipientId);

        return Result.Success();
    }
}
