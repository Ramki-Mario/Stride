using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Notifications.Application.Commands.DeleteNotification;

/// <summary>Soft-deletes a notification for the requesting user.</summary>
public sealed record DeleteNotificationCommand(
    Guid TenantId,
    Guid NotificationId,
    Guid RecipientId) : IRequest<Result>;
