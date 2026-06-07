using MediatR;
using Microsoft.Extensions.Logging;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Notifications.Application;
using STRIDE.Modules.Notifications.Application.Repositories;
using STRIDE.Modules.Notifications.Domain.Entities;

namespace STRIDE.Modules.Notifications.Application.Commands.CreateNotification;

internal sealed class CreateNotificationCommandHandler
    : IRequestHandler<CreateNotificationCommand, Result<CreateNotificationResult>>
{
    private readonly INotificationRepository _notifications;
    private readonly ILogger<CreateNotificationCommandHandler> _logger;

    public CreateNotificationCommandHandler(
        INotificationRepository notifications,
        ILogger<CreateNotificationCommandHandler> logger)
    {
        _notifications = notifications;
        _logger        = logger;
    }

    public async Task<Result<CreateNotificationResult>> Handle(
        CreateNotificationCommand request,
        CancellationToken cancellationToken)
    {
        var notification = Notification.Create(
            request.TenantId,
            request.RecipientId,
            request.Type,
            request.Title,
            request.Body,
            request.CreatedBy);

        await _notifications.AddAsync(notification, cancellationToken);
        await _notifications.SaveChangesAsync(cancellationToken);

        _logger.NotificationCreated(notification.Id, notification.Type, request.RecipientId, request.TenantId);

        return Result.Success(new CreateNotificationResult(notification.Id));
    }
}
