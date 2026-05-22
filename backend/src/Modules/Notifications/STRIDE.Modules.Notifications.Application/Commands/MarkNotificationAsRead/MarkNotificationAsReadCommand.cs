using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Notifications.Application.Commands.MarkNotificationAsRead;

/// <summary>Marks a notification as read for the requesting user.</summary>
public sealed record MarkNotificationAsReadCommand(
    Guid TenantId,
    Guid NotificationId,
    Guid RecipientId) : IRequest<Result>;
