using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
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

    public async Task<Result> Handle(MarkNotificationAsReadCommand request, CancellationToken ct)
    {
        var notification = await _notifications.GetByIdAsync(
            request.TenantId, request.NotificationId, ct);

        if (notification is null)
        {
            _logger.LogWarning(
                "MarkAsRead: notification {Id} not found for tenant {TenantId}",
                request.NotificationId, request.TenantId);
            return Result.Failure($"Notification '{request.NotificationId}' not found.");
        }

        if (notification.RecipientId != request.RecipientId)
        {
            _logger.LogWarning(
                "MarkAsRead: recipient mismatch for notification {Id}",
                request.NotificationId);
            return Result.Failure("You do not have permission to modify this notification.");
        }

        notification.MarkAsRead();
        await _notifications.UpdateAsync(notification, ct);
        await _notifications.SaveChangesAsync(ct);

        _logger.LogInformation(
            "Notification {Id} marked as read by {RecipientId}", request.NotificationId, request.RecipientId);

        return Result.Success();
    }
}
