using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Notifications.Application.Repositories;

namespace STRIDE.Modules.Notifications.Application.Commands.DeleteNotification;

internal sealed class DeleteNotificationCommandHandler
    : IRequestHandler<DeleteNotificationCommand, Result>
{
    private readonly INotificationRepository _notifications;
    private readonly ILogger<DeleteNotificationCommandHandler> _logger;

    public DeleteNotificationCommandHandler(
        INotificationRepository notifications,
        ILogger<DeleteNotificationCommandHandler> logger)
    {
        _notifications = notifications;
        _logger        = logger;
    }

    public async Task<Result> Handle(DeleteNotificationCommand request, CancellationToken cancellationToken)
    {
        var notification = await _notifications.GetByIdAsync(
            request.TenantId, request.NotificationId, cancellationToken);

        if (notification is null)
        {
            _logger.LogWarning(
                "DeleteNotification: notification {Id} not found for tenant {TenantId}",
                request.NotificationId, request.TenantId);
            return Result.Failure($"Notification '{request.NotificationId}' not found.");
        }

        if (notification.RecipientId != request.RecipientId)
        {
            _logger.LogWarning(
                "DeleteNotification: recipient mismatch for notification {Id}",
                request.NotificationId);
            return Result.Failure("You do not have permission to delete this notification.");
        }

        notification.Delete();
        await _notifications.UpdateAsync(notification, cancellationToken);
        await _notifications.SaveChangesAsync(cancellationToken);

        _logger.LogInformation(
            "Notification {Id} soft-deleted by {RecipientId}", request.NotificationId, request.RecipientId);

        return Result.Success();
    }
}
