using MediatR;
using STRIDE.BuildingBlocks.Application.Results;
using STRIDE.Modules.Notifications.Domain.Enums;

namespace STRIDE.Modules.Notifications.Application.Commands.CreateNotification;

/// <summary>
/// Creates a new notification for a recipient within a tenant.
/// </summary>
public sealed record CreateNotificationCommand(
    Guid TenantId,
    Guid RecipientId,
    NotificationType Type,
    string Title,
    string Body,
    Guid CreatedBy) : IRequest<Result<CreateNotificationResult>>;
