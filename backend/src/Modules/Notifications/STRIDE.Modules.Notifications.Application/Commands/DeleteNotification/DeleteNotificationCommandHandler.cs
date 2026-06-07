using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Notifications.Application;
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
            _logger.DeleteNotificationNotFound(request.NotificationId, request.TenantId);
            return Result.Failure($"Notification '{request.NotificationId}' not found.");
        }

        if (notification.RecipientId != request.RecipientId)
        {
            _logger.DeleteNotificationRecipientMismatch(request.NotificationId);
            return Result.Failure("You do not have permission to delete this notification.");
        }

        notification.Delete();
        await _notifications.UpdateAsync(notification, cancellationToken);
        await _notifications.SaveChangesAsync(cancellationToken);

        _logger.NotificationDeleted(request.NotificationId, request.RecipientId);

        return Result.Success();
    }
}
