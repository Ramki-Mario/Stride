using MediatR;
using STRIDE.BuildingBlocks.Application.Results;

namespace STRIDE.Modules.Notifications.Application.Queries.GetNotifications;

/// <summary>Returns all non-deleted notifications for the requesting user, newest first.</summary>
public sealed record GetNotificationsQuery(
    Guid TenantId,
    Guid RecipientId) : IRequest<Result<IReadOnlyList<NotificationDto>>>;
